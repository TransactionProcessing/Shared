﻿namespace Shared.DomainDrivenDesign.EventSourcing
{
    using System;
    using Newtonsoft.Json;

    public abstract class DomainEvent : IDomainEvent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DomainEvent"/> class.
        /// </summary>
        /// <param name="aggregateId">The aggregate identifier.</param>
        protected DomainEvent(Guid aggregateId) : this(aggregateId, Guid.NewGuid())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DomainEvent"/> class.
        /// </summary>
        /// <param name="aggregateId">The aggregate identifier.</param>
        /// <param name="eventId">The event identifier.</param>
        protected DomainEvent(Guid aggregateId,
                              Guid eventId)

        {
            this.AggregateId = aggregateId;
            this.EventId = eventId;
            this.EventType = DomainHelper.GetEventTypeName(this.GetType());
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DomainEvent"/> class.
        /// </summary>
        protected DomainEvent()
        {
        }

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
        /// Gets the event number.
        /// </summary>
        /// <value>
        /// The event number.
        /// </value>
        [JsonIgnore]
        public Int64 EventNumber { get; init; }

        /// <summary>
        /// The event type
        /// </summary>
        /// <value>
        /// The type of the event.
        /// </value>
        [JsonIgnore]
        public String EventType { get; init; }

        /// <summary>
        /// Gets the event identifier.
        /// </summary>
        /// <value>
        /// The event identifier.
        /// </value>
        [JsonIgnore]
        public Guid EventId { get; init; }

        /// <summary>
        /// Gets the event timestamp.
        /// </summary>
        /// <value>
        /// The event timestamp.
        /// </value>
        [JsonIgnore]
        public DateTimeOffset EventTimestamp { get; init; }
    }
}
