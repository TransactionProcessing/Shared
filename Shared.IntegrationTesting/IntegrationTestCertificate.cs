using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Shared.IntegrationTesting
{
    public sealed class IntegrationTestCertificate : IDisposable
    {
        private const string PasswordValue = "password";
        private const string ServerCertificateFileName = "aspnetapp-web-api.pfx";
        private const string RootCertificateFileName = "aspnetapp-root-cert.cer";
        private const string ContainerCertificateDirectoryPath = "/app/test-certs";
        private const string RootSubjectName = "CN=SecurityServiceIntegrationTestRoot";
        private const string LocalhostName = "localhost";
        private const string LoopbackAddress = "127.0.0.1";

        private readonly string _certificateDirectory;
        private readonly X509Certificate2 _certificate;
        private bool _trustedRootInstalled;
        private bool _disposed;

        private IntegrationTestCertificate(string certificateDirectory, X509Certificate2 certificate)
        {
            _certificateDirectory = certificateDirectory;
            _certificate = certificate;
        }

        public string CertificateDirectory => _certificateDirectory;

        public string ContainerCertificateDirectory => ContainerCertificateDirectoryPath;

        public string Password => PasswordValue;

        public string RootCertificatePath => Path.Combine(_certificateDirectory, RootCertificateFileName);

        public string ServerCertificatePath => Path.Combine(_certificateDirectory, ServerCertificateFileName);

        public string Thumbprint => _certificate.Thumbprint ?? string.Empty;

        public static IntegrationTestCertificate Create()
        {
            string certificateDirectory = Path.Combine(Path.GetTempPath(), $"securityservice-cert-{Guid.NewGuid():N}");
            Directory.CreateDirectory(certificateDirectory);

            using RSA rsa = RSA.Create(2048);
            CertificateRequest request = new CertificateRequest(RootSubjectName, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
            request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, true));

            OidCollection enhancedKeyUsages = new OidCollection();
            enhancedKeyUsages.Add(new Oid("1.3.6.1.5.5.7.3.1"));
            request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(enhancedKeyUsages, false));

            SubjectAlternativeNameBuilder subjectAlternativeNames = new SubjectAlternativeNameBuilder();
            subjectAlternativeNames.AddDnsName(LocalhostName);
            subjectAlternativeNames.AddIpAddress(IPAddress.Parse(LoopbackAddress));
            request.CertificateExtensions.Add(subjectAlternativeNames.Build());

            request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));

            using X509Certificate2 certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(30));
            byte[] pfxBytes = certificate.Export(X509ContentType.Pfx, PasswordValue);
            byte[] certificateBytes = certificate.Export(X509ContentType.Cert);

            string serverCertificatePath = Path.Combine(certificateDirectory, ServerCertificateFileName);
            string rootCertificatePath = Path.Combine(certificateDirectory, RootCertificateFileName);
            File.WriteAllBytes(serverCertificatePath, pfxBytes);
            File.WriteAllBytes(rootCertificatePath, certificateBytes);

            var result = new IntegrationTestCertificate(certificateDirectory, X509CertificateLoader.LoadPkcs12(pfxBytes, PasswordValue, X509KeyStorageFlags.EphemeralKeySet));
            result.InstallTrustedRoot();
            return result;
        }

        public void InstallTrustedRoot()
        {
            ThrowIfDisposed();

            using X509Store store = new X509Store(StoreName.TrustedPeople, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadWrite);
            store.Add(_certificate);
            _trustedRootInstalled = true;
        }

        public string GetContainerCertificatePath()
        {
            return $"{ContainerCertificateDirectoryPath}/{ServerCertificateFileName}";
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            if (_trustedRootInstalled)
            {
                RemoveTrustedRoot();
            }

            if (Directory.Exists(_certificateDirectory))
            {
                Directory.Delete(_certificateDirectory, recursive: true);
            }

            _certificate.Dispose();
            _disposed = true;
        }

        private void RemoveTrustedRoot()
        {
            using X509Store store = new X509Store(StoreName.TrustedPeople, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadWrite);

            foreach (X509Certificate2 certificate in store.Certificates.Find(X509FindType.FindByThumbprint, Thumbprint, validOnly: false))
            {
                store.Remove(certificate);
            }

            _trustedRootInstalled = false;
        }

        private void ThrowIfDisposed()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
        }
    }
}
