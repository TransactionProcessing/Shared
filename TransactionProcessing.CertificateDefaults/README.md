# TransactionProcessing.CertificateDefaults

Shared Kestrel HTTPS certificate configuration for Transaction Processing ASP.NET Core services targeting .NET 11.

## Setup

Install the package, configure the certificate through the deployment configuration provider, and enable the Kestrel extension:

~~~csharp
using TransactionProcessing.CertificateDefaults;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureTransactionProcessingHttps();

builder.Services.AddHttpClient("SecurityService", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["SecurityService:BaseAddress"]!);
});

var app = builder.Build();
app.MapGet("/health", () => Results.Ok());
app.Run();
~~~

The extension reads ServiceOptions:KestrelOptions:

| Configuration key | PFX | PEM |
| --- | --- | --- |
| Path | Password-protected PFX path | Full chain PEM path (leaf first, followed by intermediates) |
| Password | Required PFX password | Omit |
| KeyPath | Omit | Separate PEM private-key path |

Configure listener URLs through ASPNETCORE_URLS or Kestrel endpoint configuration. The extension fails startup if certificate configuration is missing or invalid.

## Secret handling

Use a deployment secret provider for the PFX password. The default .NET environment configuration maps ServiceOptions__KestrelOptions__Password to the password key. Mount certificate files read-only and grant access only to the service identity. PEM private keys must be unencrypted and protected by operating-system file permissions. Do not commit credentials, certificates, or private keys.

PFX files must contain exactly one certificate with a private key and include any intermediate certificates that the server must present. PEM files must contain the leaf certificate first and the intermediate certificates after it; KeyPath must identify the leaf's matching private key. Do not append the root certificate to the served chain.

## Outbound clients and trust

Use ordinary IHttpClientFactory registration, such as AddHttpClient. The client uses the operating system's certificate trust and hostname validation. Install the issuing CA root in the host or container trust store before connecting. Do not set ServerCertificateCustomValidationCallback to bypass validation.

## Rotation

After renewing a certificate, atomically replace the deployed certificate files, update the password secret if needed, and restart the service. The new certificate is loaded when Kestrel starts. Live certificate reload is not provided by this package.

A minimal runnable host is in TransactionProcessing.CertificateDefaults.Sample.
