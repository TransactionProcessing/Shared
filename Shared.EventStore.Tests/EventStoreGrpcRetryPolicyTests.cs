using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Configuration;
using Shared.EventStore;
using Shared.General;
using Shouldly;
using Xunit;

namespace Shared.EventStore.Tests;

public class EventStoreGrpcRetryPolicyTests : IDisposable
{
    [Fact]
    public async Task ExecuteAsync_RetriesUnavailableAndSucceeds()
    {
        this.InitialiseConfiguration(maxAttempts: 3, baseDelayMilliseconds: 1, maxDelayMilliseconds: 1, useJitter: false);

        int attempts = 0;
        List<string> logs = new();

        int result = await EventStoreGrpcRetryPolicy.ExecuteAsync(async () =>
        {
            attempts++;

            if (attempts < 3)
            {
                throw new RpcException(new Status(StatusCode.Unavailable, "temporary transport failure"));
            }

            await Task.CompletedTask;
            return 42;
        }, "RetryOperation", "scope-1", (level, message) => logs.Add($"{level}:{message}"));

        result.ShouldBe(42);
        attempts.ShouldBe(3);
        logs.Count.ShouldBe(2);
        logs[0].ShouldStartWith("Warning:RetryOperation retry 1");
        logs[1].ShouldStartWith("Warning:RetryOperation retry 2");
    }

    [Fact]
    public async Task ExecuteAsync_RetriesHttpRequestExceptionAndSucceeds()
    {
        this.InitialiseConfiguration(maxAttempts: 3, baseDelayMilliseconds: 1, maxDelayMilliseconds: 1, useJitter: false);

        int attempts = 0;
        List<string> logs = new();

        int result = await EventStoreGrpcRetryPolicy.ExecuteAsync(async () =>
        {
            attempts++;

            if (attempts < 3)
            {
                throw new HttpRequestException("temporary transport failure");
            }

            await Task.CompletedTask;
            return 84;
        }, "RetryOperation", "scope-1b", (level, message) => logs.Add($"{level}:{message}"));

        result.ShouldBe(84);
        attempts.ShouldBe(3);
        logs.Count.ShouldBe(2);
        logs[0].ShouldStartWith("Warning:RetryOperation retry 1");
        logs[1].ShouldStartWith("Warning:RetryOperation retry 2");
    }

    [Fact]
    public async Task ExecuteAsync_RetriesAggregateHttpRequestExceptionAndSucceeds()
    {
        this.InitialiseConfiguration(maxAttempts: 3, baseDelayMilliseconds: 1, maxDelayMilliseconds: 1, useJitter: false);

        int attempts = 0;
        List<string> logs = new();

        int result = await EventStoreGrpcRetryPolicy.ExecuteAsync(async () =>
        {
            attempts++;

            if (attempts < 3)
            {
                throw new AggregateException(new HttpRequestException("temporary transport failure"));
            }

            await Task.CompletedTask;
            return 128;
        }, "RetryOperation", "scope-1c", (level, message) => logs.Add($"{level}:{message}"));

        result.ShouldBe(128);
        attempts.ShouldBe(3);
        logs.Count.ShouldBe(2);
        logs[0].ShouldStartWith("Warning:RetryOperation retry 1");
        logs[1].ShouldStartWith("Warning:RetryOperation retry 2");
    }

    [Fact]
    public async Task ExecuteAsync_StopsAfterConfiguredRetries()
    {
        this.InitialiseConfiguration(maxAttempts: 2, baseDelayMilliseconds: 1, maxDelayMilliseconds: 1, useJitter: false);

        int attempts = 0;
        List<string> logs = new();

        await Should.ThrowAsync<RpcException>(async () =>
            await EventStoreGrpcRetryPolicy.ExecuteAsync<int>(async () =>
            {
                attempts++;
                await Task.CompletedTask;
                throw new RpcException(new Status(StatusCode.Unavailable, "temporary transport failure"));
            }, "RetryOperation", "scope-2", (level, message) => logs.Add($"{level}:{message}")));

        attempts.ShouldBe(2);
        logs.Count.ShouldBe(2);
        logs.Any(log => log.StartsWith("Warning:RetryOperation retry 1")).ShouldBeTrue();
        logs.Any(log => log.StartsWith("Error:RetryOperation failed")).ShouldBeTrue();
    }

    private void InitialiseConfiguration(int maxAttempts,
                                         int baseDelayMilliseconds,
                                         int maxDelayMilliseconds,
                                         bool useJitter)
    {
        IConfigurationBuilder builder = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["AppSettings:GrpcRetryMaxAttempts"] = maxAttempts.ToString(),
            ["AppSettings:GrpcRetryBaseDelayMilliseconds"] = baseDelayMilliseconds.ToString(),
            ["AppSettings:GrpcRetryMaxDelayMilliseconds"] = maxDelayMilliseconds.ToString(),
            ["AppSettings:GrpcRetryUseJitter"] = useJitter.ToString(),
        });

        ConfigurationReader.Initialise(builder.Build());
    }

    public void Dispose()
    {
        ConfigurationReader.Initialise(new ConfigurationRoot(new List<IConfigurationProvider>()));
    }
}
