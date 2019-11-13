namespace Shared.DomainDrivenDesign.EventStore
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using EventSourcing;

    /// <summary>
    /// 
    /// </summary>
    public abstract class Aggregate : IAggregate
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Aggregate" /> class.
        /// Used when restoring a snapshot to a specific version
        /// </summary>
        /// <param name="aggregateId">The aggregate identifier.</param>
        /// <param name="version">The version.</param>
        protected Aggregate(Guid aggregateId,
                            Int32 version)
        {
            this.AggregateId = aggregateId;
            this.Version = version;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Aggregate" /> class.
        /// </summary>
        protected Aggregate()
        {
            this.BaseInitialise();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the aggregate identifier.
        /// </summary>
        /// <value>
        /// The aggregate identifier.
        /// </value>
        public Guid AggregateId { get; set; }

        /// <summary>
        /// Gets or sets the version.
        /// </summary>
        /// <value>
        /// The version.
        /// </value>
        public Int32 Version { get; private set; }

        /// <summary>
        /// Gets or sets the event history.
        /// </summary>
        /// <value>The event history.</value>
        private List<DomainEvent> EventHistory { get; set; }

        /// <summary>
        /// Gets or sets the events still to be recorded to the event DomainEvent store.
        /// </summary>
        /// <value>The pending events.</value>
        private List<DomainEvent> PendingEvents { get; set; }

        #endregion

        #region Methods

        /// <summary>
        /// Applies the specified historic event.
        /// </summary>
        /// <param name="historicEvent">The historic event.</param>
        /// <exception cref="InvalidOperationException"></exception>
        public void Apply(DomainEvent historicEvent)
        {
            // Sanity check that the event is for this aggregate
            if (historicEvent.AggregateId != this.AggregateId)
            {
                throw new InvalidOperationException($"An attempt was made to load an event from aggregate [{historicEvent.AggregateId}] into [{this.AggregateId}]");
            }

            // We increment the version numbewr when are replaying events from persistence NOT from public aggregate calls.
            this.Version++;

            // Play the event 
            this.PlayEvent(historicEvent);

            // Add this to the historic events
            this.EventHistory.Add(historicEvent);
        }

        /// <summary>
        /// Gets the pending events.
        /// </summary>
        /// <returns></returns>
        public List<DomainEvent> GetPendingEvents()
        {
            return this.PendingEvents;
        }

        /// <summary>
        /// Gets the name of the stream.
        /// </summary>
        /// <returns></returns>
        public virtual String GetStreamName()
        {
            return $"{this.GetType().Name}-{this.AggregateId.ToString().Replace("-", string.Empty)}";
        }

        /// <summary>
        /// Applies the and pend.
        /// </summary>
        /// <param name="domainEvent">The domain event.</param>
        protected void ApplyAndPend(DomainEvent domainEvent)
        {
            if (this.IsEventDuplicate(domainEvent))
            {
                //Event has already been processed
                return;
            }

            // Play the event to apply the changes
            this.PlayEvent(domainEvent);

            // Add the event to pending events
            this.PendingEvents.Add(domainEvent);
        }

        protected abstract Object GetMetadata();

        /// <summary>
        /// Initialises this instance.
        /// </summary>
        protected virtual void Initialise()
        {
        }

        /// <summary>
        /// Plays the event.
        /// </summary>
        /// <param name="domainEvent">The domain event.</param>
        protected abstract void PlayEvent(DomainEvent domainEvent);

        /// <summary>
        /// Bases the initialise.
        /// </summary>
        private void BaseInitialise()
        {
            this.Version = -1;
            this.PendingEvents = new List<DomainEvent>();
            this.EventHistory = new List<DomainEvent>();

            // Call any init needing done by inherited classes
            this.Initialise();
        }

        public Object GetAggregateMetadata()
        {
            return this.GetMetadata();
        }

        /// <summary>
        /// Determines whether [is event duplicate] [the specified domain event].
        /// </summary>
        /// <param name="domainEvent">The domain event.</param>
        /// <returns>
        ///   <c>true</c> if [is event duplicate] [the specified domain event]; otherwise, <c>false</c>.
        /// </returns>
        private Boolean IsEventDuplicate(DomainEvent domainEvent)
        {
            if (this.EventHistory.Any(x => x.EventId == domainEvent.EventId))
            {
                return true;
            }

            if (this.PendingEvents.Any(x => x.EventId == domainEvent.EventId))
            {
                return true;
            }

            return false;
        }

        #endregion
    }
}