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
            settings.ConnectivitySettings.Address = new Uri("esdb://192.168.0.133:2113?tls=true&tlsVerifyCert=false");

            return settings;
        }

        private static async Task Main(String[] args) {
            //await Program.SubscriptionsTest();
            await Program.QueryTest();

            //TestDockerHelper t = new TestDockerHelper();
            //await t.StartContainersForScenarioRun("");
        }

        private static async Task QueryTest(){
            String query = "fromStream('$et-EstateCreatedEvent')\r\n  .when({\r\n      $init: function (s, e)\r\n        {\r\n            return {\r\n                estates:[]\r\n            };\r\n        },\r\n        \"EstateCreatedEvent\": function(s,e){\r\n          s.estates.push(e.data.estateName);\r\n        }\r\n  });";
            EventStoreClient client = new EventStoreClient(Program.ConfigureEventStoreSettings());
            EventStoreProjectionManagementClient projection = new EventStoreProjectionManagementClient(Program.ConfigureEventStoreSettings());
            EventStoreContext context = new EventStoreContext(client, projection);

            var result = await context.RunTransientQuery(query, CancellationToken.None);
            Console.WriteLine(result);
        }

        private static async Task SubscriptionsTest() {
            String eventStoreConnectionString = "esdb://127.0.0.1:2113?tls=false";
            //Int32 inflightMessages = 50;
            //Int32 persistentSubscriptionPollingInSeconds = 10;
            //Int32 cacheDuration = 0;

            //String 

            //ISubscriptionRepository subscriptionRepository = SubscriptionRepository.Create(eventStoreConnectionString, cacheDuration);

            ////((SubscriptionRepository)subscriptionRepository).Trace += (sender, s) => Extensions.log(TraceEventType.Information, "REPOSITORY", s);

            //// init our SubscriptionRepository
            //subscriptionRepository.PreWarm(CancellationToken.None).Wait();

            //IDomainEventHandlerResolver eventHandlerResolver = new DomainEventHandlerResolver(new Dictionary<String, String[]>(), null);

            //SubscriptionWorker concurrentSubscriptions = SubscriptionWorker.CreateSubscriptionWorker(Program.ConfigureEventStoreSettings(),
            //                                                                                                   eventHandlerResolver,
            //                                                                                                   subscriptionRepository,
            //                                                                                                   inflightMessages,
            //                                                                                                   persistentSubscriptionPollingInSeconds);

            //concurrentSubscriptions.Trace += (_, args) => Extensions.concurrentLog(TraceEventType.Information, args.Message);
            //concurrentSubscriptions.Warning += (_, args) => Extensions.concurrentLog(TraceEventType.Warning, args.Message);
            //concurrentSubscriptions.Error += (_, args) => Extensions.concurrentLog(TraceEventType.Error, args.Message);

            

            //concurrentSubscriptions.StartAsync(CancellationToken.None).Wait();
        }

        #endregion

        #region Others
        
        #endregion
    }

    public record AggregateNameSetEventTest : DomainEvent
    {
        public String AggregateName { get; init; }

        public AggregateNameSetEventTest(Guid aggregateId,
                                         Guid eventId,
                                         String aggregateName) : base(aggregateId, eventId)
        {
            this.AggregateName = aggregateName;
        }
    }
}