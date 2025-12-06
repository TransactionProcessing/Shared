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
    #region Fields

    private readonly KurrentDBPersistentSubscriptionsClient SubscriptionsClient;

    #endregion

    #region Constructors

    public EventStorePersistentSubscriptionsClient(KurrentDBPersistentSubscriptionsClient subscriptionsClient) {
        this.SubscriptionsClient = subscriptionsClient;
    }

    #endregion

    #region Methods

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

    #endregion
}