using System.Diagnostics;
using Ductus.FluentDocker.Builders;
using Ductus.FluentDocker.Services;
using KurrentDB.Client;
using Shared.IntegrationTesting;
using Shared.IntegrationTesting.Ductus;

namespace Shared.EventStoreContext.Tests;

public class EventStoreDockerHelper : DockerHelper
{
    public async Task StartContainers(Boolean isSecureEventStore, String testName)
    {
        //this.IsSecureEventStore = isSecureEventStore;
        this.SetHostTraceFolder(testName);
        this.ScenarioName = testName;
        await this.StartContainersForScenarioRun(testName, DockerServices.EventStore);
    }

    public override async Task StopContainersForScenarioRun(DockerServices sharedDockerServices)
    {
        if (this.Containers.Any())
        {
            this.Containers.Reverse();

            foreach ((DockerServices, IContainerService) containerService in this.Containers)
            {
                this.Trace($"Stopping container [{containerService.Item2.Name}]");
                containerService.Item2.Stop();
                containerService.Item2.Remove(true);
                this.Trace($"Container [{containerService.Item2.Name}] stopped");
            }
        }

        if (this.TestNetworks.Any())
        {
            foreach (INetworkService networkService in this.TestNetworks)
            {
                networkService.Stop();
                networkService.Remove(true);
            }
        }
    }

    public override async Task CreateSubscriptions()
    {
        // Nothing actually needed here
    }

    public override async Task StartContainersForScenarioRun(String scenarioName, DockerServices services)
    {
        this.DockerPlatform = BaseDockerHelper.GetDockerEnginePlatform().Data;
        this.TestId = Guid.NewGuid();
        INetworkService networkService = this.SetupTestNetwork("eventstoretestnetwork", true);
        this.SetupContainerNames();

        this.RequiredDockerServices = services;

        ContainerBuilder SetupSecureEventStoreContainerLocal()
        {
            this.IsSecureEventStore = true;

            this.EventStoreContainerName = "UnitTestEventStore_Secure";

            return this.SetupEventStoreContainer().ReuseIfExists();
        }

        ContainerBuilder SetupInsecureEventStoreContainerLocal()
        {
            this.IsSecureEventStore = false;

            this.EventStoreContainerName = "UnitTestEventStore_Insecure";

            return this.SetupEventStoreContainer().ReuseIfExists();
        }

        await this.StartContainer2(SetupSecureEventStoreContainerLocal,
                                  new List<INetworkService> {
                                                                networkService
                                                            },
                                  DockerServices.EventStore);

        await this.StartContainer2(SetupInsecureEventStoreContainerLocal,
            new List<INetworkService> {
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
        return this.RunTransportOutageCommand($"pause {this.EventStoreContainerName}", $"stop -t 0 {this.EventStoreContainerName}");
    }

    public Task UnpauseEventStoreContainer()
    {
        return this.RunTransportRecoveryCommand($"unpause {this.EventStoreContainerName}", $"start {this.EventStoreContainerName}");
    }

    public Task StopEventStoreContainer()
    {
        return this.RunDockerCommand($"stop -t 0 {this.EventStoreContainerName}");
    }

    public Task StartEventStoreContainer()
    {
        return this.RunDockerCommand($"start {this.EventStoreContainerName}");
    }

    public async Task RestartEventStoreContainer()
    {
        await this.StartEventStoreContainer();
    }

    private Task RunTransportOutageCommand(String linuxCommand, String windowsCommand)
    {
        return this.IsWindowsContainerHost() ? this.RunDockerCommand(windowsCommand) : this.RunDockerCommand(linuxCommand);
    }

    private Task RunTransportRecoveryCommand(String linuxCommand, String windowsCommand)
    {
        return this.IsWindowsContainerHost() ? this.RunDockerCommand(windowsCommand) : this.RunDockerCommand(linuxCommand);
    }

    private Boolean IsWindowsContainerHost()
    {
        return this.DockerPlatform == DockerEnginePlatform.Windows;
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
