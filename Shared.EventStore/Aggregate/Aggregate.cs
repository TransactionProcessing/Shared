namespace Shared.EventStore.Aggregate
{
    using System;
    using System.Collections.Generic;
    using DomainDrivenDesign.EventSourcing;

    /// <summary>
    /// 
    /// </summary>
    public abstract class Aggregate
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Aggregate" /> class.
        /// </summary>
        protected Aggregate()
        {
            this.EventHistory = new Dictionary<Guid, IDomainEvent>();
            this.PendingEvents = new List<IDomainEvent>();
            this.Version = -1L;
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
        /// Gets the version.
        /// </summary>
        /// <value>
        /// The version.
        /// </value>
        public AggregateVersion Version { get; internal set; }

        /// <summary>
        /// Gets or sets the event history.
        /// </summary>
        /// <value>
        /// The event history.
        /// </value>
        internal Dictionary<Guid, IDomainEvent> EventHistory { get; set; }

        /// <summary>
        /// Gets or sets the pending events.
        /// </summary>
        /// <value>
        /// The pending events.
        /// </value>
        internal IList<IDomainEvent> PendingEvents { get; set; }

        #endregion

        #region Methods

        /// <summary>
        /// Applies the and append.
        /// </summary>
        /// <param name="domainEvent">The domain event.</param>
        protected internal void ApplyAndAppend(IDomainEvent domainEvent)
        {
            if (domainEvent == null) return; //Silently handled

            try
            {
                if (this.IsEventDuplicate(domainEvent.EventId))
                    return;

                this.PlayEvent(domainEvent);
                this.PendingEvents.Add(domainEvent);
            }
            catch (Exception e)
            {
                Exception ex = new Exception($"Failed to apply event {domainEvent.EventType} to Aggregate {this.GetType().Name}", e);

                throw ex;
            }
        }

        public Object GetAggregateMetadata()
        {
            return this.GetMetadata();
        }

        protected abstract Object GetMetadata();

        /// <summary>
        /// Plays the event.
        /// </summary>
        /// <param name="domainEvent">The domain event.</param>
        public abstract void PlayEvent(IDomainEvent domainEvent);

        /// <summary>
        /// Gets the number of historical events.
        /// </summary>
        /// <value>
        /// The number of historical events.
        /// </value>
        protected Int64 NumberOfHistoricalEvents => EventHistory.Count;

        #endregion
    }
}