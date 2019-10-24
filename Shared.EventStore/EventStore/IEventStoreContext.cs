namespace Shared.DomainDrivenDesign.EventStore
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using EventSourcing;

    public interface IEventStoreContext
    {
        #region Events

        /// <summary>
        /// Occurs when [connection destroyed].
        /// </summary>
        event EventHandler ConnectionDestroyed;

        /// <summary>
        /// Occurs when [event appeared].
        /// </summary>
        event EventAppearedEventHandler EventAppeared;

        /// <summary>
        /// Occurs when [live process started].
        /// </summary>
        event LiveProcessStartedEventHandler LiveProcessStarted;

        /// <summary>
        /// Occurs when [subscription dropped].
        /// </summary>
        event SubscriptionDroppedEventHandler SubscriptionDropped;

        /// <summary>
        /// Occurs when trace is generated
        /// </summary>
        event TraceHandler TraceGenerated;

        #endregion

        #region Methods

        /// <summary>
        /// Connects to subscription.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="groupName">Name of the group.</param>
        /// <param name="subscriptionGroupId">The subscription group identifier.</param>
        /// <param name="bufferSize">Size of the buffer.</param>
        /// <returns></returns>
        Task ConnectToSubscription(String stream,
                                   String groupName,
                                   Guid subscriptionGroupId,
                                   Int32 bufferSize);

        /// <summary>
        /// Creates the new persistent subscription.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="groupName">Name of the group.</param>
        /// <returns></returns>
        Task CreateNewPersistentSubscription(String stream,
                                             String groupName);

        /// <summary>
        /// Creates the persistent subscription from beginning.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="groupName">Name of the group.</param>
        /// <returns></returns>
        Task CreatePersistentSubscriptionFromBeginning(String stream,
                                                       String groupName);

        /// <summary>
        /// Creates the persistent subscription from current.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="groupName">Name of the group.</param>
        /// <returns></returns>
        Task CreatePersistentSubscriptionFromCurrent(String stream,
                                                     String groupName);

        /// <summary>
        /// Creates the persistent subscription from position.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="groupName">Name of the group.</param>
        /// <param name="position">The position.</param>
        /// <returns></returns>
        Task CreatePersistentSubscriptionFromPosition(String stream,
                                                      String groupName,
                                                      Int32 position);

        /// <summary>
        /// Deletes the persistent subscription.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="groupName">Name of the group.</param>
        /// <returns></returns>
        Task DeletePersistentSubscription(String stream,
                                          String groupName);

        /// <summary>
        /// Gets the partition result from projection.
        /// </summary>
        /// <param name="projectionName">Name of the projection.</param>
        /// <param name="partitionId">The partition identifier.</param>
        /// <returns></returns>
        Task<String> GetPartitionResultFromProjection(String projectionName,
                                                      String partitionId);

        /// <summary>
        /// Gets the partition state from projection.
        /// </summary>
        /// <param name="projectionName">Name of the projection.</param>
        /// <param name="partitionId">The partition identifier.</param>
        /// <returns></returns>
        Task<String> GetPartitionStateFromProjection(String projectionName,
                                                     String partitionId);

        /// <summary>
        /// Gets the result from projection.
        /// </summary>
        /// <param name="projectionName">Name of the projection.</param>
        /// <returns></returns>
        Task<String> GetResultFromProjection(String projectionName);

        /// <summary>
        /// Gets the state from projection.
        /// </summary>
        /// <param name="projectionName">Name of the projection.</param>
        /// <returns></returns>
        Task<String> GetStateFromProjection(String projectionName);

        /// <summary>
        /// Inserts the events.
        /// </summary>
        /// <param name="streamName">Name of the stream.</param>
        /// <param name="expectedVersion">The expected version.</param>
        /// <param name="aggregateEvents">The aggregate events.</param>
        /// <returns></returns>
        Task InsertEvents(String streamName,
                          Int32 expectedVersion,
                          List<DomainEvent> aggregateEvents);

        /// <summary>
        /// Reads the events.
        /// </summary>
        /// <param name="streamName">Name of the stream.</param>
        /// <param name="fromVersion">From version.</param>
        /// <returns></returns>
        Task<List<DomainEvent>> ReadEvents(String streamName,
                                           Int64 fromVersion);

        /// <summary>
        /// Subscribes to stream from.
        /// </summary>
        /// <param name="catchUpSubscriptionId">The catch up subscription identifier.</param>
        /// <param name="stream">The stream.</param>
        /// <param name="lastCheckpoint">The last checkpoint.</param>
        /// <param name="endPointId">The end point identifier.</param>
        /// <returns></returns>
        Task SubscribeToStreamFrom(Guid catchUpSubscriptionId,
                                   String stream,
                                   Int32? lastCheckpoint,
                                   Guid endPointId);

        #endregion
    }
}