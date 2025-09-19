namespace Shared.EventStore.SubscriptionWorker;

using System;
using System.Threading;
using System.Threading.Tasks;
using global::EventStore.Client;

public interface IPersistentSubscriptionsClient
{
    #region Methods

    Task<global::EventStore.Client.PersistentSubscription> SubscribeAsync(String stream,
                                                                          String group,
                                                                          Func<global::EventStore.Client.PersistentSubscription, ResolvedEvent, Int32?,
                                                                              CancellationToken, Task> eventAppeared,
                                                                          Action<global::EventStore.Client.PersistentSubscription, SubscriptionDroppedReason,
                                                                              Exception?> subscriptionDropped,
                                                                          UserCredentials? userCredentials,
                                                                          Int32 bufferSize,
                                                                          CancellationToken cancellationToken);

    #endregion
}