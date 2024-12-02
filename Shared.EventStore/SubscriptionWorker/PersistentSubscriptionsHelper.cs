namespace Shared.EventStore.SubscriptionWorker
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading.Tasks;
    using global::EventStore.Client;

    [ExcludeFromCodeCoverage]
    public static class PersistentSubscriptionsHelper
    {
        #region Methods

        public static PersistentSubscriptions Update(this PersistentSubscriptions persistentSubscriptions, List<PersistentSubscriptionInfo> subscriptions)
        {
            return persistentSubscriptions with
                   {
                       LastTimeRefreshed = DateTime.Now,
                       InitialState = false,
                       PersistentSubscriptionInfo = subscriptions
                   };
        }

        internal static async Task AckEvent(global::EventStore.Client.PersistentSubscription persistentSubscription, ResolvedEvent resolvedEvent)
        {
            if (persistentSubscription != null)
            {
                await persistentSubscription.Ack(resolvedEvent);
            }
        }

        internal static Boolean SilentlyHandleEvent(this ResolvedEvent resolvedEvent) =>
            resolvedEvent.Event switch
            {
                null => true,
                _ when resolvedEvent.Event.EventType.StartsWith("$") => true,
                _ => false
            };

        #endregion
    }
}