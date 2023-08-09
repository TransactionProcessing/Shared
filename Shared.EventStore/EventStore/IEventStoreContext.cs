namespace Shared.EventStore.EventStore{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using global::EventStore.Client;

    public interface IEventStoreContext{
        #region Events

        /// <summary>
        /// Occurs when trace is generated
        /// </summary>
        event TraceHandler TraceGenerated;

        #endregion

        #region Methods

        Task<IList<ResolvedEvent>> GetEventsBackward(String streamName,
                                                          Int32 maxNumberOfEventsToRetrieve,
                                                          CancellationToken cancellationToken);

        Task<String> GetPartitionResultFromProjection(String projectionName,
                                                      String partitionId,
                                                      CancellationToken cancellationToken);

        Task<String> GetPartitionStateFromProjection(String projectionName,
                                                     String partitionId,
                                                     CancellationToken cancellationToken);

        Task<String> GetResultFromProjection(String projectionName,
                                             CancellationToken cancellationToken);

        Task<String> GetStateFromProjection(String projectionName,
                                            CancellationToken cancellationToken);

        Task InsertEvents(String streamName,
                          Int64 expectedVersion,
                          List<EventData> aggregateEvents,
                          CancellationToken cancellationToken);

        Task InsertEvents(String streamName,
                          Int64 expectedVersion,
                          List<EventData> aggregateEvents,
                          Object metadata,
                          CancellationToken cancellationToken);

        Task<List<ResolvedEvent>> ReadEvents(String streamName,
                                             Int64 fromVersion,
                                             CancellationToken cancellationToken);

        Task<String> RunTransientQuery(String query, CancellationToken cancellationToken);
        
        #endregion
    }
}