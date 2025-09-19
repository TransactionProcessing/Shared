namespace Shared.DomainDrivenDesign.EventSourcing;

using System;
using Newtonsoft.Json;

#region Others

public record DomainEvent : IDomainEvent
{
    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainEventRecord" /> class.
    /// </summary>
    /// <param name="aggregateId">The aggregate identifier.</param>
    /// <param name="eventId">The event identifier.</param>
    public DomainEvent(Guid aggregateId,
                       Guid eventId) {
        this.AggregateId = aggregateId;
        this.EventId = eventId;
        this.EventType = DomainHelper.GetEventTypeName(this.GetType());
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets the aggregate identifier.
    /// </summary>
    /// <value>
    /// The aggregate identifier.
    /// </value>
    [JsonIgnore]
    public Guid AggregateId { get; init; }

    /// <summary>
    /// Gets the aggregate version.
    /// </summary>
    /// <value>
    /// The aggregate version.
    /// </value>
    [JsonIgnore]
    public Int64 AggregateVersion { get; init; }

    /// <summary>
    /// Gets the event identifier.
    /// </summary>
    /// <value>
    /// The event identifier.
    /// </value>
    [JsonIgnore]
    public Guid EventId { get; init; }

    /// <summary>
    /// Gets the event number.
    /// </summary>
    /// <value>
    /// The event number.
    /// </value>
    [JsonIgnore]
    public Int64 EventNumber { get; init; }

    /// <summary>
    /// Gets the event timestamp.
    /// </summary>
    /// <value>
    /// The event timestamp.
    /// </value>
    [JsonIgnore]
    public DateTimeOffset EventTimestamp { get; init; }

    [JsonIgnore]
    public String EventType { get; init; }

    #endregion
}

#endregion