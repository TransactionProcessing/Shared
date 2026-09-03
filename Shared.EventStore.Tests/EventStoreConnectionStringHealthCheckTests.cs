using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using KurrentDB.Client;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Imposter.Abstractions;
using Shared.EventStore.EventStore;
using Shared.EventStore.Tests.TestObjects;
using Shared.Serialisation;
using Shouldly;
using SimpleResults;
using Xunit;

namespace Shared.EventStore.Tests;

public class EventStoreConnectionStringHealthCheckTests {

    public EventStoreConnectionStringHealthCheckTests() {
        StringSerialiser.Initialise(new SystemTextJsonSerializer(new JsonSerializerOptions()));
    }

    [Fact]
    public async Task EventStoreConnectionStringHealthCheck_CheckHealthAsync_EventsReturned_Healthy() {
        IEventStoreContextImposter context = new();
        AggregateNameSetEvent aggregateNameSetEvent = new(TestData.AggregateId, TestData.EventId, "Test");

        EventRecord r = TestData.CreateEventRecord<AggregateNameSetEvent>(aggregateNameSetEvent, "TestAggregate");

        var resolvedEvent = new ResolvedEvent(r, null,null);
        
        context.ReadLastEventsFromAll(Arg<long>.Any(), Arg<CancellationToken>.Any()).ReturnsAsync(Result.Success(new List<ResolvedEvent>() { resolvedEvent }));
        EventStoreConnectionStringHealthCheck healthCheck = new(context.Instance());
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);
        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public async Task EventStoreConnectionStringHealthCheck_CheckHealthAsync_NoEventsReturned_Unhealthy()
    {
        IEventStoreContextImposter context = new();
        
        context.ReadLastEventsFromAll(Arg<long>.Any(), Arg<CancellationToken>.Any()).ReturnsAsync(Result.Success(new List<ResolvedEvent>()));
        EventStoreConnectionStringHealthCheck healthCheck = new(context.Instance());
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);
        result.Status.ShouldBe(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task EventStoreConnectionStringHealthCheck_CheckHealthAsync_ReadLastEventsFromAll_Unhealthy()
    {
        IEventStoreContextImposter context = new();
        
        context.ReadLastEventsFromAll(Arg<long>.Any(), Arg<CancellationToken>.Any()).ReturnsAsync(Result.Failure());
        EventStoreConnectionStringHealthCheck healthCheck = new(context.Instance());
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);
        result.Status.ShouldBe(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task EventStoreConnectionStringHealthCheck_CheckHealthAsync_ExceptionThrown_Unhealthy()
    {
        IEventStoreContextImposter context = new();

        context.ReadLastEventsFromAll(Arg<long>.Any(), Arg<CancellationToken>.Any()).Throws(new Exception());
        EventStoreConnectionStringHealthCheck healthCheck = new(context.Instance());

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);
        result.Status.ShouldBe(HealthStatus.Unhealthy);
    }
}
