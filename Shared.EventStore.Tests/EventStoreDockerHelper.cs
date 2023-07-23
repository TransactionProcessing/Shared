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
    public async Task StartContainers(Boolean isSecureEventStore) {
        this.IsSecureEventStore = isSecureEventStore;
        this.SetHostTraceFolder("");
        await this.StartContainersForScenarioRun("");

        String url = isSecureEventStore switch {
            true => $"https://127.0.0.1:{this.EventStoreHttpPort}/ping",
            _ => $"http://127.0.0.1:{this.EventStoreHttpPort}/ping"
        };
        await Retry.For(async () => {
                            HttpClient client = new HttpClient();
                            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
                            var response = await client.SendAsync(request, CancellationToken.None);
                            var responseContent = await response.Content.ReadAsStringAsync(CancellationToken.None);

                            var responseData = new{
                                                      text = String.Empty
                                                  };

                            var x = JsonConvert.DeserializeAnonymousType(responseContent, responseData);
                            x.text.ShouldBe("Ping request successfully handled");
                        });
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