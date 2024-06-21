namespace Shared.EventStore.Aggregate
{
    using System;
    using DomainDrivenDesign.EventSourcing;
    using EventStore;

    public class AggregateRepositoryManager : IAggregateRepositoryManager
    {
        #region Fields

        /// <summary>
        /// The event store context manager
        /// </summary>
        private readonly IEventStoreContextManager EventStoreContextManager;

        private readonly IDomainEventFactory<IDomainEvent> DomainEventFactory;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="AggregateRepositoryManager" /> class.
        /// </summary>
        /// <param name="eventStoreContextManager">The event store context manager.</param>
        public AggregateRepositoryManager(IEventStoreContextManager eventStoreContextManager, IDomainEventFactory<IDomainEvent>  domainEventFactory)
        {
            this.EventStoreContextManager = eventStoreContextManager;
            DomainEventFactory = domainEventFactory;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets the aggregate repository.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="identifier">The identifier.</param>
        /// <returns></returns>
        public IAggregateRepository<TAggregate, TDomainEvent> GetAggregateRepository<TAggregate, TDomainEvent>(Guid identifier) where TAggregate : Aggregate, new()
        where TDomainEvent : IDomainEvent
        {
            IEventStoreContext context = this.EventStoreContextManager.GetEventStoreContext(identifier.ToString());

            return new AggregateRepository<TAggregate, TDomainEvent>(context, DomainEventFactory);
        }

        #endregion
    }
}