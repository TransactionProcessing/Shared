namespace Shared.DomainDrivenDesign.EventSourcing
{
    using System;
    using Newtonsoft.Json;

    /// <summary>
    /// 
    /// </summary>
    public abstract class DomainEvent
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="DomainEvent"/> class.
        /// </summary>
        public DomainEvent()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DomainEvent" /> class.
        /// </summary>
        /// <param name="aggregateId">The aggregate identifier.</param>
        /// <param name="eventId">The event identifier.</param>
        protected DomainEvent(Guid aggregateId, Guid eventId)
        {
            this.AggregateId = aggregateId;
            this.EventId = eventId;
            this.EventCreatedDateTime = DateTime.Now;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the aggregate identifier.
        /// </summary>
        /// <value>
        /// The aggregate identifier.
        /// </value>
        [JsonProperty]
        public Guid AggregateId { get; private set; }

        /// <summary>
        /// Gets the event identifier.
        /// </summary>
        /// <value>
        /// The event identifier.
        /// </value>
        [JsonProperty]
        public Guid EventId { get; private set; }

        /// <summary>
        /// Gets the event created date time.
        /// </summary>
        /// <value>
        /// The event created date time.
        /// </value>
        [JsonProperty]
        public DateTime EventCreatedDateTime { get; private set; }

        #endregion
    }
}
