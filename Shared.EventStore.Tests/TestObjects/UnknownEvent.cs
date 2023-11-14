namespace Shared.EventStore.Tests.TestObjects;

using System;
using DomainDrivenDesign.EventSourcing;

public record UnknownEvent : DomainEvent
{
    public string AggregateName { get; init; }

    public UnknownEvent(Guid aggregateId,
                        Guid eventId,
                        string aggregateName) : base(aggregateId, eventId)
    {
        AggregateName = aggregateName;
    }
}