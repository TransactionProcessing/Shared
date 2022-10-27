namespace Driver
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using EventStore.Client;
    using Shared.DomainDrivenDesign.EventSourcing;
    using Shared.EventStore.Aggregate;
    using Shared.EventStore.EventHandling;
    using Shared.EventStore.EventStore;
    using Shared.EventStore.SubscriptionWorker;
    using Shared.IntegrationTesting;
    using Shared.Logger;

    internal class Program
    {
        #region Methods

        internal static EventStoreClientSettings ConfigureEventStoreSettings() {
            EventStoreClientSettings settings = new(); //EventStoreClientSettings.Create("esdb://127.0.0.1:2113?tls=trur");

            settings.CreateHttpMessageHandler = () => new SocketsHttpHandler {
                                                                                 SslOptions = {
                                                                                                  RemoteCertificateValidationCallback = (sender,
                                                                                                      certificate,
                                                                                                      chain,
                                                                                                      errors) => true,
                                                                                              }
                                                                             };

            settings.ConnectivitySettings = EventStoreClientConnectivitySettings.Default;
            settings.ConnectivitySettings.Insecure = false;
            settings.DefaultCredentials = new UserCredentials("admin", "changeit");
            settings.ConnectivitySettings.Address = new Uri("esdb://127.0.0.1:2113?tls=true&tlsVerifyCert=false");

            return settings;
        }

        private static async Task Main(String[] args) {
            //await Program.SubscriptionsTest();

            EventStoreClientSettings settings = Program.ConfigureEventStoreSettings();

            EventStoreClient client = new(settings);

            EventStoreProjectionManagementClient projectionManagementClient = new EventStoreProjectionManagementClient(settings);
            
            var x = await projectionManagementClient.GetStatusAsync("$by_category", cancellationToken: CancellationToken.None);

            IEventStoreContext context = new EventStoreContext(client, projectionManagementClient);
            IAggregateRepository<TestAggregate, DomainEvent> aggregateRepository = new AggregateRepository<TestAggregate, DomainEvent>(context);

            Guid aggregateId = Guid.Parse("02398284-8284-8284-8284-165402398284");
            TestAggregate aggregate = await aggregateRepository.GetLatestVersion(aggregateId, CancellationToken.None);

            aggregate.SetAggregateName("Test Name");
            //aggregate.SetAggregateName("Test Name1");
            //aggregate.SetAggregateName("Test Name2");
            //aggregate.SetAggregateName("Test Name3");
            //aggregate.SetAggregateName("Test Name4");

            await aggregateRepository.SaveChanges(aggregate, CancellationToken.None);

            //TestDockerHelper t = new TestDockerHelper();
            //await t.StartContainersForScenarioRun("");
        }

        private static async Task SubscriptionsTest() {
            String eventStoreConnectionString = "esdb://127.0.0.1:2113?tls=false";
            Int32 inflightMessages = 1;
            Int32 persistentSubscriptionPollingInSeconds = 10;
            String filter = String.Empty;
            String ignore = String.Empty;
            String streamName = String.Empty;
            Int32 cacheDuration = 0;

            ISubscriptionRepository subscriptionRepository = SubscriptionRepository.Create(eventStoreConnectionString, cacheDuration);

            //((SubscriptionRepository)subscriptionRepository).Trace += (sender, s) => Extensions.log(TraceEventType.Information, "REPOSITORY", s);

            // init our SubscriptionRepository
            subscriptionRepository.PreWarm(CancellationToken.None).Wait();

            IDomainEventHandlerResolver eventHandlerResolver = new DomainEventHandlerResolver(new Dictionary<String, String[]>(), null);

            SubscriptionWorker concurrentSubscriptions = SubscriptionWorker.CreateConcurrentSubscriptionWorker(Program.ConfigureEventStoreSettings(),
                                                                                                               eventHandlerResolver,
                                                                                                               subscriptionRepository,
                                                                                                               inflightMessages,
                                                                                                               persistentSubscriptionPollingInSeconds);

            //concurrentSubscriptions.Trace += (_, args) => Extensions.concurrentLog(TraceEventType.Information, args.Message);
            //concurrentSubscriptions.Warning += (_, args) => Extensions.concurrentLog(TraceEventType.Warning, args.Message);
            //concurrentSubscriptions.Error += (_, args) => Extensions.concurrentLog(TraceEventType.Error, args.Message);

            if (!String.IsNullOrEmpty(ignore)) {
                concurrentSubscriptions = concurrentSubscriptions.IgnoreSubscriptions(ignore);
            }

            if (!String.IsNullOrEmpty(filter)) {
                //NOTE: Not overly happy with this design, but
                //the idea is if we supply a filter, this overrides ignore
                concurrentSubscriptions = concurrentSubscriptions.FilterSubscriptions(filter).IgnoreSubscriptions(null);
            }

            if (!String.IsNullOrEmpty(streamName)) {
                concurrentSubscriptions = concurrentSubscriptions.FilterByStreamName(streamName);
            }

            concurrentSubscriptions.StartAsync(CancellationToken.None).Wait();
        }

        #endregion

        #region Others

        public class ConsoleLogger : ILogger
        {
            #region Properties

            public Boolean IsInitialised { get; set; }

            #endregion

            #region Methods

            public void LogCritical(Exception exception) {
                Console.WriteLine(exception);
            }

            public void LogDebug(String message) {
                Console.WriteLine(message);
            }

            public void LogError(Exception exception) {
                Console.WriteLine(exception);
            }

            public void LogInformation(String message) {
                Console.WriteLine(message);
            }

            public void LogTrace(String message) {
                Console.WriteLine(message);
            }

            public void LogWarning(String message) {
                Console.WriteLine(message);
            }

            #endregion
        }

        //public class TestDockerHelper : DockerHelper
        //{
        //    #region Constructors

        //    public TestDockerHelper() {
        //        this.Logger = new ConsoleLogger();
        //    }

        //    #endregion

        //    #region Methods

        //    public override async Task StartContainersForScenarioRun(String scenarioName) {
        //        //await this.LoadEventStoreProjections(2113);

        //        List<(String, String, Int32)> subs = new List<(String, String, Int32)>();
        //        subs.Add(("TestStream", "TestGroup1", 0));
        //        subs.Add(("TestStream", "TestGroup2", 0));
        //        subs.Add(("TestStream1", "TestGroup3", 1));
        //        await this.PopulateSubscriptionServiceConfiguration(2113, subs);
        //    }

        //    public override async Task StopContainersForScenarioRun() {
        //    }

        //    #endregion
        //}

        #endregion
    }
}