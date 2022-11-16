﻿namespace Shared.EventStore.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Aggregate;
    using DomainDrivenDesign.EventSourcing;
    using Ductus.FluentDocker.Builders;
    using Ductus.FluentDocker.Model.Builders;
    using Ductus.FluentDocker.Services;
    using Ductus.FluentDocker.Services.Extensions;
    using EventStore;
    using global::EventStore.Client;
    using Shouldly;
    using Xunit;

    public class EventStoreContextTests : IDisposable
    {
        #region Fields

        private readonly List<IContainerService> Containers = new List<IContainerService>();

        private Int32 EventStoreHttpPort;

        private readonly List<INetworkService> TestNetworks = new List<INetworkService>();

        #endregion

        #region Methods

        public void Dispose() {
            this.StopContainers();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task EventStoreContext_InsertEvents_EventsAreWritten(Boolean secureEventStore) {
            this.StartContainers(secureEventStore);

            await Task.Delay(TimeSpan.FromSeconds(30));

            EventStoreClientSettings settings = this.CreateEventStoreClientSettings(secureEventStore);

            EventStoreClient client = new(settings);
            EventStoreProjectionManagementClient projectionManagementClient = new(settings);
            IEventStoreContext context = new EventStoreContext(client, projectionManagementClient);

            Guid aggreggateId = Guid.NewGuid();
            String streamName = $"TestStream-{aggreggateId:N}";

            EstateCreatedEvent event1 = new(aggreggateId, "Test Estate 1");
            List<IDomainEvent> domainEvents = new();
            domainEvents.Add(event1);

            IEventDataFactory factory = new EventDataFactory();
            EventData[] events = factory.CreateEventDataList(domainEvents);
            Should.NotThrow(async () => { await context.InsertEvents(streamName, -1, events.ToList(), CancellationToken.None); });
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task EventStoreContext_ReadEvents_EventsAreRead(Boolean secureEventStore) {
            this.StartContainers(secureEventStore);

            await Task.Delay(TimeSpan.FromSeconds(30));

            EventStoreClientSettings settings = this.CreateEventStoreClientSettings(secureEventStore);
            
            EventStoreClient client = new(settings);
            EventStoreProjectionManagementClient projectionManagementClient = new(settings);
            IEventStoreContext context = new EventStoreContext(client, projectionManagementClient);

            Guid aggreggateId = Guid.NewGuid();
            String streamName = $"TestStream-{aggreggateId:N}";

            EstateCreatedEvent event1 = new(aggreggateId, "Test Estate 1");
            EstateCreatedEvent event2 = new(aggreggateId, "Test Estate 2");
            List<IDomainEvent> domainEvents = new();
            domainEvents.Add(event1);
            domainEvents.Add(event2);

            IEventDataFactory factory = new EventDataFactory();
            EventData[] events = factory.CreateEventDataList(domainEvents);
            await context.InsertEvents(streamName, -1, events.ToList(), CancellationToken.None);

            Should.NotThrow(async () => {
                                List<ResolvedEvent> resolvedEvents = await context.ReadEvents(streamName, 0, CancellationToken.None);

                                resolvedEvents.Count.ShouldBe(events.Length);
                            });
        }

        private EventStoreClientSettings CreateEventStoreClientSettings(Boolean secureEventStore) {
            String connectionString = secureEventStore switch {
                true => $"esdb://admin:changeit@127.0.0.1:{this.EventStoreHttpPort}?tls=true&tlsVerifyCert=false",
                _ => $"esdb://admin:changeit@127.0.0.1:{this.EventStoreHttpPort}?tls=false"
            };

            EventStoreClientSettings settings = EventStoreClientSettings.Create(connectionString);
            settings.ConnectivitySettings.Insecure = secureEventStore switch {
                true => false,
                _ => true
            };

            if (secureEventStore == false) {
                settings.CreateHttpMessageHandler = () => new SocketsHttpHandler {
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

        private INetworkService SetupTestNetwork(String networkName = null,
                                                 Boolean reuseIfExists = false) {
            networkName = String.IsNullOrEmpty(networkName) ? $"testnetwork{Guid.NewGuid()}" : networkName;

            // Build a network
            NetworkBuilder networkService = new Builder().UseNetwork(networkName);

            if (reuseIfExists) {
                networkService.ReuseIfExist();
            }

            return networkService.Build();
        }

        private void StartContainers(Boolean isSecureEventStore) {
            INetworkService networkService = this.SetupTestNetwork($"testNetwork-{Guid.NewGuid():N}", true);
            this.TestNetworks.Add(networkService);

            IContainerService containerService = this.StartEventStoreContainer("eventstore/eventstore:22.6.0-bionic",
                                                                               $"eventStore-{Guid.NewGuid():N}",
                                                                               isSecureEventStore,
                                                                               networkService);
            this.Containers.Add(containerService);
            this.EventStoreHttpPort = containerService.ToHostExposedEndpoint($"{EventStoreContextTests.EventStoreHttpDockerPort}/tcp").Port;
        }

        private IContainerService StartEventStoreContainer(String imageName,
                                                           String containerName,
                                                           Boolean isSecureContainer,
                                                           INetworkService networkService,
                                                           Boolean forceLatestImage = false) {
            List<String> environmentVariables = new List<String>();
            environmentVariables.Add("EVENTSTORE_RUN_PROJECTIONS=all");
            environmentVariables.Add("EVENTSTORE_START_STANDARD_PROJECTIONS=true");
            environmentVariables.Add("EVENTSTORE_ENABLE_ATOM_PUB_OVER_HTTP=true");
            environmentVariables.Add("EVENTSTORE_ENABLE_EXTERNAL_TCP=true");

            ContainerBuilder eventStoreContainerBuilder = new Builder().UseContainer().UseImage(imageName, forceLatestImage)
                                                                       .ExposePort(EventStoreContextTests.EventStoreHttpDockerPort)
                                                                       .ExposePort(EventStoreContextTests.EventStoreTcpDockerPort).WithName(containerName)
                                                                       .UseNetwork(networkService);

            if (isSecureContainer == false) {
                environmentVariables.Add("EVENTSTORE_INSECURE=true");
            }
            else {
                // Copy these to the container
                String path = Path.Combine(Directory.GetCurrentDirectory(), "certs");

                eventStoreContainerBuilder = eventStoreContainerBuilder.Mount(path, "/etc/eventstore/certs", MountType.ReadWrite);

                // Certificates configuration
                environmentVariables.Add("EVENTSTORE_CertificateFile=/etc/eventstore/certs/node1/node.crt");
                environmentVariables.Add("EVENTSTORE_CertificatePrivateKeyFile=/etc/eventstore/certs/node1/node.key");
                environmentVariables.Add("EVENTSTORE_TrustedRootCertificatesPath=/etc/eventstore/certs/ca");
            }

            IContainerService eventStoreContainer = eventStoreContainerBuilder
                                                    .WithEnvironment(environmentVariables.ToArray()).Build().Start().WaitForPort("2113/tcp", 30000);

            return eventStoreContainer;
        }

        private void StopContainers() {
            if (this.Containers.Any()) {
                foreach (IContainerService containerService in this.Containers) {
                    containerService.StopOnDispose = true;
                    containerService.RemoveOnDispose = true;
                    containerService.Dispose();
                }
            }

            if (this.TestNetworks.Any()) {
                foreach (INetworkService networkService in this.TestNetworks) {
                    networkService.Stop();
                    networkService.Remove(true);
                }
            }
        }

        #endregion

        #region Others

        private const Int32 EventStoreHttpDockerPort = 2113;

        private const Int32 EventStoreTcpDockerPort = 1113;

        #endregion
    }
}