namespace Shared.DomainDrivenDesign.EventSourcing;

using System;


public record DomainEvent : IDomainEvent
{
    public DomainEvent(Guid aggregateId,
                       Guid eventId) {
        this.AggregateId = aggregateId;
        this.EventId = eventId;
        this.EventType = DomainHelper.GetEventTypeName(this.GetType());
    }

    #region Properties

    public Guid AggregateId { get; init; }

    public Int64 AggregateVersion { get; init; }

    public Guid EventId { get; init; }

    public Int64 EventNumber { get; init; }

    public DateTimeOffset EventTimestamp { get; init; }

    public String EventType { get; init; }

    #endregion
}