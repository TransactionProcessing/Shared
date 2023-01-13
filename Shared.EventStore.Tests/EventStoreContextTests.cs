namespace Shared.EventStore.Tests
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
    using Ductus.FluentDocker.Commands;
    using Ductus.FluentDocker.Model.Builders;
    using Ductus.FluentDocker.Model.Containers;
    using Ductus.FluentDocker.Services;
    using Ductus.FluentDocker.Services.Extensions;
    using EventStore;
    using global::EventStore.Client;
    using IntegrationTesting;
    using NLog;
    using Shared.Logger;
    using Shouldly;
    using Xunit;
    using NullLogger = Logger.NullLogger;

    public class EventStoreContextTests : IDisposable
    {
        private readonly EventStoreDockerHelper EventStoreDockerHelper;

        #region Methods

        public void Dispose() {
            this.EventStoreDockerHelper.StopContainersForScenarioRun().Wait();
        }
        
        public EventStoreContextTests() {
            this.EventStoreDockerHelper = new EventStoreDockerHelper();
            this.EventStoreDockerHelper.Logger = NullLogger.Instance;
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task EventStoreContext_InsertEvents_EventsAreWritten(Boolean secureEventStore) {
            await this.EventStoreDockerHelper.StartContainers(secureEventStore);

            await Task.Delay(TimeSpan.FromSeconds(30));

            EventStoreClientSettings settings = this.EventStoreDockerHelper.CreateEventStoreClientSettings(secureEventStore);

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
            await this.EventStoreDockerHelper.StartContainers(secureEventStore);

            await Task.Delay(TimeSpan.FromSeconds(30));

            EventStoreClientSettings settings = this.EventStoreDockerHelper.CreateEventStoreClientSettings(secureEventStore);

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

            await Retry.For(async () => {
                                List<ResolvedEvent> resolvedEvents = null;
                                resolvedEvents = await context.ReadEvents(streamName, 0, CancellationToken.None));

                                resolvedEvents.Count.ShouldBe(events.Length);
                            });
        }

        
        
        #endregion
    }
    
    public class EventStoreDockerHelper : DockerHelper
    {
        public async Task StartContainers(Boolean isSecureEventStore) {
            this.IsSecureEventStore = isSecureEventStore;
            await this.StartContainersForScenarioRun("");
        }

        public override async Task StartContainersForScenarioRun(String scenarioName) {
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
}