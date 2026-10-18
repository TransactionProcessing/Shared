namespace Shared.EventStore.Extensions;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public class SubscriptionWorkersRoot
{

    public Boolean InternalSubscriptionService { get; set; }

    public Int32 InternalSubscriptionServiceCacheDuration { get; set; }

    public Int32 PersistentSubscriptionPollingInSeconds { get; set; }

    public List<SubscriptionWorkerConfig> SubscriptionWorkers { get; set; }

}