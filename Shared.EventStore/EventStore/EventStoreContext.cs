namespace Shared.EventStore.EventStore{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using global::EventStore.Client;
    using Grpc.Core;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;

    /// <summary>
    /// Delegate TraceHandler
    /// </summary>
    /// <param name="trace">The trace.</param>
    public delegate void TraceHandler(String trace,
                                      LogLevel logLevel);

    public class EventStoreContext : IEventStoreContext{
        #region Fields

        /// <summary>
        /// The event store client
        /// </summary>
        private readonly EventStoreClient EventStoreClient;

        /// <summary>
        /// The projection management client
        /// </summary>
        private readonly EventStoreProjectionManagementClient ProjectionManagementClient;

        private readonly TimeSpan? Deadline;

        #endregion

        #region Constructors

        public EventStoreContext(EventStoreClient eventStoreClient, EventStoreProjectionManagementClient projectionManagementClient, TimeSpan? deadline = null){
            this.EventStoreClient = eventStoreClient;
            this.ProjectionManagementClient = projectionManagementClient;
            this.Deadline = deadline;
        }

        #endregion

        #region Events

        public event TraceHandler TraceGenerated;

        #endregion

        #region Methods

        public async Task<IList<ResolvedEvent>> GetEventsBackward(String streamName,
                                                                       Int32 maxNumberOfEventsToRetrieve,
                                                                       CancellationToken cancellationToken){
            List<ResolvedEvent> resolvedEvents = new();

            EventStoreClient.ReadStreamResult response = this.EventStoreClient.ReadStreamAsync(Direction.Backwards,
                                                                                               streamName,
                                                                                               StreamPosition.End,
                                                                                               maxNumberOfEventsToRetrieve,
                                                                                               resolveLinkTos:true,
                                                                                               deadline:this.Deadline,
                                                                                               cancellationToken:cancellationToken);

            if (await response.ReadState == ReadState.StreamNotFound){
                return resolvedEvents;
            }

            List<ResolvedEvent> events = await response.ToListAsync(cancellationToken);

            resolvedEvents.AddRange(events);

            return resolvedEvents;
        }

        public async Task<String> GetPartitionResultFromProjection(String projectionName,
                                                                   String partitionId,
                                                                   CancellationToken cancellationToken){
            JsonElement jsonElement = (JsonElement)await this.ProjectionManagementClient.GetResultAsync<dynamic>(projectionName, partitionId, deadline: this.Deadline, cancellationToken:cancellationToken);

            return jsonElement.GetRawText();
        }

        public async Task<String> GetPartitionStateFromProjection(String projectionName,
                                                                  String partitionId,
                                                                  CancellationToken cancellationToken){
            JsonElement jsonElement = (JsonElement)await this.ProjectionManagementClient.GetStateAsync<dynamic>(projectionName, partitionId, deadline: this.Deadline, cancellationToken:cancellationToken);

            return jsonElement.GetRawText();
        }

        public async Task<String> GetResultFromProjection(String projectionName,
                                                          CancellationToken cancellationToken){
            JsonElement jsonElement = (JsonElement)await this.ProjectionManagementClient.GetResultAsync<dynamic>(projectionName, deadline: this.Deadline, cancellationToken:cancellationToken);

            return jsonElement.GetRawText();
        }

        public async Task<String> GetStateFromProjection(String projectionName,
                                                         CancellationToken cancellationToken){
            JsonElement jsonElement = (JsonElement)await this.ProjectionManagementClient.GetStateAsync<dynamic>(projectionName, deadline: this.Deadline, cancellationToken:cancellationToken);

            return jsonElement.GetRawText();
        }

        public async Task InsertEvents(String streamName,
                                       Int64 expectedVersion,
                                       List<EventData> aggregateEvents,
                                       CancellationToken cancellationToken){
            await this.InsertEvents(streamName, expectedVersion, aggregateEvents, null, cancellationToken);
        }

        public async Task InsertEvents(String streamName,
                                       Int64 expectedVersion,
                                       List<EventData> aggregateEvents,
                                       Object metadata,
                                       CancellationToken cancellationToken){
            List<EventData> eventData = new List<EventData>();
            JsonSerializerSettings s = new JsonSerializerSettings{
                                                                     TypeNameHandling = TypeNameHandling.All
                                                                 };

            this.LogInformation($"About to append {aggregateEvents.Count} to Stream {streamName}");
            await this.EventStoreClient.AppendToStreamAsync(streamName, StreamRevision.FromInt64(expectedVersion), aggregateEvents.AsEnumerable(), deadline:this.Deadline, cancellationToken:cancellationToken);
        }

        public async Task<List<ResolvedEvent>> ReadEvents(String streamName,
                                                          Int64 fromVersion,
                                                          CancellationToken cancellationToken){
            this.LogInformation($"About to read events from Stream {streamName} fromVersion is {fromVersion}");

            List<ResolvedEvent> resolvedEvents = new List<ResolvedEvent>();
            EventStoreClient.ReadStreamResult response;
            List<ResolvedEvent> events;
            do{
                response = this.EventStoreClient.ReadStreamAsync(Direction.Forwards,
                                                                 streamName,
                                                                 StreamPosition.FromInt64(fromVersion),
                                                                 2,
                                                                 resolveLinkTos:true,
                                                                 deadline: this.Deadline,
                                                                 cancellationToken:cancellationToken);

                // Check the read state
                ReadState readState = await response.ReadState;

                if (readState == ReadState.StreamNotFound){
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

        public async Task<String> RunTransientQuery(String query, CancellationToken cancellationToken){
            CancellationTokenSource source = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            String queryName = Guid.NewGuid().ToString();

            await this.ProjectionManagementClient.CreateTransientAsync(queryName, query, cancellationToken:source.Token);

            Stopwatch stopwatch = Stopwatch.StartNew();

            try{
                while (true){
                    if (cancellationToken.IsCancellationRequested){
                        source.Token.ThrowIfCancellationRequested();
                    }

                    ProjectionDetails projectionDetails = await this.ProjectionManagementClient.GetStatusAsync(queryName, deadline: this.Deadline, cancellationToken:source.Token);

                    ProjectionRunningStatus status = EventStoreContext.GetStatusFrom(projectionDetails);

                    // We need to wait until the query has been run before we continue.
                    if (status == ProjectionRunningStatus.Completed){
                        JsonDocument jsonDocument = await this.ProjectionManagementClient.GetResultAsync(queryName, deadline: this.Deadline, cancellationToken:source.Token);

                        if (jsonDocument.RootElement.ToString() == "{}"){
                            return String.Empty;
                        }

                        return jsonDocument.RootElement.ToString();
                    }

                    if (stopwatch.ElapsedMilliseconds > TimeSpan.FromSeconds(5).TotalMilliseconds){
                        source.Cancel(); // This will make sure the Projection is deleted.
                        continue;
                    }

                    await Task.Delay(100, source.Token);
                }
            }
            catch(RpcException rex){
                this.LogError(rex);
                throw new Exception(ProjectionRunningStatus.Faulted.ToString(), rex);
            }
            finally{
                await this.ProjectionManagementClient.DisableAsync(queryName, deadline: this.Deadline, cancellationToken:cancellationToken);
            }

            return null;
        }

        internal static ProjectionRunningStatus GetStatusFrom(ProjectionDetails projectionDetails){
            if (projectionDetails == null){
                return ProjectionRunningStatus.StatisticsNotFound;
            }

            if (String.Compare(projectionDetails.Status, "Running", StringComparison.CurrentCultureIgnoreCase) == 0){
                return ProjectionRunningStatus.Running;
            }

            if (String.Compare(projectionDetails.Status, "Stopped", StringComparison.CurrentCultureIgnoreCase) == 0){
                return ProjectionRunningStatus.Stopped;
            }

            if (String.Compare(projectionDetails.Status, "Faulted", StringComparison.CurrentCultureIgnoreCase) == 0){
                return ProjectionRunningStatus.Faulted;
            }

            if (String.Compare(projectionDetails.Status, "Completed/Stopped/Writing results", StringComparison.CurrentCultureIgnoreCase) == 0){
                return ProjectionRunningStatus.Completed;
            }

            return ProjectionRunningStatus.Unknown;
        }

        [ExcludeFromCodeCoverage]
        private void LogDebug(String trace){
            if (this.TraceGenerated != null){
                this.TraceGenerated(trace, LogLevel.Debug);
            }
        }

        [ExcludeFromCodeCoverage]
        private void LogError(Exception exception){
            if (this.TraceGenerated != null){
                this.TraceGenerated(exception.Message, LogLevel.Error);
                if (exception.InnerException != null){
                    this.LogError(exception.InnerException);
                }
            }
        }

        [ExcludeFromCodeCoverage]
        private void LogInformation(String trace){
            if (this.TraceGenerated != null){
                this.TraceGenerated(trace, LogLevel.Information);
            }
        }

        [ExcludeFromCodeCoverage]
        private void LogWarning(String trace){
            if (this.TraceGenerated != null){
                this.TraceGenerated(trace, LogLevel.Warning);
            }
        }

        #endregion
    }
}