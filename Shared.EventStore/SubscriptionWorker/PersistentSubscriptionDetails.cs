namespace Shared.EventStore.SubscriptionWorker;

using System;
using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public record PersistentSubscriptionDetails
{

    public PersistentSubscriptionDetails(String streamName,
                                         String groupName)
    {
        this.StreamName = streamName;
        this.GroupName = groupName;
    }



    public String GroupName { get; init; }

    public Int32 InflightMessages { get; init; }

    public String StreamName { get; init; }



    public override String ToString() => $"{this.StreamName}-{this.GroupName}";

}