using Shared.Exceptions;
using SimpleResults;

namespace Shared.EventStore.EventStore
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure;
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

    [ExcludeFromCodeCoverage(Justification = "This testing is handled with a suite of integration tests")]
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

        private readonly TimeSpan? Deadline;

        #endregion

        #region Constructors

        public EventStoreContext(EventStoreClient eventStoreClient, EventStoreProjectionManagementClient projectionManagementClient, TimeSpan? deadline = null)
        {
            this.EventStoreClient = eventStoreClient;
            this.ProjectionManagementClient = projectionManagementClient;
            this.Deadline = deadline;
        }

        #endregion

        #region Events

        public event TraceHandler TraceGenerated;

        #endregion

        #region Methods

        public async Task<Result<List<ResolvedEvent>>> GetEventsBackward(String streamName,
                                                                          Int32 maxNumberOfEventsToRetrieve,
                                                                          CancellationToken cancellationToken)
        {
            List<ResolvedEvent> resolvedEvents = new();
            try
            {
                EventStoreClient.ReadStreamResult response = this.EventStoreClient.ReadStreamAsync(Direction.Backwards,
                    streamName, StreamPosition.End, maxNumberOfEventsToRetrieve, resolveLinkTos: true,
                    deadline: this.Deadline, cancellationToken: cancellationToken);

                if (await response.ReadState == ReadState.StreamNotFound)
                {
                    return Result.NotFound($"Stream name {streamName} not found");
                }

                List<ResolvedEvent> events = await response.ToListAsync(cancellationToken);

                resolvedEvents.AddRange(events);

                return Result.Success(resolvedEvents);
            }
            catch (Exception e) {
                return Result.Failure(e.GetExceptionMessages());
            }
        }

        public async Task<Result<String>> GetPartitionResultFromProjection(String projectionName,
                                                                           String partitionId,
                                                                           CancellationToken cancellationToken)
        {
            try {
                JsonElement jsonElement = (JsonElement)await this.ProjectionManagementClient.GetResultAsync<dynamic>(
                    projectionName, partitionId, deadline: this.Deadline, cancellationToken: cancellationToken);

                return Result.Success<String>(jsonElement.GetRawText());
            }
            catch (Exception ex) {
                return Result.Failure(ex.GetExceptionMessages());
            }
        }

        public async Task<Result<String>> GetPartitionStateFromProjection(String projectionName,
                                                                          String partitionId,
                                                                          CancellationToken cancellationToken)
        {
            try {
                JsonElement jsonElement = (JsonElement)await this.ProjectionManagementClient.GetStateAsync<dynamic>(
                    projectionName, partitionId, deadline: this.Deadline, cancellationToken: cancellationToken);

                return Result.Success<String>(jsonElement.GetRawText());
            }
            catch (Exception ex) {
                return Result.Failure(ex.GetExceptionMessages());
            }
        }

        public async Task<Result<String>> GetResultFromProjection(String projectionName,
                                                                  CancellationToken cancellationToken)
        {
            try {
                JsonElement jsonElement =
                    (JsonElement)await this.ProjectionManagementClient.GetResultAsync<dynamic>(projectionName,
                        deadline: this.Deadline, cancellationToken: cancellationToken);

                return Result.Success<String>(jsonElement.GetRawText());
            }
            catch (Exception ex) {
                return Result.Failure(ex.GetExceptionMessages());
            }
        }

        public async Task<Result<String>> GetStateFromProjection(String projectionName,
                                                                 CancellationToken cancellationToken) {
            try {
                JsonElement jsonElement =
                    (JsonElement)await this.ProjectionManagementClient.GetStateAsync<dynamic>(projectionName,
                        deadline: this.Deadline, cancellationToken: cancellationToken);

                return Result.Success<String>(jsonElement.GetRawText());
            }
            catch (Exception ex) {
                return Result.Failure(ex.GetExceptionMessages());
            }
        }

        public async Task<Result> InsertEvents(String streamName,
                                               Int64 expectedVersion,
                                               List<EventData> aggregateEvents,
                                               CancellationToken cancellationToken)
        {
            return await this.InsertEvents(streamName, expectedVersion, aggregateEvents, null, cancellationToken);
        }

        public async Task<Result> InsertEvents(String streamName,
                                       Int64 expectedVersion,
                                       List<EventData> aggregateEvents,
                                       Object metadata,
                                       CancellationToken cancellationToken)
        {
            this.LogInformation($"About to append {aggregateEvents.Count} to Stream {streamName}");
            try {
                await this.EventStoreClient.AppendToStreamAsync(streamName, StreamRevision.FromInt64(expectedVersion),
                    aggregateEvents.AsEnumerable(), deadline: this.Deadline, cancellationToken: cancellationToken);
                return Result.Success();
            }
            catch (Exception e) {
                return Result.Failure(e.GetExceptionMessages());
            }
        }

        public async Task<Result<List<ResolvedEvent>>> ReadEvents(String streamName,
                                                          Int64 fromVersion,
                                                          CancellationToken cancellationToken)
        {
            this.LogInformation($"About to read events from Stream {streamName} fromVersion is {fromVersion}");

            List<ResolvedEvent> resolvedEvents = new List<ResolvedEvent>();
            EventStoreClient.ReadStreamResult response;
            List<ResolvedEvent> events;
            try {
                do {
                    response = this.EventStoreClient.ReadStreamAsync(Direction.Forwards, streamName,
                        StreamPosition.FromInt64(fromVersion), 2, resolveLinkTos: true, deadline: this.Deadline,
                        cancellationToken: cancellationToken);

                    // Check the read state
                    ReadState readState = await response.ReadState;

                    if (readState == ReadState.StreamNotFound) {
                        this.LogInformation($"Read State from Stream {streamName} is {readState}");
                        return Result.NotFound($"Stream name {streamName} not found");
                    }

                    events = await response.ToListAsync(cancellationToken);

                    resolvedEvents.AddRange(events);

                    fromVersion += events.Count;
                } while (events.Any());

                this.LogInformation($"About to return {resolvedEvents.Count} events from Stream {streamName}");
                return Result.Success(resolvedEvents);
            }
            catch (Exception e)
            {
                return Result.Failure(e.GetExceptionMessages());
            }
        }

        public async Task<Result<List<ResolvedEvent>>> ReadLastEventsFromAll(Int64 numberEvents,
                                                                             CancellationToken cancellationToken) {
            try {
                IAsyncEnumerable<ResolvedEvent> readResult = this.EventStoreClient.ReadAllAsync(Direction.Backwards, Position.End, maxCount: numberEvents, resolveLinkTos: true, cancellationToken: cancellationToken);

                return Result.Success(await readResult.ToListAsync(cancellationToken));
            }
            catch (Exception ex) {
                return Result.Failure(ex.GetExceptionMessages());
            }
        }

        public async Task<Result<String>> RunTransientQuery(String query, CancellationToken cancellationToken)
        {
            using CancellationTokenSource source = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            String queryName = Guid.NewGuid().ToString();

            try
            {
                await this.ProjectionManagementClient.CreateTransientAsync(queryName, query, cancellationToken: source.Token);

                Stopwatch stopwatch = Stopwatch.StartNew();

                while (true)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        source.Token.ThrowIfCancellationRequested();
                    }

                    ProjectionDetails projectionDetails = await this.ProjectionManagementClient.GetStatusAsync(queryName, deadline: this.Deadline, cancellationToken: source.Token);

                    ProjectionRunningStatus status = EventStoreContext.GetStatusFrom(projectionDetails);

                    if (status == ProjectionRunningStatus.Faulted)
                        return Result.Failure($"Projection {projectionDetails.Name} Status is Faulted");

                    // We need to wait until the query has been run before we continue.
                    if (status == ProjectionRunningStatus.Completed)
                    {
                        JsonDocument jsonDocument = await this.ProjectionManagementClient.GetResultAsync(queryName, deadline: this.Deadline, cancellationToken: source.Token);

                        if (jsonDocument.RootElement.ToString() == "{}")
                        {
                            return Result.Success<String>(String.Empty);
                        }

                        return Result.Success<String>(jsonDocument.RootElement.ToString());
                    }

                    await Task.Delay(100, source.Token);
                }
            }
            catch (RpcException rex)
            {
                this.LogError(rex);
                Exception ex = new Exception(ProjectionRunningStatus.Faulted.ToString(), rex);
                return Result.Failure(ex.GetExceptionMessages());
            }
            finally
            {
                await this.ProjectionManagementClient.DisableAsync(queryName, deadline: this.Deadline, cancellationToken: cancellationToken);
            }
        }

        internal static ProjectionRunningStatus GetStatusFrom(ProjectionDetails projectionDetails)
        {
            return projectionDetails switch
            {
                null => ProjectionRunningStatus.StatisticsNotFound,
                { Status: var status } when String.Compare(status, "Running", StringComparison.CurrentCultureIgnoreCase) == 0 => ProjectionRunningStatus.Running,
                { Status: var status } when String.Compare(status, "Stopped", StringComparison.CurrentCultureIgnoreCase) == 0 => ProjectionRunningStatus.Stopped,
                { Status: var status } when String.Compare(status, "Faulted", StringComparison.CurrentCultureIgnoreCase) == 0
                                            || String.Compare(status, "Faulted (Enabled)", StringComparison.CurrentCultureIgnoreCase) == 0 => ProjectionRunningStatus.Faulted,
                { Status: var status } when String.Compare(status, "Completed/Stopped/Writing results", StringComparison.CurrentCultureIgnoreCase) == 0 => ProjectionRunningStatus.Completed,
                _ => ProjectionRunningStatus.Unknown
            };
        }

        [ExcludeFromCodeCoverage]
        private void LogDebug(String trace)
        {
            if (this.TraceGenerated != null)
            {
                this.TraceGenerated(trace, LogLevel.Debug);
            }
        }

        [ExcludeFromCodeCoverage]
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

        [ExcludeFromCodeCoverage]
        private void LogInformation(String trace)
        {
            if (this.TraceGenerated != null)
            {
                this.TraceGenerated(trace, LogLevel.Information);
            }
        }

        [ExcludeFromCodeCoverage]
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