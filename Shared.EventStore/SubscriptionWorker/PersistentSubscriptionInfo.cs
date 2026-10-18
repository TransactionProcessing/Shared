namespace Shared.EventStore.SubscriptionWorker;

using System;
using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public class PersistentSubscriptionInfo
{

    public String GroupName { get; set; }

    public String StreamName { get; set; }

}