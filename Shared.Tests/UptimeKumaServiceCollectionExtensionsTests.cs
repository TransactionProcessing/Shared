using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shared.Monitoring;
using Shouldly;
using SimpleResults;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Xunit;

namespace Shared.Tests;

public sealed class UptimeKumaServiceCollectionExtensionsTests
{
    [Fact]
    public async Task AddUptimeKuma_BindsConfigurationAndRegistersClient()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["UptimeKuma:Enabled"] = "true",
                ["UptimeKuma:Server"] = "http://kuma.test:3001",
                ["UptimeKuma:Username"] = "monitor-user",
                ["UptimeKuma:Password"] = "secret"
            })
            .Build();

        await using var provider = new ServiceCollection()
            .AddUptimeKuma(configuration.GetSection("UptimeKuma"))
            .BuildServiceProvider();

        var client = provider.GetService<IUptimeKumaClient>();
        client.ShouldNotBeNull();
    }

    [Fact]
    public async Task AddUptimeKuma_WithoutSection_UsesUptimeKumaConfigurationSection()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["UptimeKuma:Enabled"] = "false",
                ["UptimeKuma:Server"] = "http://kuma.test:3001"
            })
            .Build();

        await using var provider = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddUptimeKuma()
            .BuildServiceProvider();

        var resolved = provider.GetRequiredService<UptimeKumaConfiguration>();

        resolved.Enabled.ShouldBeFalse();
        resolved.ServerAddress.ShouldBe("http://kuma.test:3001");
    }

    [Fact]
    public async Task RegisterWithUptimeKumaAsync_WhenConfigurationSectionIsMissing_DoesNothing()
    {
        var configuration = new ConfigurationBuilder().Build();
        using var host = new HostBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton<IConfiguration>(configuration);
                services.AddUptimeKuma();
            })
            .Build();

        await Should.NotThrowAsync(() => host.RegisterWithUptimeKumaAsync());
    }

    [Fact]
    public async Task AddUptimeKuma_WhenConfigurationSectionIsMissing_ResolvesDisabledClient()
    {
        var configuration = new ConfigurationBuilder().Build();
        await using var provider = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddUptimeKuma()
            .BuildServiceProvider();

        var client = provider.GetRequiredService<IUptimeKumaClient>();

        client.ShouldNotBeNull();
    }

    [Fact]
    public async Task AddUptimeKuma_BindsNestedServerAndMonitorSections()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["UptimeKuma:Server:Enabled"] = "true",
                ["UptimeKuma:Server:Address"] = "http://kuma.test:3001",
                ["UptimeKuma:Server:Username"] = "monitor-user",
                ["UptimeKuma:Server:Password"] = "secret",
                ["UptimeKuma:Monitor:Name"] = "My API",
                ["UptimeKuma:Monitor:Url"] = "https://api.test/health",
                ["UptimeKuma:Monitor:IntervalInSeconds"] = "30"
            })
            .Build();

        await using var provider = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddUptimeKuma()
            .BuildServiceProvider();

        var server = provider.GetRequiredService<UptimeKumaConfiguration>();
        var monitor = provider.GetRequiredService<UptimeKumaMonitor>();

        server.Enabled.ShouldBeTrue();
        server.ServerAddress.ShouldBe("http://kuma.test:3001");
        monitor.ShouldBe(new UptimeKumaMonitor("My API", "https://api.test/health", 30));
    }

    [Fact]
    public async Task RegisterWithUptimeKumaAsync_WithoutMonitor_UsesConfiguredMonitor()
    {
        var fake = new RecordingUptimeKumaClient();
        using var host = new HostBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton<IUptimeKumaClient>(fake);
                services.AddSingleton(new UptimeKumaConfiguration(
                    true, "http://kuma.test:3001", "user", "password"));
                services.AddSingleton(new UptimeKumaMonitor(
                    "Configured API", "https://api.test/health", 30));
            })
            .Build();

        await host.RegisterWithUptimeKumaAsync();

        fake.LastMonitor.ShouldBe(new UptimeKumaMonitor(
            "Configured API", "https://api.test/health", 30));
    }

    [Fact]
    public async Task RegisterWithUptimeKumaAsync_WhenDisabled_DoesNotResolveConfiguredMonitor()
    {
        var fake = new RecordingUptimeKumaClient();
        using var host = new HostBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton<IUptimeKumaClient>(fake);
                services.AddSingleton(new UptimeKumaConfiguration(
                    false, "http://kuma.test:3001", "user", "password"));
            })
            .Build();

        await host.RegisterWithUptimeKumaAsync();

        fake.CallCount.ShouldBe(0);
    }

    [Fact]
    public async Task RegisterWithUptimeKumaAsync_WhenDisabled_DoesNotCallClient()
    {
        var fake = new RecordingUptimeKumaClient();
        using var host = new HostBuilder()
            .ConfigureServices((System.Action<IServiceCollection>)(services =>
            {
                services.AddSingleton<IUptimeKumaClient>(fake);
                services.AddSingleton(new UptimeKumaConfiguration(
                    false, "http://kuma.test:3001", "user", "password"));
            }))
            .Build();

        await host.RegisterWithUptimeKumaAsync(
            new UptimeKumaMonitor("API", "https://api.test/health"));

        fake.CallCount.ShouldBe(0);
    }

    private sealed class RecordingUptimeKumaClient : IUptimeKumaClient
    {
        public int CallCount { get; private set; }

        public UptimeKumaMonitor? LastMonitor { get; private set; }

        public Task<Result<int>> RegisterMonitor(
            UptimeKumaMonitor monitorToAdd,
            CancellationToken cancellationToken)
        {
            CallCount++;
            LastMonitor = monitorToAdd;
            return Task.FromResult(Result.Success(1));
        }
    }
}
