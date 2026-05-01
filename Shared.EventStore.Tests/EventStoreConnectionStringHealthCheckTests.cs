using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using KurrentDB.Client;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
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
        Mock<IEventStoreContext> context = new();
        AggregateNameSetEvent aggregateNameSetEvent = new(TestData.AggregateId, TestData.EventId, "Test");

        EventRecord r = TestData.CreateEventRecord<AggregateNameSetEvent>(aggregateNameSetEvent, "TestAggregate");

        var resolvedEvent = new ResolvedEvent(r, null,null);
        
        context.Setup(c => c.ReadLastEventsFromAll(It.IsAny<long>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(new List<ResolvedEvent>() { resolvedEvent }));
        EventStoreConnectionStringHealthCheck healthCheck = new(context.Object);
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);
        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public async Task EventStoreConnectionStringHealthCheck_CheckHealthAsync_NoEventsReturned_Unhealthy()
    {
        Mock<IEventStoreContext> context = new();
        
        context.Setup(c => c.ReadLastEventsFromAll(It.IsAny<long>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(new List<ResolvedEvent>()));
        EventStoreConnectionStringHealthCheck healthCheck = new(context.Object);
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);
        result.Status.ShouldBe(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task EventStoreConnectionStringHealthCheck_CheckHealthAsync_ReadLastEventsFromAll_Unhealthy()
    {
        Mock<IEventStoreContext> context = new();
        
        context.Setup(c => c.ReadLastEventsFromAll(It.IsAny<long>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Failure());
        EventStoreConnectionStringHealthCheck healthCheck = new(context.Object);
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);
        result.Status.ShouldBe(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task EventStoreConnectionStringHealthCheck_CheckHealthAsync_ExceptionThrown_Unhealthy()
    {
        Mock<IEventStoreContext> context = new();

        context.Setup(c => c.ReadLastEventsFromAll(It.IsAny<long>(), It.IsAny<CancellationToken>())).ThrowsAsync(new Exception());
        EventStoreConnectionStringHealthCheck healthCheck = new(context.Object);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);
        result.Status.ShouldBe(HealthStatus.Unhealthy);
    }
}