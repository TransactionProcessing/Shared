using System.Text.Json;
using KurrentDB.Client;
using Shared.EventStore.Tests.TestObjects;
using Shared.Serialisation;
using SimpleResults;

namespace Shared.EventStore.Tests;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aggregate;
using DomainDrivenDesign.EventSourcing;
using EventStore;
using global::EventStore.Client;
using Imposter.Abstractions;
using Shouldly;
using Xunit;

public class AggregateRepositoryTests{

    public AggregateRepositoryTests() {
        StringSerialiser.Initialise(new SystemTextJsonSerializer(new JsonSerializerOptions()));
    }

    [Fact]
    public async Task AggregateRepository_GetLatestVersion_AggregateReturned(){
        IEventStoreContextImposter context = new();
        IDomainEventFactory<IDomainEvent> factory = new DomainEventFactory();
        AggregateRepository<TestAggregate, DomainEvent> testAggregateRepository = new(context.Instance(),factory);

        AggregateNameSetEvent aggregateNameSetEvent = new(TestData.AggregateId, TestData.EventId, "Test");
        EventRecord r = TestData.CreateEventRecord<AggregateNameSetEvent>(aggregateNameSetEvent, "TestAggregate");
        List<ResolvedEvent> e = new(){
                                                           new ResolvedEvent(r, null, null)
                                                       };
        context.ReadEvents(Arg<String>.Any(), Arg<Int64>.Any(), Arg<CancellationToken>.Any()).ReturnsAsync(e);
        Result<TestAggregate> testAggregate = await testAggregateRepository.GetLatestVersion(TestData.AggregateId, CancellationToken.None);
        testAggregate.IsSuccess.ShouldBeTrue();
        testAggregate.Data.ShouldNotBeNull();
    }

    [Fact]
    public async Task AggregateRepository_GetLatestVersion_NotFound_AggregateReturned()
    {
        IEventStoreContextImposter context = new IEventStoreContextImposter();
        IDomainEventFactory<IDomainEvent> factory = new DomainEventFactory();
        AggregateRepository<TestAggregate, DomainEvent> testAggregateRepository = new AggregateRepository<TestAggregate, DomainEvent>(context.Instance(), factory);

        context.ReadEvents(Arg<String>.Any(), Arg<Int64>.Any(), Arg<CancellationToken>.Any()).ReturnsAsync(Result.NotFound("Stream doesnt exist"));
        Result<TestAggregate> testAggregate = await testAggregateRepository.GetLatestVersion(TestData.AggregateId, CancellationToken.None);
        testAggregate.IsFailed.ShouldBeTrue();
        testAggregate.Status.ShouldBe(ResultStatus.NotFound);
    }

    [Fact]
    public async Task AggregateRepository_GetLatestVersion_ErrorApplyingEvents_ErrorThrown()
    {
        IEventStoreContextImposter context = new();
        IDomainEventFactory<IDomainEvent> factory = new DomainEventFactory();
        AggregateRepository<TestAggregate, DomainEvent> testAggregateRepository = new(context.Instance(), factory);
        AggregateNameSetEvent aggregateNameSetEvent = new(TestData.AggregateId, TestData.EventId, "Error");
        EventRecord r = TestData.CreateEventRecord<AggregateNameSetEvent>(aggregateNameSetEvent, "TestAggregate");

        List<ResolvedEvent> e = new()
        {
            new ResolvedEvent(r, null, null)
        };
        context.ReadEvents(Arg<String>.Any(), Arg<Int64>.Any(), Arg<CancellationToken>.Any()).ReturnsAsync(e);
        Result<TestAggregate> result = await testAggregateRepository.GetLatestVersion(TestData.AggregateId, CancellationToken.None);
        result.IsFailed.ShouldBeTrue();
    }

