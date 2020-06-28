namespace Shared.EventStore.EventStore
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using DomainDrivenDesign.EventSourcing;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <seealso cref="Shared.EventStore.EventStore.IAggregateRepository{T}" />
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
        /// Initializes a new instance of the <see cref="AggregateRepository{T}" /> class.
        /// </summary>
        /// <param name="eventStoreContext">The event store context.</param>
        public AggregateRepository(IEventStoreContext eventStoreContext)
        {
            this.Context = eventStoreContext;
            this.Context.TraceGenerated += this.Context_TraceGenerated;
        }

        #endregion

        #region Events

        /// <summary>
        /// Occurs when trace is generated.
        /// </summary>
        public event TraceHandler TraceGenerated;

        #endregion

        #region Methods

        /// <summary>
        /// Gets the name of the by.
        /// </summary>
        /// <param name="aggregateId">The aggregate identifier.</param>
        /// <param name="streamName">Name of the stream.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public async Task<T> GetByName(Guid aggregateId,
                                       String streamName,
                                       CancellationToken cancellationToken)
        {
            //TODO:
            T aggregate = default;

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

        /// <summary>
        /// get latest version as an asynchronous operation.
        /// </summary>
        /// <param name="aggregateId">The aggregate identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// Task&lt;T&gt;.
        /// </returns>
        public async Task<T> GetLatestVersion(Guid aggregateId,
                                              CancellationToken cancellationToken)
        {
            //TODO:
            T aggregate = default;
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

        /// <summary>
        /// Saves the changes asynchronous.
        /// </summary>
        /// <param name="aggregate">The aggregate.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public async Task SaveChanges(T aggregate,
                                      CancellationToken cancellationToken)
        {
            List<DomainEvent> pendingEvents = aggregate.GetPendingEvents();

            if (!pendingEvents.Any())
            {
                //Nothing to persist
                return;
            }

            String streamName = this.GetStreamName(aggregate.AggregateId);

            Object aggregateMetadata = aggregate.GetAggregateMetadata();

            //TODO: duplicate Aggregate Exception handling
            await this.Context.InsertEvents(streamName, aggregate.Version, pendingEvents, aggregateMetadata, cancellationToken);
        }

        /// <summary>
        /// Contexts the trace generated.
        /// </summary>
        /// <param name="trace">The trace.</param>
        /// <param name="logLevel">The log level.</param>
        private void Context_TraceGenerated(String trace,
                                            LogLevel logLevel)
        {
            switch(logLevel)
            {
                case LogLevel.Information:
                    this.LogInformation(trace);
                    break;
                case LogLevel.Debug:
                    this.LogDebug(trace);
                    break;
                case LogLevel.Warning:
                    this.LogWarning(trace);
                    break;
                case LogLevel.Error:
                    this.LogError(new Exception(trace));
                    break;
                default:
                    this.LogInformation(trace);
                    break;
            }
        }

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

        /// <summary>
        /// Logs the debug.
        /// </summary>
        /// <param name="trace">The trace.</param>
        private void LogDebug(String trace)
        {
            if (this.TraceGenerated != null)
            {
                this.TraceGenerated(trace, LogLevel.Debug);
            }
        }

        /// <summary>
        /// Traces the specified exception.
        /// </summary>
        /// <param name="exception">The exception.</param>
        private void LogError(Exception exception)
        {
            if (this.TraceGenerated != null)
            {
                this.TraceGenerated(exception.Message, LogLevel.Error);
                if (exception.InnerException != null)
                {
                    this.LogError(exception.InnerException);
                }
            }
        }

        /// <summary>
        /// Traces the specified trace.
        /// </summary>
        /// <param name="trace">The trace.</param>
        private void LogInformation(String trace)
        {
            if (this.TraceGenerated != null)
            {
                this.TraceGenerated(trace, LogLevel.Information);
            }
        }

        /// <summary>
        /// Logs the warning.
        /// </summary>
        /// <param name="trace">The trace.</param>
        private void LogWarning(String trace)
        {
            if (this.TraceGenerated != null)
            {
                this.TraceGenerated(trace, LogLevel.Warning);
            }
        }

        #endregion
    }
}