namespace Shared.DomainDrivenDesign.EventSourcing;

using System;

/// <summary>
/// 
/// </summary>
public interface IDomainEvent
{
    /// <summary>
    /// Gets the aggregate identifier.
    /// </summary>
    /// <value>
    /// The aggregate identifier.
    /// </value>
    public Guid AggregateId { get; init; }

    /// <summary>
    /// Gets the aggregate version.
    /// </summary>
    /// <value>
    /// The aggregate version.
    /// </value>
    public Int64 AggregateVersion { get; init; }

    /// <summary>
    /// Gets the event number.
    /// </summary>
    /// <value>
    /// The event number.
    /// </value>
    public Int64 EventNumber { get; init; }

    /// <summary>
    /// Gets the type of the event.
    /// </summary>
    /// <value>
    /// The type of the event.
    /// </value>
    public String EventType { get; init; }

    /// <summary>
    /// Gets the event identifier.
    /// </summary>
    /// <value>
    /// The event identifier.
    /// </value>
    public Guid EventId { get; init; }

    /// <summary>
    /// Gets the event timestamp.
    /// </summary>
    /// <value>
    /// The event timestamp.
    /// </value>
    public DateTimeOffset EventTimestamp { get; init; }
}