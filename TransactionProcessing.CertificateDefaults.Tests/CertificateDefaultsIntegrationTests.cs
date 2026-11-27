using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using TransactionProcessing.CertificateDefaults;
using Xunit;

namespace TransactionProcessing.CertificateDefaults.Tests;

public sealed class CertificateDefaultsIntegrationTests
{
    private const string KestrelPathKey = "ServiceOptions:KestrelOptions:Path";
    private const string KestrelPasswordKey = "ServiceOptions:KestrelOptions:" + nameof(TestCertificates.Password);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ConfigureTransactionProcessingHttps_serves_full_trusted_chain(bool usePem)
    {
        using var testCertificates = new TestCertificates();
        string path = usePem ? testCertificates.FullChainPath : testCertificates.PfxPath;
        string? keyPath = usePem ? testCertificates.PrivateKeyPath : null;

        await using WebApplication app = await StartApplication(path, testCertificates.Password, keyPath);
        string address = GetAddress(app);
        string thumbprint = await ProbeWithTestRoot(address, testCertificates.Root, testCertificates.Intermediate);

        thumbprint.ShouldBe(testCertificates.Leaf.Thumbprint);
        await ProbeWithSystemTrustFails(address);
    }

    [Fact]
    public async Task ConfigureTransactionProcessingHttps_rejects_hostname_mismatch()
    {
        using var testCertificates = new TestCertificates();
        await using WebApplication app = await StartApplication(testCertificates.PfxPath, testCertificates.Password);
        string address = GetAddress(app).Replace("127.0.0.1", "wrong-name.invalid", StringComparison.Ordinal);

        using var handler = CreateTrustingHandler(testCertificates.Root, testCertificates.Intermediate);
        using var client = new HttpClient(handler);

        HttpRequestException exception = await Should.ThrowAsync<HttpRequestException>(
            () => client.GetStringAsync(address));

        exception.ShouldNotBeNull();
        string failureReason = ((CapturingCertificateCallback)handler.SslOptions.RemoteCertificateValidationCallback!.Target!)
            .FailureReason ?? "The certificate callback did not run.";
        failureReason.ShouldContain("hostname validation failed");
    }

    [Fact]
    public async Task Restarting_after_certificate_rotation_loads_the_new_certificate()
    {
        using var original = new TestCertificates();
        using var replacement = new TestCertificates();

        await using (WebApplication firstApp = await StartApplication(original.PfxPath, original.Password))
        {
            string firstThumbprint = await ProbeWithTestRoot(
                GetAddress(firstApp), original.Root, original.Intermediate);
            firstThumbprint.ShouldBe(original.Leaf.Thumbprint);
        }

        File.Copy(replacement.PfxPath, original.PfxPath, overwrite: true);

        await using WebApplication restartedApp = await StartApplication(original.PfxPath, replacement.Password);
        string rotatedThumbprint = await ProbeWithTestRoot(
            GetAddress(restartedApp), replacement.Root, replacement.Intermediate);

        rotatedThumbprint.ShouldBe(replacement.Leaf.Thumbprint);
    }

    [Theory]
    [InlineData(null, null, "HTTPS certificate configuration is required")]
    [InlineData("missing.pfx", "", "A password is required for PFX certificates")]
    public void ConfigureTransactionProcessingHttps_rejects_invalid_configuration(
        string? path,
        string? password,
        string expectedMessage)
    {
        var builder = WebApplication.CreateBuilder();
        var settings = new Dictionary<string, string?>();
        if (path is not null)
        {
            settings[KestrelPathKey] = path;
        }

        if (password is not null)
        {
            settings[KestrelPasswordKey] = password;
        }

        builder.Configuration.AddInMemoryCollection(settings);
        InvalidOperationException exception = Should.Throw<InvalidOperationException>(
            () => builder.WebHost.ConfigureTransactionProcessingHttps());
        exception.Message.ShouldContain(expectedMessage);
    }

