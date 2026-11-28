using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;

namespace TransactionProcessing.CertificateDefaults.Internal;

internal sealed class KestrelCertificateSet : IDisposable
{
    private readonly List<X509Certificate2> certificates = [];
    private bool disposed;
    private static readonly TimeSpan PemRegexTimeout = TimeSpan.FromSeconds(1);

    public X509Certificate2? ServerCertificate { get; private set; }

    public X509Certificate2Collection Chain { get; } = [];

    public void Load(IConfigurationSection configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        string? path = configuration["Path"];
        string? password = configuration["Password"];
        string? keyPath = configuration["KeyPath"];

        if (string.IsNullOrWhiteSpace(path))
        {
            throw new InvalidOperationException(
                "HTTPS certificate configuration is required at ServiceOptions:KestrelOptions:Path.");
        }

        try
        {
            if (!string.IsNullOrWhiteSpace(keyPath))
            {
                if (!string.IsNullOrEmpty(password))
                {
                    throw new InvalidOperationException(
                        "ServiceOptions:KestrelOptions:Password is only valid for PFX certificates; omit it when using KeyPath.");
                }

                LoadPem(path, keyPath);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(password))
                {
                    throw new InvalidOperationException(
                        "A password is required for PFX certificates at ServiceOptions:KestrelOptions:Password.");
                }

                LoadPfx(path, password);
            }
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    public void Configure(KestrelServerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (ServerCertificate is null)
        {
            throw new InvalidOperationException(
                "ConfigureTransactionProcessingHttps must be applied before Kestrel starts.");
        }

        options.ConfigureHttpsDefaults(https =>
        {
            https.ServerCertificate = ServerCertificate;
            https.ServerCertificateChain = Chain;
        });
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        foreach (X509Certificate2 certificate in certificates)
        {
            certificate.Dispose();
        }

        certificates.Clear();
        Chain.Clear();
        ServerCertificate = null;
    }

    private void LoadPfx(string path, string password)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"The HTTPS PFX certificate file was not found: '{path}'.", path);
        }

        X509Certificate2Collection imported = X509CertificateLoader.LoadPkcs12CollectionFromFile(
            path,
            password,
            GetKeyStorageFlags());

        X509Certificate2[] serverCertificates = imported.Cast<X509Certificate2>()
            .Where(certificate => certificate.HasPrivateKey)
            .ToArray();

        if (serverCertificates.Length != 1)
        {
            foreach (X509Certificate2 certificate in imported)
            {
                certificate.Dispose();
            }

            throw new InvalidOperationException(
                $"The PFX at '{path}' must contain exactly one certificate with a private key.");
        }

        ServerCertificate = serverCertificates[0];
        certificates.Add(ServerCertificate);
        foreach (X509Certificate2 certificate in imported)
        {
            if (!ReferenceEquals(certificate, ServerCertificate))
            {
                Chain.Add(certificate);
                certificates.Add(certificate);
            }
        }
    }

    private void LoadPem(string certificatePath, string keyPath)
    {
        if (!File.Exists(certificatePath))
        {
            throw new FileNotFoundException($"The HTTPS PEM certificate file was not found: '{certificatePath}'.", certificatePath);
        }

        if (!File.Exists(keyPath))
        {
            throw new FileNotFoundException($"The HTTPS PEM private-key file was not found: '{keyPath}'.", keyPath);
        }

        string pemContent = File.ReadAllText(certificatePath);
        var imported = new X509Certificate2Collection();
        foreach (Match match in Regex.Matches(
            pemContent,
            "-----BEGIN CERTIFICATE-----\\s*(?<data>[A-Za-z0-9+/=\\s]+?)\\s*-----END CERTIFICATE-----",
            RegexOptions.CultureInvariant,
            PemRegexTimeout))
        {
            string base64 = Regex.Replace(
                match.Groups["data"].Value,
                "\\s",
                string.Empty,
                RegexOptions.CultureInvariant,
                PemRegexTimeout);
            imported.Add(X509CertificateLoader.LoadCertificate(Convert.FromBase64String(base64)));
        }

        X509Certificate2? serverCertificate = null;

        try
        {
            if (imported.Count == 0)
            {
                throw new InvalidOperationException(
                    $"The PEM certificate file '{certificatePath}' contains no CERTIFICATE blocks.");
            }

            serverCertificate = X509Certificate2.CreateFromPem(
                imported[0].ExportCertificatePem(),
                File.ReadAllText(keyPath));
            if (OperatingSystem.IsWindows())
            {
                string temporaryPassword = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
                byte[] temporaryPfx = serverCertificate.Export(X509ContentType.Pkcs12, temporaryPassword);
                X509Certificate2 persistedCertificate = X509CertificateLoader.LoadPkcs12(
                    temporaryPfx,
                    temporaryPassword,
                    X509KeyStorageFlags.EphemeralKeySet);
                serverCertificate.Dispose();
                serverCertificate = persistedCertificate;
            }
            if (!imported.Cast<X509Certificate2>().Any(certificate =>
                certificate.RawData.AsSpan().SequenceEqual(serverCertificate.RawData)))
            {
                throw new InvalidOperationException(
                    $"The PEM certificate at '{certificatePath}' does not contain the certificate matching '{keyPath}'.");
            }

            ServerCertificate = serverCertificate;
            certificates.Add(serverCertificate);
            foreach (X509Certificate2 certificate in imported)
            {
                if (certificate.RawData.AsSpan().SequenceEqual(serverCertificate.RawData))
                {
                    certificate.Dispose();
                    continue;
                }

                Chain.Add(certificate);
                certificates.Add(certificate);
            }
        }
        catch
        {
            serverCertificate?.Dispose();
            foreach (X509Certificate2 certificate in imported)
            {
                if (!certificates.Contains(certificate))
                {
                    certificate.Dispose();
                }
            }

            throw;
        }
    }

    private static X509KeyStorageFlags GetKeyStorageFlags() => X509KeyStorageFlags.EphemeralKeySet;
}
