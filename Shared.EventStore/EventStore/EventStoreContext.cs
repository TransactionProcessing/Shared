namespace Shared.EventStore.EventStore
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using DomainDrivenDesign.EventSourcing;
    using global::EventStore.Client;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;

    /// <summary>
    /// Delegate TraceHandler
    /// </summary>
    /// <param name="trace">The trace.</param>
    public delegate void TraceHandler(String trace,
                                      LogLevel logLevel);

    public class EventStoreContext : IEventStoreContext
    {
        #region Fields

        /// <summary>
        /// The event store client
        /// </summary>
        private readonly EventStoreClient EventStoreClient;

        /// <summary>
        /// The projection management client
        /// </summary>
        private readonly EventStoreProjectionManagementClient ProjectionManagementClient;

        #endregion

        #region Constructors

        /// <summary>
        /// The user credentials
        /// </summary>
        /// <param name="eventStoreClient">The event store client.</param>
        /// <param name="projectionManagementClient"></param>
        public EventStoreContext(EventStoreClient eventStoreClient, EventStoreProjectionManagementClient projectionManagementClient)
        {
            this.EventStoreClient = eventStoreClient;
            this.ProjectionManagementClient = projectionManagementClient;
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
        /// Gets the partition result from projection.
        /// </summary>
        /// <param name="projectionName">Name of the projection.</param>
        /// <param name="partitionId">The partition identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<String> GetPartitionResultFromProjection(String projectionName,
                                                                   String partitionId,
                                                                   CancellationToken cancellationToken)
        {
            JsonElement jsonElement = (JsonElement)await this.ProjectionManagementClient.GetResultAsync<dynamic>(projectionName, partitionId, cancellationToken: cancellationToken);

            return jsonElement.GetRawText();

        }

        /// <summary>
        /// Gets the partition state from projection.
        /// </summary>
        /// <param name="projectionName">Name of the projection.</param>
        /// <param name="partitionId">The partition identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public async Task<String> GetPartitionStateFromProjection(String projectionName,
                                                                  String partitionId,
                                                                  CancellationToken cancellationToken)
        {
            JsonElement jsonElement = (JsonElement)await this.ProjectionManagementClient.GetStateAsync<dynamic>(projectionName, partitionId, cancellationToken: cancellationToken);

            return jsonElement.GetRawText();
        }

        /// <summary>
        /// Gets the result from projection.
        /// </summary>
        /// <param name="projectionName">Name of the projection.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public async Task<String> GetResultFromProjection(String projectionName,
                                                          CancellationToken cancellationToken)
        {
            JsonElement jsonElement = (JsonElement)await this.ProjectionManagementClient.GetResultAsync<dynamic>(projectionName, cancellationToken: cancellationToken);

            return jsonElement.GetRawText();
        }

        /// <summary>
        /// Gets the state from projection.
        /// </summary>
        /// <param name="projectionName">Name of the projection.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public async Task<String> GetStateFromProjection(String projectionName,
                                                         CancellationToken cancellationToken)
        {
            JsonElement jsonElement = (JsonElement)await this.ProjectionManagementClient.GetStateAsync<dynamic>(projectionName, cancellationToken: cancellationToken);

            return jsonElement.GetRawText();
        }

        /// <summary>
        /// Inserts the events.
        /// </summary>
        /// <param name="streamName">Name of the stream.</param>
        /// <param name="expectedVersion">The expected version.</param>
        /// <param name="aggregateEvents">The aggregate events.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public async Task InsertEvents(String streamName,
                                       Int64 expectedVersion,
                                       List<EventData> aggregateEvents,
                                       CancellationToken cancellationToken)
        {
            await this.InsertEvents(streamName, expectedVersion, aggregateEvents, null, cancellationToken);
        }

        /// <summary>
        /// Inserts the events.
        /// </summary>
        /// <param name="streamName">Name of the stream.</param>
        /// <param name="expectedVersion">The expected version.</param>
        /// <param name="aggregateEvents">The aggregate events.</param>
        /// <param name="metadata">The metadata.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public async Task InsertEvents(String streamName,
                                       Int64 expectedVersion,
                                       List<EventData> aggregateEvents,
                                       Object metadata,
                                       CancellationToken cancellationToken)
        {
            List<EventData> eventData = new List<EventData>();
            JsonSerializerSettings s = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All
            };

            this.LogInformation($"About to append {aggregateEvents.Count} to Stream {streamName}");
            await this.EventStoreClient.AppendToStreamAsync(streamName, StreamRevision.FromInt64(expectedVersion), aggregateEvents.AsEnumerable(), cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Reads the events.
        /// </summary>
        /// <param name="streamName">Name of the stream.</param>
        /// <param name="fromVersion">From version.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public async Task<List<ResolvedEvent>> ReadEvents(String streamName,
                                                                            Int64 fromVersion,
                                                                            CancellationToken cancellationToken)
        {
            this.LogInformation($"About to read events from Stream {streamName} fromVersion is {fromVersion}");

            List<ResolvedEvent> resolvedEvents = new List<ResolvedEvent>();
            EventStoreClient.ReadStreamResult response;
            List<ResolvedEvent> events;
            do
            {
                response = this.EventStoreClient.ReadStreamAsync(Direction.Forwards,
                                                                 streamName,
                                                                 StreamPosition.FromInt64(fromVersion),
                                                                 2,
                                                                 resolveLinkTos: true,
                                                                 cancellationToken: cancellationToken);

                // Check the read state
                ReadState readState = await response.ReadState;

                if (readState == ReadState.StreamNotFound)
                {
                    this.LogInformation($"Read State from Stream {streamName} is {readState}");
                    return null;
                }

                events = await response.ToListAsync(cancellationToken);

                resolvedEvents.AddRange(events);

                fromVersion += events.Count;

            } while (events.Any());

            this.LogInformation($"About to return {resolvedEvents.Count} events from Stream {streamName}");
            return resolvedEvents;
        }

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