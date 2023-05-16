namespace Shared.EventStore.Aggregate
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using DomainDrivenDesign.EventSourcing;
    using EventStore;
    using global::EventStore.Client;

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TAggregate">The type of the aggregate.</typeparam>
    /// <typeparam name="TDomainEvent">The type of the domain event.</typeparam>
    /// <seealso cref="Shared.EventStore.Aggregate.IAggregateRepository&lt;TAggregate, TDomainEvent&gt;" />
    /// <seealso cref="Shared.EventStore.EventStore.IAggregateRepository{T}" />
    public sealed class AggregateRepository<TAggregate, TDomainEvent> : IAggregateRepository<TAggregate, TDomainEvent> where TAggregate : Aggregate, new()
        where TDomainEvent : IDomainEvent
    {
        #region Fields

        /// <summary>
        /// The convert to event
        /// </summary>
        internal readonly IEventStoreContext EventStoreContext;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="AggregateRepository{TAggregate, TDomainEvent}"/> class.
        /// </summary>
        /// <param name="eventStoreContext">The event store context.</param>
        public AggregateRepository(IEventStoreContext eventStoreContext)
        {
            this.EventStoreContext = eventStoreContext;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets the latest version.
        /// </summary>
        /// <param name="aggregateId">The aggregate identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public async Task<TAggregate> GetLatestVersion(Guid aggregateId,
                                                       CancellationToken cancellationToken)
        {
            TAggregate aggregate = new()
                                   {
                                       AggregateId = aggregateId
                                   };

            String streamName = AggregateRepository<TAggregate, TDomainEvent>.GetStreamName(aggregate.AggregateId);

            var resolvedEvents = await this.EventStoreContext.ReadEvents(streamName, 0, cancellationToken);

            return this.ProcessEvents(aggregate, resolvedEvents);
        }

        /// <summary>
        /// Gets the latest version from last event.
        /// </summary>
        /// <param name="aggregateId">The aggregate identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public async Task<TAggregate> GetLatestVersionFromLastEvent(Guid aggregateId,
                                                                    CancellationToken cancellationToken)
        {
            TAggregate aggregate = new()
                                   {
                                       AggregateId = aggregateId
                                   };

            String streamName = AggregateRepository<TAggregate, TDomainEvent>.GetStreamName(aggregate.AggregateId);

            IList<ResolvedEvent> events = await this.EventStoreContext.GetEventsBackward(streamName, 1, cancellationToken);

            aggregate = this.ProcessEvents(aggregate, events);

            return aggregate;
        }

        /// <summary>
        /// Gets the name of the stream.
        /// </summary>
        /// <param name="aggregateId">The aggregate identifier.</param>
        /// <returns></returns>
        public static String GetStreamName(Guid aggregateId)
        {
            return typeof(TAggregate).Name + "-" + aggregateId.ToString().Replace("-", string.Empty);
        }

        /// <summary>
        /// Saves the changes.
        /// </summary>
        /// <param name="aggregate">The aggregate.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public async Task SaveChanges(TAggregate aggregate,
                                      CancellationToken cancellationToken)
        {
            String streamName = AggregateRepository<TAggregate, TDomainEvent>.GetStreamName(aggregate.AggregateId);
            IList<IDomainEvent> pendingEvents = aggregate.GetPendingEvents();

            if (!pendingEvents.Any())
                return;

            List<EventData> events = new();

            foreach (IDomainEvent domainEvent in pendingEvents)
            {
                EventData @event = TypeMapConvertor.Convertor(domainEvent);

                events.Add(@event);
            }

            await this.EventStoreContext.InsertEvents(streamName, aggregate.Version, events, cancellationToken);
            aggregate.CommitPendingEvents();
        }

        /// <summary>
        /// Processes the events.
        /// </summary>
        /// <param name="aggregate">The aggregate.</param>
        /// <param name="resolvedEvents">The resolved events.</param>
        /// <returns></returns>
        private TAggregate ProcessEvents(TAggregate aggregate,
                                         IList<ResolvedEvent> resolvedEvents)
        {
            if (resolvedEvents != null && resolvedEvents.Count > 0)
            {
                List<IDomainEvent> domainEvents = new();

                foreach (var resolvedEvent in resolvedEvents)
                {
                    IDomainEvent domainEvent = TypeMapConvertor.Convertor(aggregate.AggregateId, resolvedEvent);

                    domainEvents.Add(domainEvent);
                }

                return domainEvents.Aggregate(aggregate,
                                              (aggregate1,
                                               @event) =>
                                              {
                                                  try
                                                  {
                                                      aggregate1.Apply(@event);
                                                      return aggregate1;
                                                  }
                                                  catch(Exception e)
                                                  {
                                                      Exception ex = new Exception($"Failed to apply domain event {@event.EventType} to Aggregate {aggregate.GetType()} ",
                                                                                   e);
                                                      throw ex;
                                                  }
                                              });
            }

            return aggregate;
        }

        #endregion
    }
}