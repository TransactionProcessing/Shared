namespace Shared.EventStore.Extensions;

using System;
using System.Collections.Generic;

public class SubscriptionWorkersRoot
{
    #region Properties

    public Boolean InternalSubscriptionService { get; set; }

    public Int32 InternalSubscriptionServiceCacheDuration { get; set; }

    public Int32 PersistentSubscriptionPollingInSeconds { get; set; }

    public List<SubscriptionWorkerConfig> SubscriptionWorkers { get; set; }

    #endregion
}