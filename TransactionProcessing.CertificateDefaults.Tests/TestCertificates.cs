using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace TransactionProcessing.CertificateDefaults.Tests;

internal sealed class TestCertificates : IDisposable
{
    private readonly RSA rootKey = RSA.Create(2048);
    private readonly RSA intermediateKey = RSA.Create(2048);
    private readonly RSA leafKey = RSA.Create(2048);
    private bool disposed;

    public TestCertificates()
    {
        Password = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        DateTimeOffset notBefore = DateTimeOffset.UtcNow.AddMinutes(-5);
        DateTimeOffset notAfter = DateTimeOffset.UtcNow.AddDays(2);

        var rootRequest = CreateCaRequest("CN=Test Root", rootKey);
        Root = rootRequest.CreateSelfSigned(notBefore, notAfter);

        var intermediateRequest = CreateCaRequest("CN=Test Intermediate", intermediateKey);
        X509Certificate2 intermediatePublic = intermediateRequest.Create(
            Root,
            notBefore,
            notAfter,
            RandomNumberGenerator.GetBytes(16));
        Intermediate = intermediatePublic.CopyWithPrivateKey(intermediateKey);

        var leafRequest = new CertificateRequest("CN=localhost", leafKey, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var san = new SubjectAlternativeNameBuilder();
        san.AddDnsName("localhost");
        san.AddIpAddress(IPAddress.Loopback);
        leafRequest.CertificateExtensions.Add(san.Build());
        leafRequest.CertificateExtensions.Add(new X509KeyUsageExtension(
            X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment,
            critical: true));
        var eku = new OidCollection { new("1.3.6.1.5.5.7.3.1") };
        leafRequest.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(eku, critical: false));
        X509Certificate2 leafPublic = leafRequest.Create(
            Intermediate,
            notBefore,
            notAfter,
            RandomNumberGenerator.GetBytes(16));
        Leaf = leafPublic.CopyWithPrivateKey(leafKey);

        string directory = Path.Combine(Path.GetTempPath(), "transaction-processing-certificate-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        PfxPath = Path.Combine(directory, "server.pfx");
        FullChainPath = Path.Combine(directory, "fullchain.pem");
        PrivateKeyPath = Path.Combine(directory, "server.key");
        var pfxCertificates = new X509Certificate2Collection
        {
            Leaf,
            X509CertificateLoader.LoadCertificate(Intermediate.RawData)
        };
        File.WriteAllBytes(PfxPath, pfxCertificates.Export(X509ContentType.Pkcs12, Password)!);
        foreach (X509Certificate2 certificate in pfxCertificates.Skip(1))
        {
            certificate.Dispose();
        }
        File.WriteAllText(FullChainPath, Leaf.ExportCertificatePem() + Intermediate.ExportCertificatePem());
        File.WriteAllText(PrivateKeyPath, leafKey.ExportPkcs8PrivateKeyPem());
    }

    public X509Certificate2 Root { get; }
    public X509Certificate2 Intermediate { get; }
    public X509Certificate2 Leaf { get; }
    public string PfxPath { get; }
    public string FullChainPath { get; }
    public string PrivateKeyPath { get; }
    public string Password { get; }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        Root.Dispose();
        Intermediate.Dispose();
        Leaf.Dispose();
        rootKey.Dispose();
        intermediateKey.Dispose();
        leafKey.Dispose();

        string directory = Path.GetDirectoryName(PfxPath)!;
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static CertificateRequest CreateCaRequest(string subject, RSA key)
    {
        var request = new CertificateRequest(subject, key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(certificateAuthority: true, hasPathLengthConstraint: false, pathLengthConstraint: 0, critical: true));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(
            X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign | X509KeyUsageFlags.DigitalSignature,
            critical: true));
        request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, critical: false));
        return request;
    }
}
