using System.Diagnostics;
using System.Threading;
using KurrentDB.Client;
using Shared.IntegrationTesting;
using Shared.IntegrationTesting.TestContainers;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;

namespace Shared.EventStoreContext.Tests;

public class EventStoreDockerHelper : Shared.IntegrationTesting.TestContainers.DockerHelper
{
    public async Task StartContainers(
        Boolean isSecureEventStore,
        String testName,
        CancellationToken cancellationToken)
    {
        this.SetHostTraceFolder(testName);
        this.ScenarioName = testName;
        await this.StartContainersForScenarioRun(testName, DockerServices.KurrentDb, cancellationToken);
    }

    public override async Task StopContainersForScenarioRun(DockerServices sharedDockerServices)
    {
        if (this.Containers.Any())
        {
            this.Containers.Reverse();

            foreach ((DockerServices, IContainer) containerService in this.Containers)
            {
                this.Trace($"Stopping container [{containerService.Item2.Name}]");
                await containerService.Item2.StopAsync(CancellationToken.None);
                await containerService.Item2.DisposeAsync();
                this.Trace($"Container [{containerService.Item2.Name}] stopped");
            }
        }

        if (this.TestNetworks.Any())
        {
            foreach (INetwork networkService in this.TestNetworks)
            {
                await networkService.DeleteAsync(CancellationToken.None);
                await networkService.DisposeAsync();
            }
        }
    }

    public override async Task CreateSubscriptions(CancellationToken cancellationToken)
    {
        // Nothing actually needed here
    }

    public override async Task StartContainersForScenarioRun(
        String scenarioName,
        DockerServices services,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        this.TestId = Guid.NewGuid();
        String networkName = $"eventstoretestnetwork{this.TestId:N}";
        INetwork networkService = await this.SetupTestNetwork(networkName, true);
        this.SetupContainerNames();

        this.RequiredDockerServices = services;

        ContainerBuilder SetupSecureEventStoreContainerLocal()
        {
            this.IsSecureKurrentDb = true;

            this.KurrentDbContainerName = "UnitTestKurrentDb_Secure";

            return this.SetupKurrentDbContainer().WithReuse(true);
        }

        ContainerBuilder SetupInsecureEventStoreContainerLocal()
        {
            this.IsSecureKurrentDb = false;

            this.KurrentDbContainerName = "UnitTestKurrentDb_Insecure";

            return this.SetupKurrentDbContainer().WithReuse(true);
        }

        await this.StartContainer2(SetupSecureEventStoreContainerLocal,
                                  new List<INetwork> {
                                                                networkService
                                                            },
                                  DockerServices.KurrentDb,
                                  cancellationToken);

        await this.StartContainer2(SetupInsecureEventStoreContainerLocal,
            new List<INetwork> {
                networkService
            },
            DockerServices.KurrentDb,
            cancellationToken);
    }

    public KurrentDBClientSettings CreateEventStoreClientSettings(Boolean secureEventStore, TimeSpan? deadline = null, String userName = "admin", String password = "changeit")
    {
        String connectionString = secureEventStore switch
        {
            true => $"esdb://{userName}:{password}@127.0.0.1:{this.KurrentDbSecureHttpPort}?tls=true&tlsVerifyCert=false",
            _ => $"esdb://{userName}:{password}@127.0.0.1:{this.KurrentDbHttpPort}?tls=false"
        };

        KurrentDBClientSettings settings = KurrentDBClientSettings.Create(connectionString);
        settings.ConnectivitySettings.Insecure = secureEventStore switch
        {
            true => false,
            _ => true
        };
        settings.DefaultDeadline = deadline;

        if (!secureEventStore)
        {
            settings.CreateHttpMessageHandler = () => new SocketsHttpHandler
            {
                SslOptions =
                {
                    RemoteCertificateValidationCallback = (_, _, _, _) => true,
                }
            };
        }

        return settings;
    }

    public Task PauseKurrentDbContainer()
    {
        return this.RunDockerCommand($"pause {this.KurrentDbContainerName}");
    }

    public Task UnpauseKurrentDbContainer()
    {
        return this.RunDockerCommand($"unpause {this.KurrentDbContainerName}");
    }

    public Task StopEventStoreContainer()
    {
        return this.RunDockerCommand($"stop -t 0 {this.KurrentDbContainerName}");
    }

    public Task StartKurrentDbContainer()
    {
        return this.RunDockerCommand($"start {this.KurrentDbContainerName}");
    }

    private async Task RunDockerCommand(String arguments)
    {
        ProcessStartInfo startInfo = new()
        {
            FileName = "docker",
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using Process process = Process.Start(startInfo) ?? throw new InvalidOperationException("Unable to start docker command.");
        String standardOutput = await process.StandardOutput.ReadToEndAsync();
        String standardError = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"Docker command failed: {arguments}. Stdout: {standardOutput}. Stderr: {standardError}");
        }
    }
}
