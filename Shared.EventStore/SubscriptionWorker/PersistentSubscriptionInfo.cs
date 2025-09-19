namespace Shared.EventStore.SubscriptionWorker;

using System;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

[ExcludeFromCodeCoverage]
public class PersistentSubscriptionInfo
{
    #region Properties

    public String GroupName { get; set; }

    [JsonProperty("eventStreamId")]
    public String StreamName { get; set; }

    #endregion
}