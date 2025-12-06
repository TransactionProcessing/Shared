using KurrentDB.Client;
using SimpleResults;

namespace Shared.EventStore.EventStore;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public interface IEventStoreContext{
    #region Events

    /// <summary>
    /// Occurs when trace is generated
    /// </summary>
    event TraceHandler TraceGenerated;

    #endregion

    #region Methods

    Task<Result<List<ResolvedEvent>>> GetEventsBackward(String streamName,
                                                        Int32 maxNumberOfEventsToRetrieve,
                                                        CancellationToken cancellationToken);

    Task<Result<String>> GetPartitionResultFromProjection(String projectionName,
                                                          String partitionId,
                                                          CancellationToken cancellationToken);

    Task<Result<String>> GetPartitionStateFromProjection(String projectionName,
                                                         String partitionId,
                                                         CancellationToken cancellationToken);

    Task<Result<String>> GetResultFromProjection(String projectionName,
                                                 CancellationToken cancellationToken);

    Task<Result<String>> GetStateFromProjection(String projectionName,
                                                CancellationToken cancellationToken);

    Task<Result> InsertEvents(String streamName,
                              Int64 expectedVersion,
                              List<EventData> aggregateEvents,
                              CancellationToken cancellationToken);

    Task<Result> InsertEvents(String streamName,
                              Int64 expectedVersion,
                              List<EventData> aggregateEvents,
                              Object metadata,
                              CancellationToken cancellationToken);

    Task<Result<List<ResolvedEvent>>> ReadEvents(String streamName,
                                                 Int64 fromVersion,
                                                 CancellationToken cancellationToken);

    Task<Result<List<ResolvedEvent>>> ReadLastEventsFromAll(Int64 numberEvents,
                                                            CancellationToken cancellationToken);

    Task<Result<String>> RunTransientQuery(String query, CancellationToken cancellationToken);
        
    #endregion
}