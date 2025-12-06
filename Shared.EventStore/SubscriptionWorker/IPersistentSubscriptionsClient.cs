using KurrentDB.Client;

namespace Shared.EventStore.SubscriptionWorker;

using System;
using System.Threading;
using System.Threading.Tasks;

public interface IPersistentSubscriptionsClient
{
    #region Methods

    Task<KurrentDB.Client.PersistentSubscription> SubscribeAsync(String stream,
                                                                 String group,
                                                                 Func<KurrentDB.Client.PersistentSubscription, ResolvedEvent, Int32?,
                                                                     CancellationToken, Task> eventAppeared,
                                                                 Action<KurrentDB.Client.PersistentSubscription, SubscriptionDroppedReason,
                                                                     Exception?> subscriptionDropped,
                                                                 UserCredentials? userCredentials,
                                                                 Int32 bufferSize,
                                                                 CancellationToken cancellationToken);

    #endregion
}