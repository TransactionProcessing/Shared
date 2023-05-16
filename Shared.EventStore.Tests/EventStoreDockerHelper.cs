namespace Shared.EventStore.Tests;

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Ductus.FluentDocker.Services;
using global::EventStore.Client;
using IntegrationTesting;

public class EventStoreDockerHelper : DockerHelper
{
    public async Task StartContainers(Boolean isSecureEventStore) {
        this.IsSecureEventStore = isSecureEventStore;
        this.SetHostTraceFolder("");
        await this.StartContainersForScenarioRun("");
    }

    public override async Task StartContainersForScenarioRun(String scenarioName) {
        this.TestId = Guid.NewGuid();
        INetworkService networkService = this.SetupTestNetwork();
        this.SetupContainerNames();
        await this.StartContainer(this.SetupEventStoreContainer,
                                  new List<INetworkService> {
                                                                networkService
                                                            });
    }

    public EventStoreClientSettings CreateEventStoreClientSettings(Boolean secureEventStore)
    {
        String connectionString = secureEventStore switch
        {
            true => $"esdb://admin:changeit@127.0.0.1:{this.EventStoreHttpPort}?tls=true&tlsVerifyCert=false",
            _ => $"esdb://admin:changeit@127.0.0.1:{this.EventStoreHttpPort}?tls=false"
        };

        EventStoreClientSettings settings = EventStoreClientSettings.Create(connectionString);
        settings.ConnectivitySettings.Insecure = secureEventStore switch
        {
            true => false,
            _ => true
        };
            
        if (secureEventStore == false)
        {
            settings.CreateHttpMessageHandler = () => new SocketsHttpHandler
                                                      {
                                                          SslOptions = {
                                                                           RemoteCertificateValidationCallback = (sender,
                                                                                                                  certificate,
                                                                                                                  chain,
                                                                                                                  errors) => true,
                                                                       }
                                                      };
        }

        return settings;
    }
}