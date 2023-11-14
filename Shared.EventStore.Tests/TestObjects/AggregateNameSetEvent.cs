namespace Shared.EventStore.Tests.TestObjects;

using System;
using DomainDrivenDesign.EventSourcing;

public record AggregateNameSetEvent : DomainEvent
{
    public string AggregateName { get; init; }

    public AggregateNameSetEvent(Guid aggregateId,
                                 Guid eventId,
                                 string aggregateName) : base(aggregateId, eventId)
    {
        AggregateName = aggregateName;
    }
}