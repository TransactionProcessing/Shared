namespace Shared.EventStore.SubscriptionWorker;

using System;
using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public class PersistentSubscriptionInfo
{
    #region Properties

    public String GroupName { get; set; }

    public String StreamName { get; set; }

    #endregion
}