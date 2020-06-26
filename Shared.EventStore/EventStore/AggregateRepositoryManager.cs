namespace Shared.EventStore.EventStore
{
    using System;

    public class AggregateRepositoryManager : IAggregateRepositoryManager
    {
        #region Fields

        /// <summary>
        /// The event store context manager
        /// </summary>
        private readonly IEventStoreContextManager EventStoreContextManager;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="AggregateRepositoryManager" /> class.
        /// </summary>
        /// <param name="eventStoreContextManager">The event store context manager.</param>
        public AggregateRepositoryManager(IEventStoreContextManager eventStoreContextManager)
        {
            this.EventStoreContextManager = eventStoreContextManager;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets the aggregate repository.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="identifier">The identifier.</param>
        /// <returns></returns>
        public IAggregateRepository<T> GetAggregateRepository<T>(Guid identifier) where T : Aggregate, new()
        {
            IEventStoreContext context = this.EventStoreContextManager.GetEventStoreContext(identifier.ToString());

            return new AggregateRepository<T>(context);
        }

        #endregion
    }
}