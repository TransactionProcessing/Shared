namespace Shared.EventStore.SubscriptionWorker
{
    using System;
    using System.Collections.Generic;

    public record PersistentSubscriptions
    {
        public PersistentSubscriptions()
        {
            this.PersistentSubscriptionInfo = new List<PersistentSubscriptionInfo>();
            this.InitialState = true;
        }

        public Boolean InitialState { get; init; }
        public DateTime LastTimeRefreshed { get; set; }
        public List<PersistentSubscriptionInfo> PersistentSubscriptionInfo { get; init; }
    }
}