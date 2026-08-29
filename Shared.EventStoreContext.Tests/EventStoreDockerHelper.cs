using System.Diagnostics;
using KurrentDB.Client;
using Shared.IntegrationTesting;
using Shared.IntegrationTesting.TestContainers;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;

namespace Shared.EventStoreContext.Tests;

public class EventStoreDockerHelper : Shared.IntegrationTesting.TestContainers.DockerHelper
{
    public async Task StartContainers(Boolean isSecureEventStore, String testName)
    {
        this.SetHostTraceFolder(testName);
        this.ScenarioName = testName;
        await this.StartContainersForScenarioRun(testName, DockerServices.EventStore);
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

    public override async Task CreateSubscriptions()
    {
        // Nothing actually needed here
    }

    public override async Task StartContainersForScenarioRun(String scenarioName, DockerServices services)
    {
        this.TestId = Guid.NewGuid();
        String networkName = $"eventstoretestnetwork{this.TestId:N}";
        INetwork networkService = await this.SetupTestNetwork(networkName, true);
        this.SetupContainerNames();

        this.RequiredDockerServices = services;

        ContainerBuilder SetupSecureEventStoreContainerLocal()
        {
            this.IsSecureEventStore = true;

            this.EventStoreContainerName = "UnitTestEventStore_Secure";

            return this.SetupEventStoreContainer().WithReuse(true);
        }

        ContainerBuilder SetupInsecureEventStoreContainerLocal()
        {
            this.IsSecureEventStore = false;

            this.EventStoreContainerName = "UnitTestEventStore_Insecure";

            return this.SetupEventStoreContainer().WithReuse(true);
        }

        await this.StartContainer2(SetupSecureEventStoreContainerLocal,
                                  new List<INetwork> {
                                                                networkService
                                                            },
                                  DockerServices.EventStore);

        await this.StartContainer2(SetupInsecureEventStoreContainerLocal,
            new List<INetwork> {
                networkService
            },
            DockerServices.EventStore);
    }

    public KurrentDBClientSettings CreateEventStoreClientSettings(Boolean secureEventStore, TimeSpan? deadline = null, String userName = "admin", String password = "changeit")
    {
        String connectionString = secureEventStore switch
        {
            true => $"esdb://{userName}:{password}@127.0.0.1:{this.EventStoreSecureHttpPort}?tls=true&tlsVerifyCert=false",
            _ => $"esdb://{userName}:{password}@127.0.0.1:{this.EventStoreHttpPort}?tls=false"
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

    public Task PauseEventStoreContainer()
    {
        return this.RunDockerCommand($"pause {this.EventStoreContainerName}");
    }

    public Task UnpauseEventStoreContainer()
    {
        return this.RunDockerCommand($"unpause {this.EventStoreContainerName}");
    }

    public Task StopEventStoreContainer()
    {
        return this.RunDockerCommand($"stop -t 0 {this.EventStoreContainerName}");
    }

    public Task StartEventStoreContainer()
    {
        return this.RunDockerCommand($"start {this.EventStoreContainerName}");
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
