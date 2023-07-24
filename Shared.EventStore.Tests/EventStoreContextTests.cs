namespace Shared.EventStore.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Aggregate;
    using DomainDrivenDesign.EventSourcing;
    using Ductus.FluentDocker.Builders;
    using Ductus.FluentDocker.Commands;
    using Ductus.FluentDocker.Model.Builders;
    using Ductus.FluentDocker.Model.Containers;
    using Ductus.FluentDocker.Services.Extensions;
    using EventStore;
    using global::EventStore.Client;
    using IntegrationTesting;
    using Newtonsoft.Json;
    using NLog;
    using Shared.Logger;
    using Shouldly;
    using Xunit;
    using Logger = Logger.Logger;
    using NullLogger = Logger.NullLogger;

    public class EventStoreContextTests : IDisposable{
        private readonly EventStoreDockerHelper EventStoreDockerHelper;

        #region Methods

        public void Dispose(){
            this.EventStoreDockerHelper.StopContainersForScenarioRun().Wait();
        }

        public EventStoreContextTests(){

            NlogLogger logger = new NlogLogger();
            logger.Initialise(LogManager.GetLogger("Specflow"), "Specflow");
            LogManager.AddHiddenAssembly(typeof(NlogLogger).Assembly);
            
            this.EventStoreDockerHelper = new EventStoreDockerHelper();
            this.EventStoreDockerHelper.Logger = logger;
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task EventStoreContext_InsertEvents_EventsAreWritten(Boolean secureEventStore){
            
            await this.EventStoreDockerHelper.StartContainers(secureEventStore, $"EventStoreContext_InsertEvents_EventsAreWritten_{secureEventStore}");

            EventStoreClientSettings settings = this.EventStoreDockerHelper.CreateEventStoreClientSettings(secureEventStore);
            settings.DefaultDeadline = TimeSpan.FromSeconds(60);
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
            await Retry.For(async () => { await context.InsertEvents(streamName, -1, events.ToList(), CancellationToken.None); });
        }

        
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task EventStoreContext_ReadEvents_EventsAreRead(Boolean secureEventStore){
            await this.EventStoreDockerHelper.StartContainers(secureEventStore, $"EventStoreContext_ReadEvents_EventsAreRead{secureEventStore}");

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
                                resolvedEvents = await context.ReadEvents(streamName, 0, CancellationToken.None);

                                resolvedEvents.Count.ShouldBe(events.Length);
                            });
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task EventStoreContext_ReadEventsBackwards_EventsAreRead(Boolean secureEventStore){
            await this.EventStoreDockerHelper.StartContainers(secureEventStore,$"EventStoreContext_ReadEventsBackwards_EventsAreRead{secureEventStore}");

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
                                IList<ResolvedEvent> resolvedEvents = await context.GetEventsBackward(streamName, events.Length, CancellationToken.None);

                                resolvedEvents.Count.ShouldBe(events.Length);
                            });
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task EventStoreContext_ReadEventsBackwards_StreamNotFound_EventsAreRead(Boolean secureEventStore)
        {
            await this.EventStoreDockerHelper.StartContainers(secureEventStore, $"EventStoreContext_ReadEventsBackwards_StreamNotFound_EventsAreRead{secureEventStore}");

            await Task.Delay(TimeSpan.FromSeconds(30));

            EventStoreClientSettings settings = this.EventStoreDockerHelper.CreateEventStoreClientSettings(secureEventStore);

            EventStoreClient client = new(settings);
            EventStoreProjectionManagementClient projectionManagementClient = new(settings);
            IEventStoreContext context = new EventStoreContext(client, projectionManagementClient);

            Guid aggreggateId = Guid.NewGuid();
            String streamName = $"TestStream1-{aggreggateId:N}";
            
            await Retry.For(async () => {
                                IList<ResolvedEvent> resolvedEvents = await context.GetEventsBackward(streamName, 1, CancellationToken.None);

                                resolvedEvents.ShouldBeEmpty();
                            });
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task EventStoreContext_RunTransientQuery_QueryIsRun(Boolean secureEventStore){
            await this.EventStoreDockerHelper.StartContainers(secureEventStore, $"EventStoreContext_RunTransientQuery_QueryIsRun{secureEventStore}");

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
                                resolvedEvents = await context.ReadEvents(streamName, 0, CancellationToken.None);

                                resolvedEvents.Count.ShouldBe(events.Length);
                            });

            await Task.Delay(TimeSpan.FromSeconds(15));

            String query = "fromStream('$et-EstateCreatedEvent')\r\n  .when({\r\n      $init: function (s, e)\r\n        {\r\n            return {\r\n                estates:[]\r\n            };\r\n        },\r\n        \"EstateCreatedEvent\": function(s,e){\r\n          s.estates.push(e.data.estateName);\r\n        }\r\n  });";

            String queryResult = await context.RunTransientQuery(query, CancellationToken.None);
            queryResult.ShouldNotBeNullOrEmpty();
            
            var definition = new{
                                    estates = new List<String>()
                                };
            var result = JsonConvert.DeserializeAnonymousType(queryResult, definition);
            
            result.estates.Contains(event1.EstateName).ShouldBeTrue();
            result.estates.Contains(event2.EstateName).ShouldBeTrue();
        }


        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task EventStoreContext_RunTransientQuery_Faulted_ErrorThrown(Boolean secureEventStore)
        {
            await this.EventStoreDockerHelper.StartContainers(secureEventStore, $"EventStoreContext_RunTransientQuery_Faulted_ErrorThrown{secureEventStore}");

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
                resolvedEvents = await context.ReadEvents(streamName, 0, CancellationToken.None);

                resolvedEvents.Count.ShouldBe(events.Length);
            });

            await Task.Delay(TimeSpan.FromSeconds(15));

            String query = "fromStream('$et-EstateCreatedEvent')\r\n  .when({\r\n      $init: function (s, e)\r\n        {\r\n            return {\r\n                estates:[]\r\n            };\r\n        },\r\n        \"EstateCreatedEvent\": function(s e){\r\n          s.estates.push(e.data.estateName);\r\n        }\r\n  });";

            Exception ex = Should.Throw<Exception>(async () => {
                                                       String queryResult = await context.RunTransientQuery(query, CancellationToken.None);
                                                   });
            ex.Message.ShouldBe("Faulted");
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task EventStoreContext_RunTransientQuery_ResultIsEmpty_ErrorThrown(Boolean secureEventStore)
        {
            await this.EventStoreDockerHelper.StartContainers(secureEventStore, $"EventStoreContext_RunTransientQuery_ResultIsEmpty_ErrorThrown{secureEventStore}");

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
                resolvedEvents = await context.ReadEvents(streamName, 0, CancellationToken.None);

                resolvedEvents.Count.ShouldBe(events.Length);
            });

            await Task.Delay(TimeSpan.FromSeconds(15));

            String query = "fromStream('$et-EstateCreatedEvent')\r\n  .when({\r\n      $init: function (s, e)\r\n        {\r\n            return {\r\n                \r\n            };\r\n        },\r\n        \"EstateCreatedEvent\": function(s, e){\r\n          }\r\n  });";

            String queryResult = await context.RunTransientQuery(query, CancellationToken.None);
            queryResult.ShouldBeEmpty();
        }

        [Theory]
        [InlineData("Running", ProjectionRunningStatus.Running)]
        [InlineData("running", ProjectionRunningStatus.Running)]
        [InlineData("RUNNING", ProjectionRunningStatus.Running)]
        [InlineData("Stopped", ProjectionRunningStatus.Stopped)]
        [InlineData("stopped", ProjectionRunningStatus.Stopped)]
        [InlineData("STOPPED", ProjectionRunningStatus.Stopped)]
        [InlineData("Faulted", ProjectionRunningStatus.Faulted)]
        [InlineData("faulted", ProjectionRunningStatus.Faulted)]
        [InlineData("FAULTED", ProjectionRunningStatus.Faulted)]
        [InlineData("Completed/Stopped/Writing results", ProjectionRunningStatus.Completed)]
        [InlineData("completed/stopped/writing results", ProjectionRunningStatus.Completed)]
        [InlineData("COMPLETED/STOPPED/WRITING RESULTS", ProjectionRunningStatus.Completed)]
        [InlineData("Unknown", ProjectionRunningStatus.Unknown)]

        public void EventStoreContext_GetStatusFrom_CorrectValueReturned(String status, ProjectionRunningStatus expected){
            ProjectionDetails projectionDetails = new ProjectionDetails(0,
                                                                        0,
                                                                        0,
                                                                        String.Empty,
                                                                        0,
                                                                        0,
                                                                        0,
                                                                        status,
                                                                        String.Empty,
                                                                        "TestProjection",
                                                                        String.Empty,
                                                                        "0",
                                                                        0,
                                                                        "",
                                                                        0,
                                                                        "",
                                                                        0,
                                                                        0,
                                                                        0);
            ProjectionRunningStatus result = EventStoreContext.GetStatusFrom(projectionDetails);
            result.ShouldBe(expected);
        }

        [Fact]
        public void EventStoreContext_GetStatusFrom_ProjectionDetailsIsNull_CorrectValueReturned(){
            ProjectionDetails projectionDetails = null;
            ProjectionRunningStatus result = EventStoreContext.GetStatusFrom(projectionDetails);
            result.ShouldBe(ProjectionRunningStatus.StatisticsNotFound);
        }
        #endregion
    }
}