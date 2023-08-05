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
    using Ductus.FluentDocker.Common;
    using Ductus.FluentDocker.Model.Builders;
    using Ductus.FluentDocker.Model.Containers;
    using Ductus.FluentDocker.Services.Extensions;
    using EventStore;
    using EventStoreContext.Tests;
    using global::EventStore.Client;
    using IntegrationTesting;
    using Newtonsoft.Json;
    using NLog;
    using Shared.Logger;
    using Shouldly;
    using Logger = Logger.Logger;
    using NullLogger = Logger.NullLogger;

    public class EventStoreContextTests : IDisposable{
        private readonly EventStoreDockerHelper EventStoreDockerHelper;

        #region Methods
        TimeSpan? deadline = null;
        public void Dispose(){
            this.EventStoreDockerHelper.StopContainersForScenarioRun().Wait();
        }

        public EventStoreContextTests(){

            NlogLogger logger = new NlogLogger();
            logger.Initialise(LogManager.GetLogger("Specflow"), "Specflow");
            LogManager.AddHiddenAssembly(typeof(NlogLogger).Assembly);
            
            this.EventStoreDockerHelper = new EventStoreDockerHelper();
            this.EventStoreDockerHelper.Logger = logger;
            
            if (FdOs.IsOsx())
            {
                deadline = new TimeSpan(0, 0, 2, 0, 0);
            }
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task EventStoreContext_InsertEvents_EventsAreWritten(Boolean secureEventStore){
            
            await this.EventStoreDockerHelper.StartContainers(secureEventStore, $"EventStoreContext_InsertEvents_EventsAreWritten_{secureEventStore}");

            IEventStoreContext context = this.CreateContext(secureEventStore);

            Guid aggreggateId = Guid.NewGuid();
            String streamName = $"TestStream-{aggreggateId:N}";

            EstateCreatedEvent event1 = new(aggreggateId, "Test Estate 1");
            List<IDomainEvent> domainEvents = new();
            domainEvents.Add(event1);

            IEventDataFactory factory = new EventDataFactory();
            EventData[] events = factory.CreateEventDataList(domainEvents);
            await Retry.For(async () => { await context.InsertEvents(streamName, -1, events.ToList(), CancellationToken.None); });
        }

        
        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task EventStoreContext_ReadEvents_EventsAreRead(Boolean secureEventStore){
            await this.EventStoreDockerHelper.StartContainers(secureEventStore, $"EventStoreContext_ReadEvents_EventsAreRead{secureEventStore}");

            await Task.Delay(TimeSpan.FromSeconds(30));

            IEventStoreContext context = this.CreateContext(secureEventStore);

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

        private IEventStoreContext CreateContext(Boolean secureEventStore){
            EventStoreClientSettings settings = this.EventStoreDockerHelper.CreateEventStoreClientSettings(secureEventStore, this.deadline);

            EventStoreClient client = new(settings);
            EventStoreProjectionManagementClient projectionManagementClient = new(settings);
            IEventStoreContext context = new EventStoreContext(client, projectionManagementClient);
            return context;
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task EventStoreContext_ReadEventsBackwards_EventsAreRead(Boolean secureEventStore){
            await this.EventStoreDockerHelper.StartContainers(secureEventStore,$"EventStoreContext_ReadEventsBackwards_EventsAreRead{secureEventStore}");

            await Task.Delay(TimeSpan.FromSeconds(30));

            IEventStoreContext context = this.CreateContext(secureEventStore);

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

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task EventStoreContext_ReadEventsBackwards_StreamNotFound_EventsAreRead(Boolean secureEventStore)
        {
            await this.EventStoreDockerHelper.StartContainers(secureEventStore, $"EventStoreContext_ReadEventsBackwards_StreamNotFound_EventsAreRead{secureEventStore}");

            await Task.Delay(TimeSpan.FromSeconds(30));

            IEventStoreContext context = this.CreateContext(secureEventStore);

            Guid aggreggateId = Guid.NewGuid();
            String streamName = $"TestStream1-{aggreggateId:N}";
            
            await Retry.For(async () => {
                                IList<ResolvedEvent> resolvedEvents = await context.GetEventsBackward(streamName, 1, CancellationToken.None);

                                resolvedEvents.ShouldBeEmpty();
                            });
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task EventStoreContext_RunTransientQuery_QueryIsRun(Boolean secureEventStore){
            await this.EventStoreDockerHelper.StartContainers(secureEventStore, $"EventStoreContext_RunTransientQuery_QueryIsRun{secureEventStore}");

            await Task.Delay(TimeSpan.FromSeconds(30));

            IEventStoreContext context = this.CreateContext(secureEventStore);

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


        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task EventStoreContext_RunTransientQuery_Faulted_ErrorThrown(Boolean secureEventStore)
        {
            await this.EventStoreDockerHelper.StartContainers(secureEventStore, $"EventStoreContext_RunTransientQuery_Faulted_ErrorThrown{secureEventStore}");

            await Task.Delay(TimeSpan.FromSeconds(30));

            IEventStoreContext context = this.CreateContext(secureEventStore);

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

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task EventStoreContext_RunTransientQuery_ResultIsEmpty_ErrorThrown(Boolean secureEventStore)
        {
            await this.EventStoreDockerHelper.StartContainers(secureEventStore, $"EventStoreContext_RunTransientQuery_ResultIsEmpty_ErrorThrown{secureEventStore}");

            await Task.Delay(TimeSpan.FromSeconds(30));

            IEventStoreContext context = this.CreateContext(secureEventStore);

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

        [Test]
        [TestCase("Running", ProjectionRunningStatus.Running)]
        [TestCase("running", ProjectionRunningStatus.Running)]
        [TestCase("RUNNING", ProjectionRunningStatus.Running)]
        [TestCase("Stopped", ProjectionRunningStatus.Stopped)]
        [TestCase("stopped", ProjectionRunningStatus.Stopped)]
        [TestCase("STOPPED", ProjectionRunningStatus.Stopped)]
        [TestCase("Faulted", ProjectionRunningStatus.Faulted)]
        [TestCase("faulted", ProjectionRunningStatus.Faulted)]
        [TestCase("FAULTED", ProjectionRunningStatus.Faulted)]
        [TestCase("Completed/Stopped/Writing results", ProjectionRunningStatus.Completed)]
        [TestCase("completed/stopped/writing results", ProjectionRunningStatus.Completed)]
        [TestCase("COMPLETED/STOPPED/WRITING RESULTS", ProjectionRunningStatus.Completed)]
        [TestCase("Unknown", ProjectionRunningStatus.Unknown)]

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

        [Test]
        public void EventStoreContext_GetStatusFrom_ProjectionDetailsIsNull_CorrectValueReturned(){
            ProjectionDetails projectionDetails = null;
            ProjectionRunningStatus result = EventStoreContext.GetStatusFrom(projectionDetails);
            result.ShouldBe(ProjectionRunningStatus.StatisticsNotFound);
        }
        #endregion
    }
}