namespace Shared.EventStore.SubscriptionWorker
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using global::EventStore.Client;

    public class EventStorePersistentSubscriptionsClient : IPersistentSubscriptionsClient
    {
        #region Fields

        private readonly global::EventStore.Client.EventStorePersistentSubscriptionsClient SubscriptionsClient;

        #endregion

        #region Constructors

        public EventStorePersistentSubscriptionsClient(global::EventStore.Client.EventStorePersistentSubscriptionsClient subscriptionsClient) {
            this.SubscriptionsClient = subscriptionsClient;
        }

        #endregion

        #region Methods

        public Task<global::EventStore.Client.PersistentSubscription> SubscribeAsync(String stream,
                                                                                     String group,
                                                                                     Func<global::EventStore.Client.PersistentSubscription, ResolvedEvent, Int32?,
                                                                                         CancellationToken, Task> eventAppeared,
                                                                                     Action<global::EventStore.Client.PersistentSubscription, SubscriptionDroppedReason,
                                                                                         Exception?>? subscriptionDropped,
                                                                                     UserCredentials? userCredentials,
                                                                                     Int32 bufferSize,
                                                                                     CancellationToken cancellationToken) {
            return this.SubscriptionsClient.SubscribeToStreamAsync(stream, group, eventAppeared, subscriptionDropped, userCredentials, bufferSize, cancellationToken);
        }

        #endregion
    }
}