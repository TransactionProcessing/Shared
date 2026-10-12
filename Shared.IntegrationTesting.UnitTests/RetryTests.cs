namespace Shared.IntegrationTesting.UnitTests;

using System;
using System.Threading;
using System.Threading.Tasks;
using Shared.IntegrationTesting;
using Shouldly;

public class RetryTests
{
    [Fact]
    public async Task Retry_For_RetriesUntilActionSucceeds()
    {
        Int32 attempts = 0;

        await Retry.For(async () =>
        {
            attempts++;

            if (attempts < 2)
            {
                throw new InvalidOperationException("temporary failure");
            }

            await Task.CompletedTask;
        },
        TimeSpan.FromMilliseconds(100),
        TimeSpan.FromMilliseconds(1));

        attempts.ShouldBe(2);
    }

    [Fact]
    public async Task Retry_For_stops_immediately_when_cancelled()
    {
        using CancellationTokenSource cancellationTokenSource = new();
        cancellationTokenSource.Cancel();

        await Should.ThrowAsync<OperationCanceledException>(() => Retry.For(
            _ => Task.CompletedTask,
            cancellationTokenSource.Token,
            TimeSpan.FromMinutes(1),
            TimeSpan.FromSeconds(1)));
    }
}