    [Fact]
    public async Task AggregateRepository_GetLatestVersion_NoEvents_AggregateReturned()
    {
        IEventStoreContextImposter context = new IEventStoreContextImposter();

        List<ResolvedEvent> e = new();
        context.ReadEvents(Arg<String>.Any(), Arg<Int64>.Any(), Arg<CancellationToken>.Any())
            .ReturnsAsync(e);

        IDomainEventFactory<IDomainEvent> factory = new DomainEventFactory();
        AggregateRepository<TestAggregate, DomainEvent> testAggregateRepository = new AggregateRepository<TestAggregate, DomainEvent>(context.Instance(), factory);
        Result<TestAggregate> testAggregate = await testAggregateRepository.GetLatestVersion(TestData.AggregateId, CancellationToken.None);
        testAggregate.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task AggregateRepository_GetLatestVersionFromLastEvent_AggregateReturned()
    {
        IEventStoreContextImposter context = new();
        IDomainEventFactory<IDomainEvent> factory = new DomainEventFactory();

        AggregateRepository<TestAggregate, DomainEvent> testAggregateRepository = new(context.Instance(), factory);

        AggregateNameSetEvent aggregateNameSetEvent = new(TestData.AggregateId, TestData.EventId, "Test");
        EventRecord r = TestData.CreateEventRecord<AggregateNameSetEvent>(aggregateNameSetEvent, "TestAggregate");
        List<ResolvedEvent> e = new(){
            new ResolvedEvent(r, null, null)
        };
        context.GetEventsBackward(Arg<String>.Any(), Arg<Int32>.Any(), Arg<CancellationToken>.Any()).ReturnsAsync(e);

        Result<TestAggregate> testAggregate = await testAggregateRepository.GetLatestVersionFromLastEvent(TestData.AggregateId, CancellationToken.None);
        testAggregate.IsSuccess.ShouldBeTrue();
    }
    
    [Fact]
    public async Task AggregateRepository_SaveChanges_NoChangesMade_ChangesAreSaved()
    {
        IEventStoreContextImposter context = new();
        IDomainEventFactory<IDomainEvent> factory = new DomainEventFactory();

        AggregateNameSetEvent aggregateNameSetEvent = new(TestData.AggregateId, TestData.EventId, "Test");
        EventRecord r = TestData.CreateEventRecord<AggregateNameSetEvent>(aggregateNameSetEvent, "TestAggregate");
        List<ResolvedEvent> e = new(){
            new ResolvedEvent(r, null, null)
        };
        context.GetEventsBackward(Arg<String>.Any(), Arg<Int32>.Any(), Arg<CancellationToken>.Any()).ReturnsAsync(e);

        AggregateRepository<TestAggregate, DomainEvent> testAggregateRepository = new(context.Instance(), factory);
        Result<TestAggregate> testAggregate = await testAggregateRepository.GetLatestVersionFromLastEvent(TestData.AggregateId, CancellationToken.None);

        Result result = await testAggregateRepository.SaveChanges(testAggregate.Data, CancellationToken.None);
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task AggregateRepository_SaveChanges_ErrorsOnInsert_FailedResult()
    {
        IEventStoreContextImposter context = new();
        IDomainEventFactory<IDomainEvent> factory = new DomainEventFactory();

        AggregateNameSetEvent aggregateNameSetEvent = new(TestData.AggregateId, TestData.EventId, "Test");
        EventRecord r = TestData.CreateEventRecord<AggregateNameSetEvent>(aggregateNameSetEvent, "TestAggregate");
        List<ResolvedEvent> e = new() {
            new ResolvedEvent(r, null, null)
        };
        context.GetEventsBackward(Arg<String>.Any(), Arg<Int32>.Any(), Arg<CancellationToken>.Any()).ReturnsAsync(Result.Success(e));
        context.InsertEvents(Arg<String>.Any(), Arg<long>.Any(), Arg<List<EventData>>.Any(), Arg<CancellationToken>.Any()).ReturnsAsync(Result.Failure("error"));

        AggregateRepository<TestAggregate, DomainEvent> testAggregateRepository = new(context.Instance(), factory);
        Result<TestAggregate> testAggregate = await testAggregateRepository.GetLatestVersionFromLastEvent(TestData.AggregateId, CancellationToken.None);
        testAggregate.Data.SetAggregateName("New name", Guid.NewGuid());
        Result result = await testAggregateRepository.SaveChanges(testAggregate.Data, CancellationToken.None);
        result.IsFailed.ShouldBeTrue();
    }

    [Fact]
    public async Task AggregateRepository_GetLatestVersionFromLastEvent_GetEventsFailed_FailedResult()
    {
        IEventStoreContextImposter context = new();
        IDomainEventFactory<IDomainEvent> factory = new DomainEventFactory();

        AggregateRepository<TestAggregate, DomainEvent> testAggregateRepository = new(context.Instance(), factory);

        context.GetEventsBackward(Arg<String>.Any(), Arg<Int32>.Any(), Arg<CancellationToken>.Any()).ReturnsAsync(Result.Failure("error"));

        Result<TestAggregate> result = await testAggregateRepository.GetLatestVersionFromLastEvent(TestData.AggregateId, CancellationToken.None);
        result.IsFailed.ShouldBeTrue();
    }

    [Fact]
    public async Task AggregateRepository_SaveChanges_ChangesMade_ChangesAreSaved()
    {
        IEventStoreContextImposter context = new();
        context.InsertEvents(Arg<String>.Any(), Arg<long>.Any(), Arg<List<EventData>>.Any(), Arg<CancellationToken>.Any()).ReturnsAsync(Result.Success());
        IDomainEventFactory<IDomainEvent> factory = new DomainEventFactory();

        AggregateNameSetEvent aggregateNameSetEvent = new(TestData.AggregateId, TestData.EventId, "Test");
        EventRecord r = TestData.CreateEventRecord<AggregateNameSetEvent>(aggregateNameSetEvent, "TestAggregate");
        List<ResolvedEvent> e = new() {
            new ResolvedEvent(r, null, null)
        };
        context.GetEventsBackward(Arg<String>.Any(), Arg<Int32>.Any(), Arg<CancellationToken>.Any()).ReturnsAsync(e);

        AggregateRepository<TestAggregate, DomainEvent> testAggregateRepository = new(context.Instance(), factory);
        Result<TestAggregate> testAggregaterResult = await testAggregateRepository.GetLatestVersionFromLastEvent(TestData.AggregateId, CancellationToken.None);
        var testAggregate = testAggregaterResult.Data;
        testAggregate.SetAggregateName("New name", Guid.NewGuid());
        Result result = await testAggregateRepository.SaveChanges(testAggregate, CancellationToken.None);
        result.IsSuccess.ShouldBeTrue();
    }
}
