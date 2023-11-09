namespace Shared.EventStore.Tests;

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Ductus.FluentDocker.Services;
using global::EventStore.Client;
using IntegrationTesting;
using Newtonsoft.Json;
using Shouldly;

public class EventStoreDockerHelper : DockerHelper
{
    public async Task StartContainers(Boolean isSecureEventStore, String testName) {
        this.IsSecureEventStore = isSecureEventStore;
        this.SetHostTraceFolder(testName);
        this.ScenarioName = testName;
        await this.StartContainersForScenarioRun(testName, DockerServices.EventStore);
    }

    public override async Task StartContainersForScenarioRun(String scenarioName, DockerServices services) {
        this.TestId = Guid.NewGuid();
        INetworkService networkService = this.SetupTestNetwork();
        this.SetupContainerNames();

        this.RequiredDockerServices = services;

        await this.StartContainer2(this.SetupEventStoreContainer,
                                  new List<INetworkService> {
                                                                networkService
                                                            },
                                  DockerServices.EventStore);

    }

    public EventStoreClientSettings CreateEventStoreClientSettings(Boolean secureEventStore, TimeSpan? deadline = null)
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
        settings.DefaultDeadline = deadline;
        
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