    [Fact]
    public void ConfigureTransactionProcessingHttps_fails_when_certificate_file_is_missing()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [KestrelPathKey] = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".pfx"),
            [KestrelPasswordKey] = "invalid-test-input"
        });
        Exception exception = Should.Throw<Exception>(
            () => builder.WebHost.ConfigureTransactionProcessingHttps());
        exception.ToString().ShouldContain("pfx");
    }

    [Fact]
    public void ConfigureTransactionProcessingHttps_rejects_invalid_pfx_contents()
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".pfx");
        File.WriteAllText(path, "not a PKCS#12 certificate");
        try
        {
            var builder = WebApplication.CreateBuilder();
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                [KestrelPathKey] = path,
                [KestrelPasswordKey] = "invalid-test-input"
            });

            Should.Throw<CryptographicException>(
                () => builder.WebHost.ConfigureTransactionProcessingHttps());
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void ConfigureTransactionProcessingHttps_rejects_missing_pem_private_key()
    {
        using var testCertificates = new TestCertificates();
        var builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [KestrelPathKey] = testCertificates.FullChainPath,
            ["ServiceOptions:KestrelOptions:KeyPath"] = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".key")
        });

        Should.Throw<FileNotFoundException>(
            () => builder.WebHost.ConfigureTransactionProcessingHttps());
    }

    private static async Task<WebApplication> StartApplication(string certificatePath, string password, string? keyPath = null)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Production" });
        var settings = new Dictionary<string, string?>
        {
            [KestrelPathKey] = certificatePath,
            [KestrelPasswordKey] = keyPath is null ? password : null,
            ["ServiceOptions:KestrelOptions:KeyPath"] = keyPath
        };
        builder.Configuration.AddInMemoryCollection(settings);
        builder.WebHost.ConfigureTransactionProcessingHttps();

        var app = builder.Build();
        app.Urls.Add("https://127.0.0.1:0");
        app.MapGet("/", () => Results.Text("ready"));
        await app.StartAsync();
        return app;
    }

    private static string GetAddress(WebApplication app)
    {
        IServer server = app.Services.GetRequiredService<IServer>();
        IServerAddressesFeature addresses = server.Features.Get<IServerAddressesFeature>()!;
        return addresses.Addresses.Single(address => address.StartsWith("https://", StringComparison.Ordinal));
    }

    private static async Task<string> ProbeWithTestRoot(string address, X509Certificate2 root, X509Certificate2 intermediate)
    {
        var handler = CreateTrustingHandler(root, intermediate);
        using var client = new HttpClient(handler);
        HttpResponseMessage response;
        try
        {
            response = await client.GetAsync(address);
        }
        catch (HttpRequestException exception)
        {
            var callback = handler.SslOptions.RemoteCertificateValidationCallback?.Target as CapturingCertificateCallback;
            throw new InvalidOperationException(callback?.FailureReason ?? $"The test certificate chain was rejected (thumbprint {callback?.Thumbprint ?? "not received"}).", exception);
        }

        using (response)
        {
            response.EnsureSuccessStatusCode();
            return GetServerThumbprint(handler);
        }
    }

    private static string GetServerThumbprint(SocketsHttpHandler handler)
    {
        return handler.SslOptions.RemoteCertificateValidationCallback?.Target is CapturingCertificateCallback callback
            ? callback.Thumbprint ?? string.Empty
            : string.Empty;
    }

    private static SocketsHttpHandler CreateTrustingHandler(X509Certificate2 root, X509Certificate2 intermediate)
    {
        var handler = new SocketsHttpHandler { UseProxy = false };
        handler.ConnectCallback = async (context, cancellationToken) =>
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                await socket.ConnectAsync(IPAddress.Loopback, context.DnsEndPoint.Port, cancellationToken);
                return new NetworkStream(socket, ownsSocket: true);
            }
            catch
            {
                socket.Dispose();
                throw;
            }
        };
        var callback = new CapturingCertificateCallback(root, intermediate);
        handler.SslOptions.RemoteCertificateValidationCallback = callback.Invoke;
        return handler;
    }

    private static async Task ProbeWithSystemTrustFails(string address)
    {
        using var handler = new SocketsHttpHandler { UseProxy = false };
        using var client = new HttpClient(handler);
        await Should.ThrowAsync<HttpRequestException>(() => client.GetStringAsync(address));
    }

    private sealed class CapturingCertificateCallback(X509Certificate2 root, X509Certificate2 intermediate)
    {
        public string? Thumbprint { get; private set; }
        public string? FailureReason { get; private set; }

        public bool Invoke(object? sender, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors errors)
        {
            if (certificate is null || chain is null || (errors & SslPolicyErrors.RemoteCertificateNameMismatch) != 0)
            {
                FailureReason = $"Certificate or chain was missing, or hostname validation failed: {errors}. Sender: {sender?.GetType().Name ?? "unknown"}.";
                return false;
            }

            Thumbprint = certificate.GetCertHashString();
            if (!chain.ChainElements.Cast<X509ChainElement>().Any(element => element.Certificate.Thumbprint == intermediate.Thumbprint))
            {
                FailureReason = $"Server did not send the intermediate certificate. Received: {string.Join(", ", chain.ChainElements.Cast<X509ChainElement>().Select(element => element.Certificate.Subject))}.";
                return false;
            }

            using var verifiedChain = new X509Chain();
            verifiedChain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
            verifiedChain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
            verifiedChain.ChainPolicy.CustomTrustStore.Add(root);
            verifiedChain.ChainPolicy.ExtraStore.Add(intermediate);
            bool isTrusted = verifiedChain.Build(X509CertificateLoader.LoadCertificate(certificate.GetRawCertData()));
            FailureReason = isTrusted
                ? null
                : string.Join("; ", verifiedChain.ChainStatus.Select(status => $"{status.Status}: {status.StatusInformation}"));
            return isTrusted;
        }

    }
}
