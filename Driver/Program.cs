using System.Diagnostics;

namespace Driver;

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

    internal static EventStoreClientSettings ConfigureEventStoreSettings()
    {
        EventStoreClientSettings settings = new();

        settings.CreateHttpMessageHandler = () => new SocketsHttpHandler
        {
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

    public static void TestFunction(String param1, String param2, String param3, String param4, String param5, String param6, String param7, String param8, String param9, String param10, String param11)
    {
    }

    private static async Task Main(String[] args)
    {
        Logger.Initialise(NullLogger.Instance);
        await Program.SubscriptionsTest();

        Console.ReadKey();
    }

    private static async Task QueryTest()
    {
        String query = "fromStream('$et-EstateCreatedEvent')\r\n  .when({\r\n      $init: function (s, e)\r\n        {\r\n            return {\r\n                estates:[]\r\n            };\r\n        },\r\n        \"EstateCreatedEvent\": function(s,e){\r\n          s.estates.push(e.data.estateName);\r\n        }\r\n  });";
        EventStoreClient client = new(Program.ConfigureEventStoreSettings());
        EventStoreProjectionManagementClient projection = new(Program.ConfigureEventStoreSettings());
        EventStoreContext context = new(client, projection);

        var result = await context.RunTransientQuery(query, CancellationToken.None);
        Console.WriteLine(result);
    }

    private static async Task SubscriptionsTest()
    {
        String eventStoreConnectionString = "esdb://127.0.0.1:2113?tls=false";
        Int32 inflightMessages = 50;
        Int32 persistentSubscriptionPollingInSeconds = 10;
        Int32 cacheDuration = 0;

        ISubscriptionRepository subscriptionRepository = SubscriptionRepository.Create(eventStoreConnectionString, cacheDuration);

        // init our SubscriptionRepository
        subscriptionRepository.PreWarm(CancellationToken.None).Wait();

        IDomainEventHandlerResolver eventHandlerResolver = new DomainEventHandlerResolver(new Dictionary<String, String[]>(), null);

        SubscriptionWorker concurrentSubscriptions = SubscriptionWorker.CreateSubscriptionWorker(
            eventStoreConnectionString, eventHandlerResolver,
            subscriptionRepository, inflightMessages, persistentSubscriptionPollingInSeconds);

        concurrentSubscriptions.Trace += (_, args) => Console.WriteLine($"{TraceEventType.Information}|{args.Message}");
        concurrentSubscriptions.Warning += (_, args) => Console.WriteLine($"{TraceEventType.Warning}|{args.Message}");
        concurrentSubscriptions.Error += (_, args) => Console.WriteLine($"{TraceEventType.Error}|{args.Message}");



        concurrentSubscriptions.StartAsync(CancellationToken.None).Wait();
    }

    #endregion
}
