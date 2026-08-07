namespace Shared.IntegrationTesting.UnitTests;

using System;
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
}
