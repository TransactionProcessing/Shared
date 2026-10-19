using System;
using System.Threading;
using System.Threading.Tasks;

namespace Shared.IntegrationTesting.Tests;

using NLog;
using Reqnroll;
using Shared.Logger;
using Shouldly;

[Binding]
public class Setup
{
    private static readonly SemaphoreSlim SqlServerFixtureLock = new(1, 1);
    private static Shared.IntegrationTesting.TestContainers.SqlServerTestRunFixture? sqlServerFixture;
    private static long InfrastructureStartupStartedTimestamp;
    private static long InfrastructureStartupCompletedTimestamp;

    public static async Task<Shared.IntegrationTesting.TestContainers.SqlServerTestRunFixture> GetSqlServerFixtureAsync()
    {
        await SqlServerFixtureLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (sqlServerFixture is null)
            {
                System.Diagnostics.Stopwatch startupTimer = System.Diagnostics.Stopwatch.StartNew();
                try
                {
                    sqlServerFixture = await Shared.IntegrationTesting.TestContainers.SqlServerTestRunFixture
                        .StartAsync().ConfigureAwait(false);
                }
                finally
                {
                    startupTimer.Stop();
                    LogManager.GetLogger(nameof(Setup)).Info(
                        "Shared SQL Server fixture startup completed in {0:N1}s",
                        startupTimer.Elapsed.TotalSeconds);
                }
            }

            return sqlServerFixture;
        }
        finally
        {
            SqlServerFixtureLock.Release();
        }
    }

    [BeforeTestRun]
    public static Task GlobalSetup()
    {
        ShouldlyConfiguration.DefaultTaskTimeout = TimeSpan.FromMinutes(1);
        return Task.CompletedTask;
    }

    [AfterTestRun]
    public static async Task GlobalTeardown()
    {
        try
        {
            if (sqlServerFixture is not null)
            {
                await sqlServerFixture.DisposeAsync().ConfigureAwait(false);
                sqlServerFixture = null;
            }
        }
        finally
        {
            long startedTimestamp = Interlocked.Read(ref InfrastructureStartupStartedTimestamp);
            long completedTimestamp = Interlocked.Read(ref InfrastructureStartupCompletedTimestamp);
            TimeSpan totalStartup = startedTimestamp == 0 || completedTimestamp < startedTimestamp
                ? TimeSpan.Zero
                : TimeSpan.FromSeconds(
                    (completedTimestamp - startedTimestamp) / (double)System.Diagnostics.Stopwatch.Frequency);
            LogManager.GetLogger(nameof(Setup)).Info(
                "Integration-test infrastructure startup wall time (first scenario request to last scenario ready): {0:N1}s",
                totalStartup.TotalSeconds);
        }
    }

    internal static void RecordInfrastructureStartupStarted()
    {
        long timestamp = System.Diagnostics.Stopwatch.GetTimestamp();
        Interlocked.CompareExchange(ref InfrastructureStartupStartedTimestamp, timestamp, 0);
    }

    internal static void RecordInfrastructureStartupCompleted()
    {
        long timestamp = System.Diagnostics.Stopwatch.GetTimestamp();
        long previousTimestamp;
        do
        {
            previousTimestamp = Interlocked.Read(ref InfrastructureStartupCompletedTimestamp);
            if (previousTimestamp >= timestamp)
            {
                return;
            }
        }
        while (Interlocked.CompareExchange(
                   ref InfrastructureStartupCompletedTimestamp,
                   timestamp,
                   previousTimestamp) != previousTimestamp);
    }
}
