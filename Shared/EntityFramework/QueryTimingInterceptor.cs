#nullable enable
using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Shared.EntityFramework;

/// <summary>Logs EF Core database commands whose execution time exceeds a configured threshold.</summary>
public sealed class QueryTimingInterceptor : DbCommandInterceptor
{
    private readonly ILogger<QueryTimingInterceptor> _logger;
    private readonly QueryTimingInterceptorOptions _options;

    public QueryTimingInterceptor(ILogger<QueryTimingInterceptor> logger)
        : this(logger, new QueryTimingInterceptorOptions())
    {
    }

    public QueryTimingInterceptor(ILogger<QueryTimingInterceptor> logger, QueryTimingInterceptorOptions options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        if (_options.ThresholdMilliseconds < 0)
            throw new ArgumentOutOfRangeException(nameof(options), "The timing threshold cannot be negative.");
    }

    public override DbDataReader ReaderExecuted(DbCommand command, CommandExecutedEventData eventData, DbDataReader result)
    {
        this.LogIfSlow(command, eventData);
        return result;
    }

    public override ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command,
                                                               CommandExecutedEventData eventData,
                                                               DbDataReader result,
                                                               CancellationToken cancellationToken = default)
    {
        this.LogIfSlow(command, eventData);
        return new ValueTask<DbDataReader>(result);
    }

    public override object? ScalarExecuted(DbCommand command, CommandExecutedEventData eventData, object? result)
    {
        this.LogIfSlow(command, eventData);
        return result;
    }

    public override ValueTask<object?> ScalarExecutedAsync(DbCommand command,
                                                           CommandExecutedEventData eventData,
                                                           object? result,
                                                           CancellationToken cancellationToken = default)
    {
        this.LogIfSlow(command, eventData);
        return new ValueTask<object?>(result);
    }

    public override int NonQueryExecuted(DbCommand command, CommandExecutedEventData eventData, int result)
    {
        this.LogIfSlow(command, eventData);
        return result;
    }

    public override ValueTask<int> NonQueryExecutedAsync(DbCommand command,
                                                         CommandExecutedEventData eventData,
                                                         int result,
                                                         CancellationToken cancellationToken = default)
    {
        this.LogIfSlow(command, eventData);
        return new ValueTask<int>(result);
    }

    private void LogIfSlow(DbCommand command, CommandExecutedEventData eventData)
    {
        if (eventData.Duration.TotalMilliseconds < _options.ThresholdMilliseconds)
            return;

        if (_options.IncludeCommandText)
        {
            _logger.LogWarning("EF command {CommandKind} completed in {DurationMilliseconds} ms. SQL: {CommandText}",
                eventData.ExecuteMethod, eventData.Duration.TotalMilliseconds, command.CommandText);
            return;
        }

        _logger.LogWarning("EF command {CommandKind} completed in {DurationMilliseconds} ms",
            eventData.ExecuteMethod, eventData.Duration.TotalMilliseconds);
    }
}
