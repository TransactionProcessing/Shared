#nullable enable
using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Shared.EntityFramework;
using Shouldly;
using Xunit;

namespace Shared.Tests;

public sealed class QueryTimingInterceptorTests
{
    [Fact]
    public async Task Interceptor_LogsSyncAndAsyncReaderScalarAndNonQueryCommandsWithoutSqlByDefault()
    {
        RecordingLoggerProvider provider = new();
        using ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddProvider(provider));
        QueryTimingInterceptor interceptor = new(loggerFactory.CreateLogger<QueryTimingInterceptor>());
        using DbCommand command = new SqlCommand("SELECT sensitive_value");

        using DbDataReader reader = new DataTable().CreateDataReader();
        interceptor.ReaderExecuted(command, CreateEventData(command, DbCommandMethod.ExecuteReader), reader);
        await interceptor.ReaderExecutedAsync(command, CreateEventData(command, DbCommandMethod.ExecuteReader), reader, TestContext.Current.CancellationToken);
        interceptor.ScalarExecuted(command, CreateEventData(command, DbCommandMethod.ExecuteScalar), 1);
        await interceptor.ScalarExecutedAsync(command, CreateEventData(command, DbCommandMethod.ExecuteScalar), 1, TestContext.Current.CancellationToken);
        interceptor.NonQueryExecuted(command, CreateEventData(command, DbCommandMethod.ExecuteNonQuery), 1);
        await interceptor.NonQueryExecutedAsync(command, CreateEventData(command, DbCommandMethod.ExecuteNonQuery), 1, TestContext.Current.CancellationToken);

        provider.Count.ShouldBe(6);
        provider.Messages.ShouldAllBe(message => message.Contains("completed in 600 ms"));
        provider.Messages.ShouldAllBe(message => !message.Contains("sensitive_value"));
    }

    [Fact]
    public void Interceptor_UsesConfiguredThresholdAndCanIncludeCommandText()
    {
        RecordingLoggerProvider provider = new();
        using ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddProvider(provider));
        QueryTimingInterceptor interceptor = new(loggerFactory.CreateLogger<QueryTimingInterceptor>(), new QueryTimingInterceptorOptions
        {
            ThresholdMilliseconds = 750,
            IncludeCommandText = true
        });
        using DbCommand command = new SqlCommand("SELECT explicitly_enabled_value");

        interceptor.ScalarExecuted(command, CreateEventData(command, DbCommandMethod.ExecuteScalar, TimeSpan.FromMilliseconds(749)), 1);
        provider.Count.ShouldBe(0);

        interceptor.ScalarExecuted(command, CreateEventData(command, DbCommandMethod.ExecuteScalar, TimeSpan.FromMilliseconds(750)), 1);
        provider.Count.ShouldBe(1);
        provider.Messages[0].ShouldContain("explicitly_enabled_value");
    }

    private static CommandExecutedEventData CreateEventData(DbCommand command,
                                                              DbCommandMethod method,
                                                              TimeSpan? duration = null)
    {
        return new CommandExecutedEventData(
            null!,
            (_, _) => String.Empty,
            null!,
            command,
            command.CommandText,
            null,
            method,
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            false,
            false,
            DateTimeOffset.UtcNow,
            duration ?? TimeSpan.FromMilliseconds(600),
            CommandSource.Unknown);
    }

    private sealed class RecordingLoggerProvider : ILoggerProvider
    {
        public int Count => this.Messages.Count;
        public System.Collections.Generic.List<string> Messages { get; } = new();

        public ILogger CreateLogger(string categoryName) => new RecordingLogger(this.Messages);
        public void Dispose() { }
    }

    private sealed class RecordingLogger : ILogger
    {
        private readonly System.Collections.Generic.List<string> _messages;

        public RecordingLogger(System.Collections.Generic.List<string> messages) => _messages = messages;
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel,
                                EventId eventId,
                                TState state,
                                Exception? exception,
                                Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Warning)
                _messages.Add(formatter(state, exception));
        }
    }
}
