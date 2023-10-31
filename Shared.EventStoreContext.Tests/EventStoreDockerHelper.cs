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
        
        await this.StartContainersForScenarioRun(testName, DockerServices.EventStore);

        String url = isSecureEventStore switch {
            true => $"https://127.0.0.1:{this.EventStoreHttpPort}/ping",
            _ => $"http://127.0.0.1:{this.EventStoreHttpPort}/ping"
        };

        HttpClientHandler handler = new HttpClientHandler{
                                                             ServerCertificateCustomValidationCallback = (sender,
                                                                                                          certificate,
                                                                                                          chain,
                                                                                                          errors) => true,
                                                         };

        await Retry.For(async () => {
                            HttpResponseMessage response = null;
                            try{
                                HttpClient client = new HttpClient(handler);
                                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
                                response = await client.SendAsync(request, CancellationToken.None);

                                var responseContent = await response.Content.ReadAsStringAsync(CancellationToken.None);

                                var responseData = new{
                                                          text = String.Empty
                                                      };

                                var x = JsonConvert.DeserializeAnonymousType(responseContent, responseData);
                                x.text.ShouldBe("Ping request successfully handled");
                            }
                            catch(Exception e){
                                if (response != null){
                                    if (response.IsSuccessStatusCode == false){
                                        Console.WriteLine(response.StatusCode);
                                    }
                                }

                                throw;
                            }
                        });
    }

    public override async Task StartContainersForScenarioRun(String scenarioName, DockerServices services) {
        this.TestId = Guid.NewGuid();
        INetworkService networkService = this.SetupTestNetwork();
        this.SetupContainerNames();

        this.RequiredDockerServices = services;

        await this.StartContainer(this.SetupEventStoreContainer,
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