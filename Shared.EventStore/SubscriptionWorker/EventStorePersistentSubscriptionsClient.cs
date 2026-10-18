using KurrentDB.Client;

namespace Shared.EventStore.SubscriptionWorker;

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using global::EventStore.Client;

[ExcludeFromCodeCoverage]
public class EventStorePersistentSubscriptionsClient : IPersistentSubscriptionsClient
{

    private readonly KurrentDBPersistentSubscriptionsClient SubscriptionsClient;



    public EventStorePersistentSubscriptionsClient(KurrentDBPersistentSubscriptionsClient subscriptionsClient) {
        this.SubscriptionsClient = subscriptionsClient;
    }



    public Task<KurrentDB.Client.PersistentSubscription> SubscribeAsync(String stream,
                                                                                String group,
                                                                                Func<KurrentDB.Client.PersistentSubscription, ResolvedEvent, Int32?,
                                                                                    CancellationToken, Task> eventAppeared,
                                                                                Action<KurrentDB.Client.PersistentSubscription, SubscriptionDroppedReason,
                                                                                    Exception?>? subscriptionDropped,
                                                                                UserCredentials? userCredentials,
                                                                                Int32 bufferSize,
                                                                                CancellationToken cancellationToken) {
        return this.SubscriptionsClient.SubscribeToStreamAsync(stream, group, eventAppeared, subscriptionDropped, userCredentials, bufferSize, cancellationToken);
    }

}