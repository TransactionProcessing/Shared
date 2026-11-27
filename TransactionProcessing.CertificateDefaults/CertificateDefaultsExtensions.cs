using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TransactionProcessing.CertificateDefaults.Internal;

namespace TransactionProcessing.CertificateDefaults;

/// <summary>
/// Configures shared HTTPS certificate defaults for ASP.NET Core hosts.
/// </summary>
public static class CertificateDefaultsExtensions
{
    /// <summary>
    /// Configures Kestrel to use the service certificate from
    /// <c>ServiceOptions:KestrelOptions</c>.
    /// </summary>
    /// <remarks>
    /// Set <c>Path</c> and <c>Password</c> for a password-protected PFX, or
    /// <c>Path</c> and <c>KeyPath</c> for a PEM full chain and separate private key.
    /// </remarks>
    public static IWebHostBuilder ConfigureTransactionProcessingHttps(this IWebHostBuilder webHostBuilder)
    {
        ArgumentNullException.ThrowIfNull(webHostBuilder);

        var certificateSet = new KestrelCertificateSet();
        webHostBuilder.ConfigureServices((context, services) =>
        {
            certificateSet.Load(context.Configuration.GetSection("ServiceOptions:KestrelOptions"));
            services.AddSingleton<IHostedService>(serviceProvider =>
                new CertificateLifetimeService(serviceProvider.GetRequiredService<IHostApplicationLifetime>(), certificateSet));
        });
        webHostBuilder.ConfigureKestrel((_, options) =>
        {
            certificateSet.Configure(options);
        });

        return webHostBuilder;
    }
}
