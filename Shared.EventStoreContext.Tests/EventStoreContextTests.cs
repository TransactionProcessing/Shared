namespace Shared.EventStore.Tests{
    using Aggregate;
    using DomainDrivenDesign.EventSourcing;
    using EventStore;
    using EventStoreContext.Tests;
    using global::EventStore.Client;
    using IntegrationTesting;
    using Logger;
    using Newtonsoft.Json;
    using NLog;
    using Shouldly;

    public class EventStoreContextTests : IDisposable{
        #region Fields

        private readonly EventStoreDockerHelper EventStoreDockerHelper;

        #endregion

        #region Constructors

        public EventStoreContextTests(){
            NlogLogger logger = new NlogLogger();
            logger.Initialise(LogManager.GetLogger("Specflow"), "Specflow");
            LogManager.AddHiddenAssembly(typeof(NlogLogger).Assembly);

            this.EventStoreDockerHelper = new EventStoreDockerHelper();
            this.EventStoreDockerHelper.Logger = logger;
        }

        #endregion

        #region Methods

        //TimeSpan? deadline = null;
        public void Dispose(){
            this.EventStoreDockerHelper.StopContainersForScenarioRun().Wait();
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

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task EventStoreContext_InsertEvents_EventsAreWritten(Boolean secureEventStore){
            await this.EventStoreDockerHelper.StartContainers(secureEventStore, $"EventStoreContext_InsertEvents_EventsAreWritten_{secureEventStore}");

            TimeSpan deadline = TimeSpan.FromMinutes(2);
            TimeSpan retryTimeout = TimeSpan.FromMinutes(6);

            IEventStoreContext context = this.CreateContext(secureEventStore, deadline);

            Guid aggreggateId = Guid.NewGuid();
            String streamName = $"TestStream-{aggreggateId:N}";

            EstateCreatedEvent event1 = new(aggreggateId, "Test Estate 1");
            List<IDomainEvent> domainEvents = new();
            domainEvents.Add(event1);

            IEventDataFactory factory = new EventDataFactory();
            EventData[] events = factory.CreateEventDataList(domainEvents);

            await Retry.For(async () => { await context.InsertEvents(streamName, -1, events.ToList(), CancellationToken.None); },
                            retryTimeout,
                            deadline);
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task EventStoreContext_ReadEvents_EventsAreRead(Boolean secureEventStore){
            TimeSpan deadline = TimeSpan.FromMinutes(2);
            TimeSpan retryTimeout = TimeSpan.FromMinutes(6);

            await this.EventStoreDockerHelper.StartContainers(secureEventStore, $"EventStoreContext_ReadEvents_EventsAreRead{secureEventStore}");

            await Task.Delay(TimeSpan.FromSeconds(30));

            IEventStoreContext context = this.CreateContext(secureEventStore, deadline);

            Guid aggreggateId = Guid.NewGuid();
            String streamName = $"TestStream-{aggreggateId:N}";

            EstateCreatedEvent event1 = new(aggreggateId, "Test Estate 1");
            EstateCreatedEvent event2 = new(aggreggateId, "Test Estate 2");
            List<IDomainEvent> domainEvents = new();
            domainEvents.Add(event1);
            domainEvents.Add(event2);

            IEventDataFactory factory = new EventDataFactory();
            EventData[] events = factory.CreateEventDataList(domainEvents);

            await Retry.For(async () => { await context.InsertEvents(streamName, -1, events.ToList(), CancellationToken.None); },
                            retryTimeout,
                            deadline);

            await Retry.For(async () => {
                                List<ResolvedEvent> resolvedEvents = null;
                                resolvedEvents = await context.ReadEvents(streamName, 0, CancellationToken.None);

                                resolvedEvents.Count.ShouldBe(events.Length);
                            },
                            retryTimeout,
                            deadline);
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task EventStoreContext_ReadEventsBackwards_EventsAreRead(Boolean secureEventStore){
            TimeSpan deadline = TimeSpan.FromMinutes(2);
            TimeSpan retryTimeout = TimeSpan.FromMinutes(6);

            await this.EventStoreDockerHelper.StartContainers(secureEventStore, $"EventStoreContext_ReadEventsBackwards_EventsAreRead{secureEventStore}");

            IEventStoreContext context = this.CreateContext(secureEventStore, deadline);

            Guid aggreggateId = Guid.NewGuid();
            String streamName = $"TestStream-{aggreggateId:N}";

            EstateCreatedEvent event1 = new(aggreggateId, "Test Estate 1");
            EstateCreatedEvent event2 = new(aggreggateId, "Test Estate 2");
            List<IDomainEvent> domainEvents = new();
            domainEvents.Add(event1);
            domainEvents.Add(event2);

            IEventDataFactory factory = new EventDataFactory();
            EventData[] events = factory.CreateEventDataList(domainEvents);
            await Retry.For(async () => { await context.InsertEvents(streamName, -1, events.ToList(), CancellationToken.None); },
                            retryTimeout,
                            deadline);

            await Retry.For(async () => {
                                IList<ResolvedEvent> resolvedEvents = await context.GetEventsBackward(streamName, events.Length, CancellationToken.None);

                                resolvedEvents.Count.ShouldBe(events.Length);
                            },
                            retryTimeout,
                            deadline);
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task EventStoreContext_ReadEventsBackwards_StreamNotFound_EventsAreRead(Boolean secureEventStore){
            TimeSpan deadline = TimeSpan.FromMinutes(2);
            TimeSpan retryTimeout = TimeSpan.FromMinutes(6);

            await this.EventStoreDockerHelper.StartContainers(secureEventStore, $"EventStoreContext_ReadEventsBackwards_StreamNotFound_EventsAreRead{secureEventStore}");

            IEventStoreContext context = this.CreateContext(secureEventStore, deadline);

            Guid aggreggateId = Guid.NewGuid();
            String streamName = $"TestStream1-{aggreggateId:N}";

            await Retry.For(async () => {
                                IList<ResolvedEvent> resolvedEvents = await context.GetEventsBackward(streamName, 1, CancellationToken.None);

                                resolvedEvents.ShouldBeEmpty();
                            },
                            retryTimeout,
                            deadline);
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task EventStoreContext_RunTransientQuery_Faulted_ErrorThrown(Boolean secureEventStore){
            TimeSpan deadline = TimeSpan.FromMinutes(2);
            TimeSpan retryTimeout = TimeSpan.FromMinutes(6);

            await this.EventStoreDockerHelper.StartContainers(secureEventStore, $"EventStoreContext_RunTransientQuery_Faulted_ErrorThrown{secureEventStore}");

            await Task.Delay(TimeSpan.FromSeconds(30));

            IEventStoreContext context = this.CreateContext(secureEventStore, deadline);

            Guid aggreggateId = Guid.NewGuid();
            String streamName = $"TestStream-{aggreggateId:N}";

            EstateCreatedEvent event1 = new(aggreggateId, "Test Estate 1");
            EstateCreatedEvent event2 = new(aggreggateId, "Test Estate 2");
            List<IDomainEvent> domainEvents = new();
            domainEvents.Add(event1);
            domainEvents.Add(event2);

            IEventDataFactory factory = new EventDataFactory();
            EventData[] events = factory.CreateEventDataList(domainEvents);

            await Retry.For(async () => { await context.InsertEvents(streamName, -1, events.ToList(), CancellationToken.None); },
                            retryTimeout,
                            deadline);

            await Retry.For(async () => {
                                List<ResolvedEvent> resolvedEvents = null;
                                resolvedEvents = await context.ReadEvents(streamName, 0, CancellationToken.None);

                                resolvedEvents.Count.ShouldBe(events.Length);
                            },
                            retryTimeout,
                            deadline);

            await Task.Delay(TimeSpan.FromSeconds(15));

            String query = "fromStream('$et-EstateCreatedEvent')\r\n  .when({\r\n      $init: function (s, e)\r\n        {\r\n            return {\r\n                estates:[]\r\n            };\r\n        },\r\n        \"EstateCreatedEvent\": function(s e){\r\n          s.estates.push(e.data.estateName);\r\n        }\r\n  });";

            Exception ex = Should.Throw<Exception>(async () => { await context.RunTransientQuery(query, CancellationToken.None); });
            ex.Message.ShouldBe("Faulted");
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task EventStoreContext_RunTransientQuery_QueryIsRun(Boolean secureEventStore){
            TimeSpan deadline = TimeSpan.FromMinutes(2);
            TimeSpan retryTimeout = TimeSpan.FromMinutes(6);

            await this.EventStoreDockerHelper.StartContainers(secureEventStore, $"EventStoreContext_RunTransientQuery_QueryIsRun{secureEventStore}");

            await Task.Delay(TimeSpan.FromSeconds(30));

            IEventStoreContext context = this.CreateContext(secureEventStore, deadline);

            Guid aggreggateId = Guid.NewGuid();
            String streamName = $"TestStream-{aggreggateId:N}";

            EstateCreatedEvent event1 = new(aggreggateId, "Test Estate 1");
            EstateCreatedEvent event2 = new(aggreggateId, "Test Estate 2");
            List<IDomainEvent> domainEvents = new();
            domainEvents.Add(event1);
            domainEvents.Add(event2);

            IEventDataFactory factory = new EventDataFactory();
            EventData[] events = factory.CreateEventDataList(domainEvents);

            await Retry.For(async () => { await context.InsertEvents(streamName, -1, events.ToList(), CancellationToken.None); },
                            retryTimeout,
                            deadline);

            await Retry.For(async () => {
                                List<ResolvedEvent> resolvedEvents = null;
                                resolvedEvents = await context.ReadEvents(streamName, 0, CancellationToken.None);

                                resolvedEvents.Count.ShouldBe(events.Length);
                            },
                            retryTimeout,
                            deadline);

            await Task.Delay(TimeSpan.FromSeconds(15));

            String query = "fromStream('$et-EstateCreatedEvent')\r\n  .when({\r\n      $init: function (s, e)\r\n        {\r\n            return {\r\n                estates:[]\r\n            };\r\n        },\r\n        \"EstateCreatedEvent\": function(s,e){\r\n          s.estates.push(e.data.estateName);\r\n        }\r\n  });";

            String queryResult = null;
            await Retry.For(async () => {
                                queryResult = await context.RunTransientQuery(query, CancellationToken.None);
                                queryResult.ShouldNotBeNullOrEmpty();
                            },
                            retryTimeout,
                            deadline);

            var definition = new{
                                    estates = new List<String>()
                                };
            await Retry.For(async () => {
                                var result = JsonConvert.DeserializeAnonymousType(queryResult, definition);

                                result.estates.Contains(event1.EstateName).ShouldBeTrue();
                                result.estates.Contains(event2.EstateName).ShouldBeTrue();
                            },
                            retryTimeout,
                            deadline);
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task EventStoreContext_RunTransientQuery_ResultIsEmpty_ErrorThrown(Boolean secureEventStore){
            TimeSpan deadline = TimeSpan.FromMinutes(2);
            TimeSpan retryTimeout = TimeSpan.FromMinutes(6);

            await this.EventStoreDockerHelper.StartContainers(secureEventStore, $"EventStoreContext_RunTransientQuery_ResultIsEmpty_ErrorThrown{secureEventStore}");

            await Task.Delay(TimeSpan.FromSeconds(30));

            IEventStoreContext context = this.CreateContext(secureEventStore, deadline);

            Guid aggreggateId = Guid.NewGuid();
            String streamName = $"TestStream-{aggreggateId:N}";

            EstateCreatedEvent event1 = new(aggreggateId, "Test Estate 1");
            EstateCreatedEvent event2 = new(aggreggateId, "Test Estate 2");
            List<IDomainEvent> domainEvents = new();
            domainEvents.Add(event1);
            domainEvents.Add(event2);

            IEventDataFactory factory = new EventDataFactory();
            EventData[] events = factory.CreateEventDataList(domainEvents);
            await Retry.For(async () => { await context.InsertEvents(streamName, -1, events.ToList(), CancellationToken.None); },
                            retryTimeout,
                            deadline);

            await Retry.For(async () => {
                                List<ResolvedEvent> resolvedEvents = null;
                                resolvedEvents = await context.ReadEvents(streamName, 0, CancellationToken.None);

                                resolvedEvents.Count.ShouldBe(events.Length);
                            },
                            retryTimeout,
                            deadline);

            await Task.Delay(TimeSpan.FromSeconds(15));

            String query = "fromStream('$et-EstateCreatedEvent')\r\n  .when({\r\n      $init: function (s, e)\r\n        {\r\n            return {\r\n                \r\n            };\r\n        },\r\n        \"EstateCreatedEvent\": function(s, e){\r\n          }\r\n  });";

            await Retry.For(async () => {
                                String queryResult = await context.RunTransientQuery(query, CancellationToken.None);
                                queryResult.ShouldBeEmpty();
                            },
                            retryTimeout,
                            deadline);
        }

        private IEventStoreContext CreateContext(Boolean secureEventStore, TimeSpan? deadline = null){
            EventStoreClientSettings settings = this.EventStoreDockerHelper.CreateEventStoreClientSettings(secureEventStore, deadline);

            EventStoreClient client = new(settings);
            EventStoreProjectionManagementClient projectionManagementClient = new(settings);
            IEventStoreContext context = new EventStoreContext(client, projectionManagementClient, deadline);
            return context;
        }

        #endregion
    }
}