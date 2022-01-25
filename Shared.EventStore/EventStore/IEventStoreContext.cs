namespace Shared.EventStore.EventStore
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using global::EventStore.Client;

    public interface IEventStoreContext
    {
        #region Events

        /// <summary>
        /// Occurs when trace is generated
        /// </summary>
        event TraceHandler TraceGenerated;

        #endregion

        #region Methods

        /// <summary>
        /// Gets the events backwards asynchronous.
        /// </summary>
        /// <param name="streamName">Name of the stream.</param>
        /// <param name="maxNumberOfEventsToRetrieve">The maximum number of events to retrieve.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        Task<IList<ResolvedEvent>> GetEventsBackwardAsync(String streamName,
                                                          Int32 maxNumberOfEventsToRetrieve,
                                                          CancellationToken cancellationToken);

        /// <summary>
        /// Gets the partition result from projection.
        /// </summary>
        /// <param name="projectionName">Name of the projection.</param>
        /// <param name="partitionId">The partition identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        Task<String> GetPartitionResultFromProjection(String projectionName,
                                                      String partitionId,
                                                      CancellationToken cancellationToken);

        /// <summary>
        /// Gets the partition state from projection.
        /// </summary>
        /// <param name="projectionName">Name of the projection.</param>
        /// <param name="partitionId">The partition identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        Task<String> GetPartitionStateFromProjection(String projectionName,
                                                     String partitionId,
                                                     CancellationToken cancellationToken);

        /// <summary>
        /// Gets the result from projection.
        /// </summary>
        /// <param name="projectionName">Name of the projection.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        Task<String> GetResultFromProjection(String projectionName,
                                             CancellationToken cancellationToken);

        /// <summary>
        /// Gets the state from projection.
        /// </summary>
        /// <param name="projectionName">Name of the projection.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        Task<String> GetStateFromProjection(String projectionName,
                                            CancellationToken cancellationToken);

        /// <summary>
        /// Inserts the events.
        /// </summary>
        /// <param name="streamName">Name of the stream.</param>
        /// <param name="expectedVersion">The expected version.</param>
        /// <param name="aggregateEvents">The aggregate events.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        Task InsertEvents(String streamName,
                          Int64 expectedVersion,
                          List<EventData> aggregateEvents,
                          CancellationToken cancellationToken);

        /// <summary>
        /// Inserts the events.
        /// </summary>
        /// <param name="streamName">Name of the stream.</param>
        /// <param name="expectedVersion">The expected version.</param>
        /// <param name="aggregateEvents">The aggregate events.</param>
        /// <param name="metadata">The metadata.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        Task InsertEvents(String streamName,
                          Int64 expectedVersion,
                          List<EventData> aggregateEvents,
                          Object metadata,
                          CancellationToken cancellationToken);

        /// <summary>
        /// Reads the events.
        /// </summary>
        /// <param name="streamName">Name of the stream.</param>
        /// <param name="fromVersion">From version.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        Task<List<ResolvedEvent>> ReadEvents(String streamName,
                                             Int64 fromVersion,
                                             CancellationToken cancellationToken);

        #endregion
    }
}