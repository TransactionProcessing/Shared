namespace Shared.EventStore.SubscriptionWorker
{
    using System;
    using Newtonsoft.Json;

    public class PersistentSubscriptionInfo
    {
        #region Properties

        public String GroupName { get; set; }

        [JsonProperty("eventStreamId")]
        public String StreamName { get; set; }

        #endregion
    }
}