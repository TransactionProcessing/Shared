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
    using static IdentityModel.OidcConstants;

    public class EventStoreContextTests : IDisposable{
        #region Fields

        private readonly EventStoreDockerHelper EventStoreDockerHelper;

        #endregion

        #region Constructors

        public EventStoreContextTests(){
            NlogLogger logger = new NlogLogger();
            LogManager.AddHiddenAssembly(typeof(NlogLogger).Assembly);
            LogManager.LoadConfiguration("nlog.config");

            logger.Initialise(LogManager.GetLogger("Reqnroll"), "Reqnroll");
           
            this.EventStoreDockerHelper = new EventStoreDockerHelper();
            this.EventStoreDockerHelper.Logger = logger;
        }

        #endregion

        #region Methods

        //TimeSpan? deadline = null;
        public void Dispose(){
            //this.EventStoreDockerHelper.StopContainersForScenarioRun().Wait();
        }

        [TearDown]
        public async Task TearDown(){
            DockerServices sharedDockerServices = DockerServices.SqlServer;

            await this.EventStoreDockerHelper.StopContainersForScenarioRun(sharedDockerServices);
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
        [TestCase("Faulted (Enabled)", ProjectionRunningStatus.Faulted)]
        [TestCase("faulted (Enabled)", ProjectionRunningStatus.Faulted)]
        [TestCase("FAULTED (Enabled)", ProjectionRunningStatus.Faulted)]
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
            List<IDomainEvent> domainEvents = new(){
                                                       event1
                                                   };

            IEventDataFactory factory = new EventDataFactory();
            EventData[] events = factory.CreateEventDataList(domainEvents);

            await this.InsertEvents(context, streamName, events, deadline, retryTimeout);
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task EventStoreContext_ReadEvents_EventsAreRead(Boolean secureEventStore){
            TimeSpan deadline = TimeSpan.FromMinutes(2);
            TimeSpan retryTimeout = TimeSpan.FromMinutes(6);

            await this.EventStoreDockerHelper.StartContainers(secureEventStore, $"EventStoreContext_ReadEvents_EventsAreRead{secureEventStore}");
            
            IEventStoreContext context = this.CreateContext(secureEventStore, deadline);

            Guid aggreggateId = Guid.NewGuid();
            String streamName = $"TestStream-{aggreggateId:N}";

            EstateCreatedEvent event1 = new(aggreggateId, "Test Estate 1");
            EstateCreatedEvent event2 = new(aggreggateId, "Test Estate 2");
            List<IDomainEvent> domainEvents = new(){
                                                       event1,
                                                       event2
                                                   };

            IEventDataFactory factory = new EventDataFactory();
            EventData[] events = factory.CreateEventDataList(domainEvents);

            await this.InsertEvents(context, streamName, events, deadline, retryTimeout);

            await this.ReadEvents(context, streamName, events.Length, deadline, retryTimeout);
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

            await this.InsertEvents(context, streamName, events, deadline, retryTimeout);

            await this.ReadEventsBackwards(context, streamName, events.Length, deadline, retryTimeout);
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

            await this.ReadEventsBackwards(context, streamName, null, deadline, retryTimeout);
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task EventStoreContext_RunTransientQuery_Faulted_ErrorThrown(Boolean secureEventStore){
            TimeSpan deadline = TimeSpan.FromMinutes(2);
            TimeSpan retryTimeout = TimeSpan.FromMinutes(6);

            await this.EventStoreDockerHelper.StartContainers(secureEventStore, $"EventStoreContext_RunTransientQuery_Faulted_ErrorThrown{secureEventStore}");
            
            IEventStoreContext context = this.CreateContext(secureEventStore, deadline);

            Guid aggreggateId = Guid.NewGuid();
            String streamName = $"TestStream-{aggreggateId:N}";

            EstateCreatedEvent event1 = new(aggreggateId, "Test Estate 1");
            EstateCreatedEvent event2 = new(aggreggateId, "Test Estate 2");
            List<IDomainEvent> domainEvents = new(){
                                                       event1,
                                                       event2
                                                   };

            IEventDataFactory factory = new EventDataFactory();
            EventData[] events = factory.CreateEventDataList(domainEvents);

            await this.InsertEvents(context, streamName, events, deadline, retryTimeout);
            await this.ReadEvents(context, streamName, events.Length, deadline, retryTimeout);

            //String query = "fromStream('$et-EstateCreatedEvent')\r\n  .when({\r\n      $init: function (s, e)\r\n        {\r\n            return {\r\n                estates:[]\r\n            };\r\n        },\r\n        \"EstateCreatedEvent\": function(s e){\r\n          s.estates.push(e.data.estateName);\r\n        }\r\n  });";
            String query = "fromStream('$et-EstateCreatedEvent')\r\n  .when({\r\n      $init: function (s, e)\r\n        {\r\n            return {\r\n                estates:[]\r\n            };\r\n        },\r\n        \"EstateCreatedEvent\": function(s,e){\r\n          s.estate.push(e.data.estateName);\r\n        }\r\n  });";

            //Exception ex = Should.Throw<Exception>(async () => { await this.RunTransientQuery(context, query, 1);});
            //ex.Message.ShouldBe("Faulted");
            await this.RunTransientQuery(context, query, 1);
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task EventStoreContext_RunTransientQuery_QueryIsRun(Boolean secureEventStore){
            TimeSpan deadline = TimeSpan.FromMinutes(2);
            TimeSpan retryTimeout = TimeSpan.FromMinutes(6);

            await this.EventStoreDockerHelper.StartContainers(secureEventStore, $"EventStoreContext_RunTransientQuery_QueryIsRun{secureEventStore}");

            IEventStoreContext context = this.CreateContext(secureEventStore, deadline);

            Guid aggreggateId = Guid.NewGuid();
            String streamName = $"TestStream-{aggreggateId:N}";

            EstateCreatedEvent event1 = new(aggreggateId, "Test Estate 1");
            EstateCreatedEvent event2 = new(aggreggateId, "Test Estate 2");
            List<IDomainEvent> domainEvents = new(){
                                                       event1,
                                                       event2,
                                                   };
            
            IEventDataFactory factory = new EventDataFactory();
            EventData[] events = factory.CreateEventDataList(domainEvents);

            await this.InsertEvents(context, streamName, events, deadline, retryTimeout);
            await this.ReadEvents(context, streamName, events.Length, deadline, retryTimeout);

            String query = "fromStream('$et-EstateCreatedEvent')\r\n  .when({\r\n      $init: function (s, e)\r\n        {\r\n            return {\r\n                estates:[]\r\n            };\r\n        },\r\n        \"EstateCreatedEvent\": function(s,e){\r\n          s.estates.push(e.data.estateName);\r\n        }\r\n  });";

            String queryResult = await this.RunTransientQuery(context, query, 2, deadline, retryTimeout);

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
        public async Task EventStoreContext_RunTransientQuery_ResultIsEmpty_ErrorThrown(Boolean secureEventStore){
            TimeSpan deadline = TimeSpan.FromMinutes(2);
            TimeSpan retryTimeout = TimeSpan.FromMinutes(6);

            await this.EventStoreDockerHelper.StartContainers(secureEventStore, $"EventStoreContext_RunTransientQuery_ResultIsEmpty_ErrorThrown{secureEventStore}");

            IEventStoreContext context = this.CreateContext(secureEventStore, deadline);

            Guid aggreggateId = Guid.NewGuid();
            String streamName = $"TestStream-{aggreggateId:N}";

            EstateCreatedEvent event1 = new(aggreggateId, "Test Estate 1");
            EstateCreatedEvent event2 = new(aggreggateId, "Test Estate 2");
            List<IDomainEvent> domainEvents = new(){
                                                       event1,
                                                       event2
                                                   };

            IEventDataFactory factory = new EventDataFactory();
            EventData[] events = factory.CreateEventDataList(domainEvents);

            await this.InsertEvents(context, streamName, events, deadline, retryTimeout);
            await this.ReadEvents(context, streamName, events.Length, deadline, retryTimeout);

            String query = "fromStream('$et-EstateCreatedEvent')\r\n  .when({\r\n      $init: function (s, e)\r\n        {\r\n            return {\r\n                \r\n            };\r\n        },\r\n        \"EstateCreatedEvent\": function(s, e){\r\n          }\r\n  });";

            await this.RunTransientQuery(context, query, 3, deadline, retryTimeout);
        }

        private IEventStoreContext CreateContext(Boolean secureEventStore, TimeSpan? deadline = null){
            EventStoreClientSettings settings = this.EventStoreDockerHelper.CreateEventStoreClientSettings(secureEventStore, deadline);

            EventStoreClient client = new(settings);
            EventStoreProjectionManagementClient projectionManagementClient = new(settings);
            IEventStoreContext context = new EventStoreContext(client, projectionManagementClient, deadline);
            return context;
        }

        #endregion
        
        private async Task InsertEvents(IEventStoreContext context, String streamName, EventData[] events, TimeSpan deadline, TimeSpan retryTimeout){
            await Retry.For(async () => {
                                await context.InsertEvents(streamName, -1, events.ToList(), CancellationToken.None);
                            },
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

        private async Task ReadEvents(IEventStoreContext context, String streamName, Int32 eventCount, TimeSpan deadline, TimeSpan retryTimeout){
            await Retry.For(async () => {
                                List<ResolvedEvent> resolvedEvents = null;
                                resolvedEvents = await context.ReadEvents(streamName, 0, CancellationToken.None);

                                resolvedEvents.Count.ShouldBe(eventCount);
                            },
                            retryTimeout,
                            deadline);
        }

        private async Task ReadEventsBackwards(IEventStoreContext context, String streamName, Int32? eventCount, TimeSpan deadline, TimeSpan retryTimeout){
            await Retry.For(async () => {

                                IList<ResolvedEvent> resolvedEvents = await context.GetEventsBackward(streamName, eventCount.GetValueOrDefault(1), CancellationToken.None);

                                if (eventCount == null){
                                    resolvedEvents.ShouldBeEmpty();
                                }
                                else{
                                    resolvedEvents.Count.ShouldBe(eventCount.GetValueOrDefault(0));
                                }
                            },
                            retryTimeout,
                            deadline);
        }

        private async Task<String> RunTransientQuery(IEventStoreContext context, String query, Int32 checkType, TimeSpan? deadline = null, TimeSpan? retryTimeout = null){
            Int32 counter = 0;
            String queryResult = null;
            await Retry.For(async () => {
                                counter++;
                                this.EventStoreDockerHelper.Trace($"Inside Retry Counter [{counter}] Check Type [{checkType}]");

                                if (checkType == 1){
                                    
                                    Exception ex = Should.Throw<Exception>(async () => {
                                        this.EventStoreDockerHelper.Trace($"About to call RunTransientQuery");
                                        await context.RunTransientQuery(query, CancellationToken.None);
                                                                           });
                                    this.EventStoreDockerHelper.Trace($"{ex.Message}");
                                    ex.Message.ShouldBe("Faulted");
                                }
                                else{
                                    queryResult = await context.RunTransientQuery(query, CancellationToken.None);

                                    switch(checkType){
                                        case 2: // Not Null or Empty
                                            queryResult.ShouldNotBeNullOrEmpty();
                                            break;
                                        case 3: // Empty
                                            queryResult.ShouldBeEmpty();
                                            break;
                                    }
                                }
                            },
                            retryTimeout,
                            deadline);
            return queryResult; 
            
        }
    }
}