namespace Shared.EventStore.EventStore
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using DomainDrivenDesign.EventSourcing;

    public sealed class AggregateRepository<T> : IAggregateRepository<T> where T : Aggregate, new()
    {
        #region Fields

        /// <summary>
        /// The context
        /// </summary>
        private readonly IEventStoreContext Context;
        
        #endregion

        #region Constructors
        
        /// <summary>
        /// Initializes a new instance of the <see cref="AggregateRepository{T}"/> class.
        /// </summary>
        public AggregateRepository(IEventStoreContext eventStoreContext)
        {
            this.Context = eventStoreContext;
        }
        
        #endregion

        #region Public Methods

        #region public async Task<T> GetLatestVersion(Guid aggregateId,CancellationToken cancellationToken)
        /// <summary>
        /// get latest version as an asynchronous operation.
        /// </summary>
        /// <param name="aggregateId">The aggregate identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// Task&lt;T&gt;.
        /// </returns>
        public async Task<T> GetLatestVersion(Guid aggregateId,CancellationToken cancellationToken)
        {
            //TODO:
            T aggregate = default(T);
            String streamName = this.GetStreamName(aggregateId);

            List<DomainEvent> domainEvents = await this.Context.ReadEvents(streamName, 0, cancellationToken);

            if ((domainEvents != null) && (domainEvents.Any()))
            {
                aggregate = new T();
                aggregate.AggregateId = domainEvents.First().AggregateId;

                domainEvents.ForEach(@event => aggregate.Apply(@event));
            }

            if (aggregate == null)
            {
                aggregate = new T();
                aggregate.AggregateId = aggregateId;
            }

            return aggregate;
        }
        #endregion

        #region public async Task<T> GetByName(Guid aggregateId, String streamName, CancellationToken cancellationToken)
        /// <summary>
        /// Gets the name of the by.
        /// </summary>
        /// <param name="aggregateId">The aggregate identifier.</param>
        /// <param name="streamName">Name of the stream.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public async Task<T> GetByName(Guid aggregateId, String streamName, CancellationToken cancellationToken)
        {
            //TODO:
            T aggregate = default(T);

            List<DomainEvent> domainEvents = await this.Context.ReadEvents(streamName, 0, cancellationToken);

            if ((domainEvents != null) && (domainEvents.Any()))
            {
                aggregate = new T();
                aggregate.AggregateId = domainEvents.First().AggregateId;

                domainEvents.ForEach(@event => aggregate.Apply(@event));
            }

            if (aggregate == null)
            {
                aggregate = new T();
                aggregate.AggregateId = aggregateId;
            }

            return aggregate;
        }
        #endregion

        #region public async Task SaveChanges(T aggregate, CancellationToken cancellationToken)
        /// <summary>
        /// Saves the changes asynchronous.
        /// </summary>
        /// <param name="aggregate">The aggregate.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// Task.
        /// </returns>
        public async Task SaveChanges(T aggregate, CancellationToken cancellationToken)
        {
            List<DomainEvent> pendingEvents = aggregate.GetPendingEvents();

            if(!pendingEvents.Any() )
            {
                //Nothing to persist
                return;
            }

            String streamName = this.GetStreamName(aggregate.AggregateId);

            Object aggregateMetadata = aggregate.GetAggregateMetadata();

            //TODO: duplicate Aggregate Exception handling
            await this.Context.InsertEvents(streamName, aggregate.Version, pendingEvents, aggregateMetadata, cancellationToken);
        }
        #endregion

        #endregion

        #region Private Methods

        #region private String GetStreamName(Guid aggregateId)
        /// <summary>
        /// Gets the name of the stream.
        /// </summary>
        /// <param name="aggregateId">The aggregate identifier.</param>
        /// <returns></returns>
        private String GetStreamName(Guid aggregateId)
        {
            T aggregate = new T();
            aggregate.AggregateId = aggregateId;

            return aggregate.GetStreamName();
        }
        #endregion

        #endregion

        

        
    }
}