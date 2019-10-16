namespace Shared.DomainDrivenDesign.EventStore
{
    using System;

    /// <summary>
    /// The Live Process Started Event Handler deletgate
    /// </summary>
    /// <param name="catchUpSubscriptionId"></param>
    public delegate void LiveProcessStartedEventHandler(Guid catchUpSubscriptionId);
}