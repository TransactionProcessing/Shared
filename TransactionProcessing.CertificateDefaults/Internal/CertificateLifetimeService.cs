using Microsoft.Extensions.Hosting;

namespace TransactionProcessing.CertificateDefaults.Internal;

internal sealed class CertificateLifetimeService(
    IHostApplicationLifetime applicationLifetime,
    KestrelCertificateSet certificateSet) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        applicationLifetime.ApplicationStopped.Register(certificateSet.Dispose);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
