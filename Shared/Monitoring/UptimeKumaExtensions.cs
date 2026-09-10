using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SocketIOClient;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Shared.Monitoring;

public static class UptimeKumaExtensions
{
    public static IServiceCollection AddUptimeKuma(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<UptimeKumaConfiguration>(serviceProvider =>
            BindConfiguration(serviceProvider
                .GetRequiredService<IConfiguration>()
                .GetSection("UptimeKuma")));
        services.AddSingleton<UptimeKumaMonitor>(serviceProvider =>
            BindMonitor(serviceProvider
                .GetRequiredService<IConfiguration>()
                .GetSection("UptimeKuma")));

        return services.AddUptimeKumaClient();
    }

    public static IServiceCollection AddUptimeKuma(
        this IServiceCollection services,
        IConfigurationSection configurationSection)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configurationSection);

        services.AddSingleton(_ => BindConfiguration(configurationSection));
        services.AddSingleton(_ => BindMonitor(configurationSection));

        return services.AddUptimeKumaClient();
    }
    
    private static IServiceCollection AddUptimeKumaClient(this IServiceCollection services)
    {
        services.AddSingleton<UptimeKumaClient>(serviceProvider =>
        {
            var configuration = serviceProvider.GetRequiredService<UptimeKumaConfiguration>();
            var socket = new SocketIO(new Uri(configuration.ServerAddress));
            return new UptimeKumaClient(socket, configuration);
        });
        services.AddSingleton<IUptimeKumaClient>(serviceProvider =>
            serviceProvider.GetRequiredService<UptimeKumaClient>());

        return services;
    }

    public static Task RegisterWithUptimeKumaAsync(
        this IHost host,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(host);

        return host.RegisterWithUptimeKumaAsync(
            host.Services.GetRequiredService<UptimeKumaMonitor>(),
            cancellationToken);
    }

    public static async Task RegisterWithUptimeKumaAsync(
        this IHost host,
        UptimeKumaMonitor monitor,
        CancellationToken cancellationToken = default)
    {
        var configuration = host.Services.GetService<UptimeKumaConfiguration>();
        if (configuration is null || !configuration.Enabled)
        {
            return;
        }

        var logger = host.Services
            .GetService<ILoggerFactory>()?
            .CreateLogger("Shared.Monitoring.UptimeKuma");

        try
        {
            await host.Services
                .GetRequiredService<IUptimeKumaClient>()
                .RegisterMonitor(monitor, cancellationToken);

            logger?.LogInformation("Registered Uptime Kuma monitor {MonitorName}.", monitor.Name);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger?.LogWarning(
                exception,
                "Unable to register Uptime Kuma monitor {MonitorName}. Application startup will continue.",
                monitor.Name);
        }
    }

    private static UptimeKumaConfiguration BindConfiguration(IConfigurationSection section)
    {
        var server = section.GetSection("Server");
        var serverAddress = server["Address"]
            ?? server["ServerAddress"]
            ?? section["ServerAddress"]
            ?? section["Server"];

        if (string.IsNullOrWhiteSpace(serverAddress))
        {
            throw new InvalidOperationException(
                "Uptime Kuma configuration requires either 'ServerAddress' or 'Server'.");
        }

        return new UptimeKumaConfiguration(
            server.GetValue("Enabled", section.GetValue("Enabled", false)),
            serverAddress,
            server["Username"] ?? section["Username"] ?? string.Empty,
            server["Password"] ?? section["Password"] ?? string.Empty);
    }

    private static UptimeKumaMonitor BindMonitor(IConfigurationSection section)
    {
        var monitor = section.GetSection("Monitor");
        var name = monitor["Name"] ?? section["MonitorName"];
        var url = monitor["Url"] ?? section["MonitorUrl"];

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(url))
        {
            throw new InvalidOperationException(
                "Uptime Kuma configuration requires Monitor:Name and Monitor:Url.");
        }

        return new UptimeKumaMonitor(
            name,
            url,
            monitor.GetValue(
                "IntervalInSeconds",
                section.GetValue("IntervalInSeconds", 60)));
    }
}
