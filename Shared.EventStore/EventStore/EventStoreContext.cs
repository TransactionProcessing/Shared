using KurrentDB.Client;
using Shared.Exceptions;
using SimpleResults;

namespace Shared.EventStore.EventStore;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Shared.EventStore;

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
    internal readonly KurrentDBClient? KurrentDBClient;

    /// <summary>
    /// The event store client settings
    /// </summary>
    private readonly KurrentDBClientSettings? ClientSettings;

    /// <summary>
    /// The projection management client
    /// </summary>
    private readonly KurrentDBProjectionManagementClient? ProjectionManagementClient;

    private readonly TimeSpan? Deadline;

    #endregion

    #region Constructors

    public EventStoreContext(KurrentDBClient eventStoreClient, KurrentDBProjectionManagementClient projectionManagementClient, TimeSpan? deadline = null)
    {
        this.KurrentDBClient = eventStoreClient;
        this.ProjectionManagementClient = projectionManagementClient;
        this.Deadline = deadline;
    }

    public EventStoreContext(KurrentDBClientSettings clientSettings, TimeSpan? deadline = null)
    {
        this.ClientSettings = clientSettings;
        this.Deadline = deadline;
    }

    #endregion

    #region Events

    public event TraceHandler TraceGenerated;

    #endregion

    #region Methods

    private async Task<T> UseClientAsync<T>(Func<KurrentDBClient, Task<T>> action)
    {
        if (this.ClientSettings != null)
        {
            using KurrentDBClient client = new(this.ClientSettings);
            return await action(client);
        }

        return await action(this.KurrentDBClient!);
    }

    private async Task UseClientAsync(Func<KurrentDBClient, Task> action)
    {
        if (this.ClientSettings != null)
        {
            using KurrentDBClient client = new(this.ClientSettings);
            await action(client);
            return;
        }

        await action(this.KurrentDBClient!);
    }

    private async Task<T> UseProjectionManagementClientAsync<T>(Func<KurrentDBProjectionManagementClient, Task<T>> action)
    {
        if (this.ClientSettings != null)
        {
            using KurrentDBProjectionManagementClient client = new(this.ClientSettings);
            return await action(client);
        }

        return await action(this.ProjectionManagementClient!);
    }

    private async Task UseProjectionManagementClientAsync(Func<KurrentDBProjectionManagementClient, Task> action)
    {
        if (this.ClientSettings != null)
        {
            using KurrentDBProjectionManagementClient client = new(this.ClientSettings);
            await action(client);
            return;
        }

        await action(this.ProjectionManagementClient!);
    }

    public async Task<Result<List<ResolvedEvent>>> GetEventsBackward(String streamName,
                                                                     Int32 maxNumberOfEventsToRetrieve,
                                                                     CancellationToken cancellationToken)
    {
        try
        {
            return await EventStoreGrpcRetryPolicy.ExecuteAsync(async () =>
            {
                return await this.UseClientAsync(async client =>
                {
                    KurrentDBClient.ReadStreamResult response = client.ReadStreamAsync(Direction.Backwards,
                        streamName, StreamPosition.End, maxNumberOfEventsToRetrieve, resolveLinkTos: true,
                        deadline: this.Deadline, cancellationToken: cancellationToken);

                    if (await response.ReadState == ReadState.StreamNotFound)
                    {
                        return Result.NotFound($"Stream name {streamName} not found");
                    }

                    List<ResolvedEvent> resolvedEvents = await response.ToListAsync(cancellationToken);
                    return Result.Success(resolvedEvents);
                });
            }, nameof(this.GetEventsBackward), $"stream {streamName}", this.LogRetry);
        }
        catch (Exception e)
        {
            return Result.Failure(e.GetExceptionMessages());
        }
    }

    public async Task<Result<String>> GetPartitionResultFromProjection(String projectionName,
                                                                       String partitionId,
                                                                       CancellationToken cancellationToken)
    {
        try {
            JsonElement jsonElement = await EventStoreGrpcRetryPolicy.ExecuteAsync(async () =>
            {
                return await this.UseProjectionManagementClientAsync(async client =>
                {
                    return (JsonElement)await client.GetResultAsync<dynamic>(
                        projectionName, partitionId, deadline: this.Deadline, cancellationToken: cancellationToken);
                });
            }, nameof(this.GetPartitionResultFromProjection), $"projection {projectionName} partition {partitionId}", this.LogRetry);

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
            JsonElement jsonElement = await EventStoreGrpcRetryPolicy.ExecuteAsync(async () =>
            {
                return await this.UseProjectionManagementClientAsync(async client =>
                {
                    return (JsonElement)await client.GetStateAsync<dynamic>(
                        projectionName, partitionId, deadline: this.Deadline, cancellationToken: cancellationToken);
                });
            }, nameof(this.GetPartitionStateFromProjection), $"projection {projectionName} partition {partitionId}", this.LogRetry);

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
                await EventStoreGrpcRetryPolicy.ExecuteAsync(async () =>
                    await this.UseProjectionManagementClientAsync(async client =>
                        (JsonElement)await client.GetResultAsync<dynamic>(projectionName,
                            deadline: this.Deadline, cancellationToken: cancellationToken)),
                    nameof(this.GetResultFromProjection), $"projection {projectionName}", this.LogRetry);

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
                await EventStoreGrpcRetryPolicy.ExecuteAsync(async () =>
                    await this.UseProjectionManagementClientAsync(async client =>
                        (JsonElement)await client.GetStateAsync<dynamic>(projectionName,
                            deadline: this.Deadline, cancellationToken: cancellationToken)),
                    nameof(this.GetStateFromProjection), $"projection {projectionName}", this.LogRetry);

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
            StreamState expectedState = expectedVersion < 0
                ? StreamState.NoStream
                : StreamState.StreamRevision((ulong)expectedVersion);

            await EventStoreGrpcRetryPolicy.ExecuteAsync(() =>
                    this.UseClientAsync(client =>
                        client.AppendToStreamAsync(streamName, expectedState,
                            aggregateEvents.AsEnumerable(), deadline: this.Deadline, cancellationToken: cancellationToken)),
                nameof(this.InsertEvents), $"stream {streamName}", this.LogRetry);
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
        try {
            while (true) {
                (ReadState ReadState, List<ResolvedEvent> Events) page = await EventStoreGrpcRetryPolicy.ExecuteAsync(async () =>
                {
                    return await this.UseClientAsync(async client =>
                    {
                        KurrentDBClient.ReadStreamResult response = client.ReadStreamAsync(Direction.Forwards, streamName,
                            StreamPosition.FromInt64(fromVersion), Int32.MaxValue, resolveLinkTos: true, deadline: this.Deadline,
                            cancellationToken: cancellationToken);

                        ReadState readState = await response.ReadState;
                        if (readState == ReadState.StreamNotFound) {
                            return (readState, new List<ResolvedEvent>());
                        }

                        List<ResolvedEvent> events = await response.ToListAsync(cancellationToken);
                        return (readState, events);
                    });
                }, nameof(this.ReadEvents), $"stream {streamName} fromVersion {fromVersion}", this.LogRetry);

                if (page.ReadState == ReadState.StreamNotFound) {
                    this.LogInformation($"Read State from Stream {streamName} is {page.ReadState}");
                    return Result.NotFound($"Stream name {streamName} not found");
                }

                resolvedEvents.AddRange(page.Events);
                fromVersion += page.Events.Count;

                if (!page.Events.Any()) {
                    break;
                }
            }

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
            return Result.Success(await EventStoreGrpcRetryPolicy.ExecuteAsync(async () =>
            {
                return await this.UseClientAsync(async client =>
                {
                    IAsyncEnumerable<ResolvedEvent> readResult = client.ReadAllAsync(Direction.Backwards, Position.End, maxCount: numberEvents, resolveLinkTos: true, cancellationToken: cancellationToken);
                    return await readResult.ToListAsync(cancellationToken);
                });
            }, nameof(this.ReadLastEventsFromAll), $"last {numberEvents} events", this.LogRetry));
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
            await EventStoreGrpcRetryPolicy.ExecuteAsync(() =>
                    this.UseProjectionManagementClientAsync(client =>
                        client.CreateTransientAsync(queryName, query, cancellationToken: source.Token)),
                nameof(this.RunTransientQuery), $"transient query {queryName}", this.LogRetry);

            while (true)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    source.Token.ThrowIfCancellationRequested();
                }

                ProjectionDetails projectionDetails = await EventStoreGrpcRetryPolicy.ExecuteAsync(async () =>
                    await this.UseProjectionManagementClientAsync(client =>
                        client.GetStatusAsync(queryName, deadline: this.Deadline, cancellationToken: source.Token)),
                    nameof(this.RunTransientQuery), $"transient query {queryName}", this.LogRetry);

                ProjectionRunningStatus status = EventStoreContext.GetStatusFrom(projectionDetails);

                if (status == ProjectionRunningStatus.Faulted)
                    return Result.Failure($"Projection {projectionDetails.Name} Status is Faulted");

                // We need to wait until the query has been run before we continue.
                if (status == ProjectionRunningStatus.Completed)
                {
                    JsonDocument jsonDocument = await EventStoreGrpcRetryPolicy.ExecuteAsync(async () =>
                        await this.UseProjectionManagementClientAsync(client =>
                            client.GetResultAsync(queryName, deadline: this.Deadline, cancellationToken: source.Token)),
                        nameof(this.RunTransientQuery), $"transient query {queryName}", this.LogRetry);

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
            Exception ex = new(ProjectionRunningStatus.Faulted.ToString(), rex);
            return Result.Failure(ex.GetExceptionMessages());
        }
        finally
        {
            await EventStoreGrpcRetryPolicy.ExecuteAsync(() =>
                    this.UseProjectionManagementClientAsync(client =>
                        client.DisableAsync(queryName, deadline: this.Deadline, cancellationToken: cancellationToken)),
                nameof(this.RunTransientQuery), $"transient query {queryName}", this.LogRetry);
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

    private void LogRetry(LogLevel logLevel,
                          String message)
    {
        if (this.TraceGenerated != null)
        {
            this.TraceGenerated(message, logLevel);
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
