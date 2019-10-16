namespace Shared.DomainDrivenDesign.EventStore
{
    using System;
    using global::EventStore.ClientAPI;

    /// <summary>
    /// The Subscription Dropped Event Handler delegate
    /// </summary>
    /// <param name="streamName"></param>
    /// <param name="groupName"></param>
    /// <param name="subscriptionType"></param>
    /// <param name="subscriptionDropReason"></param>
    /// <param name="exception"></param>
    /// <param name="subscriptionGroupId"></param>
    public delegate void SubscriptionDroppedEventHandler(String streamName, String groupName, SubscriptionType subscriptionType, SubscriptionDropReason subscriptionDropReason, Exception exception, Guid subscriptionGroupId);
}