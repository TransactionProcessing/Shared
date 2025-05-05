using Shared.EventStore.Tests.TestObjects;
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
using Moq;
using Shouldly;
using Xunit;

public class AggregateRepositoryTests{
    [Fact]
    public async Task AggregateRepository_GetLatestVersion_AggregateReturned(){
        Mock<IEventStoreContext> context = new Mock<IEventStoreContext>();
        IDomainEventFactory<IDomainEvent> factory = new DomainEventFactory();
        AggregateRepository<TestAggregate, DomainEvent> testAggregateRepository = new AggregateRepository<TestAggregate, DomainEvent>(context.Object,factory);

        AggregateNameSetEvent aggregateNameSetEvent = new AggregateNameSetEvent(TestData.AggregateId, TestData.EventId, "Test");
        EventRecord r = TestData.CreateEventRecord<AggregateNameSetEvent>(aggregateNameSetEvent, "TestAggregate");
        List<ResolvedEvent> e = new List<ResolvedEvent>{
                                                           new ResolvedEvent(r, null, null)
                                                       };
        context.Setup(c => c.ReadEvents(It.IsAny<String>(), It.IsAny<Int64>(), It.IsAny<CancellationToken>())).ReturnsAsync(e);
        Result<TestAggregate> testAggregate = await testAggregateRepository.GetLatestVersion(TestData.AggregateId, CancellationToken.None);
        testAggregate.IsSuccess.ShouldBeTrue();
        testAggregate.Data.ShouldNotBeNull();
    }

    [Fact]
    public async Task AggregateRepository_GetLatestVersion_NotFound_AggregateReturned()
    {
        Mock<IEventStoreContext> context = new Mock<IEventStoreContext>();
        IDomainEventFactory<IDomainEvent> factory = new DomainEventFactory();
        AggregateRepository<TestAggregate, DomainEvent> testAggregateRepository = new AggregateRepository<TestAggregate, DomainEvent>(context.Object, factory);

        context.Setup(c => c.ReadEvents(It.IsAny<String>(), It.IsAny<Int64>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.NotFound("Stream doesnt exist"));
        Result<TestAggregate> testAggregate = await testAggregateRepository.GetLatestVersion(TestData.AggregateId, CancellationToken.None);
        testAggregate.IsFailed.ShouldBeTrue();
        testAggregate.Status.ShouldBe(ResultStatus.NotFound);
    }

    [Fact]
    public async Task AggregateRepository_GetLatestVersion_ErrorApplyingEvents_ErrorThrown()
    {
        Mock<IEventStoreContext> context = new Mock<IEventStoreContext>();
        IDomainEventFactory<IDomainEvent> factory = new DomainEventFactory();
        AggregateRepository<TestAggregate, DomainEvent> testAggregateRepository = new AggregateRepository<TestAggregate, DomainEvent>(context.Object, factory);
        AggregateNameSetEvent aggregateNameSetEvent = new AggregateNameSetEvent(TestData.AggregateId, TestData.EventId, "Error");
        EventRecord r = TestData.CreateEventRecord<AggregateNameSetEvent>(aggregateNameSetEvent, "TestAggregate");

        List<ResolvedEvent> e = new List<ResolvedEvent>{
                                                           new ResolvedEvent(r, null, null)
                                                       };
        context.Setup(c => c.ReadEvents(It.IsAny<String>(), It.IsAny<Int64>(), It.IsAny<CancellationToken>())).ReturnsAsync(e);
        Result<TestAggregate> result = await testAggregateRepository.GetLatestVersion(TestData.AggregateId, CancellationToken.None);
        result.IsFailed.ShouldBeTrue();
    }

    [Fact]
    public async Task AggregateRepository_GetLatestVersion_NoEvents_AggregateReturned()
    {
        Mock<IEventStoreContext> context = new Mock<IEventStoreContext>();

        List<ResolvedEvent> e = new();
        context.Setup(c => c.ReadEvents(It.IsAny<String>(), It.IsAny<Int64>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(e);

        IDomainEventFactory<IDomainEvent> factory = new DomainEventFactory();
        AggregateRepository<TestAggregate, DomainEvent> testAggregateRepository = new AggregateRepository<TestAggregate, DomainEvent>(context.Object, factory);
        Result<TestAggregate> testAggregate = await testAggregateRepository.GetLatestVersion(TestData.AggregateId, CancellationToken.None);
        testAggregate.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task AggregateRepository_GetLatestVersionFromLastEvent_AggregateReturned()
    {
        Mock<IEventStoreContext> context = new Mock<IEventStoreContext>();
        IDomainEventFactory<IDomainEvent> factory = new DomainEventFactory();

        AggregateRepository<TestAggregate, DomainEvent> testAggregateRepository = new AggregateRepository<TestAggregate, DomainEvent>(context.Object, factory);

        AggregateNameSetEvent aggregateNameSetEvent = new AggregateNameSetEvent(TestData.AggregateId, TestData.EventId, "Test");
        EventRecord r = TestData.CreateEventRecord<AggregateNameSetEvent>(aggregateNameSetEvent, "TestAggregate");
        List<ResolvedEvent> e = new List<ResolvedEvent>{
            new ResolvedEvent(r, null, null)
        };
        context.Setup(c => c.GetEventsBackward(It.IsAny<String>(), It.IsAny<Int32>(), It.IsAny<CancellationToken>())).ReturnsAsync(e);

        Result<TestAggregate> testAggregate = await testAggregateRepository.GetLatestVersionFromLastEvent(TestData.AggregateId, CancellationToken.None);
        testAggregate.IsSuccess.ShouldBeTrue();
    }
    
    [Fact]
    public async Task AggregateRepository_SaveChanges_NoChangesMade_ChangesAreSaved()
    {
        Mock<IEventStoreContext> context = new Mock<IEventStoreContext>();
        IDomainEventFactory<IDomainEvent> factory = new DomainEventFactory();

        AggregateNameSetEvent aggregateNameSetEvent = new AggregateNameSetEvent(TestData.AggregateId, TestData.EventId, "Test");
        EventRecord r = TestData.CreateEventRecord<AggregateNameSetEvent>(aggregateNameSetEvent, "TestAggregate");
        List<ResolvedEvent> e = new List<ResolvedEvent>{
            new ResolvedEvent(r, null, null)
        };
        context.Setup(c => c.GetEventsBackward(It.IsAny<String>(), It.IsAny<Int32>(), It.IsAny<CancellationToken>())).ReturnsAsync(e);

        AggregateRepository<TestAggregate, DomainEvent> testAggregateRepository = new AggregateRepository<TestAggregate, DomainEvent>(context.Object, factory);
        Result<TestAggregate> testAggregate = await testAggregateRepository.GetLatestVersionFromLastEvent(TestData.AggregateId, CancellationToken.None);

        Result result = await testAggregateRepository.SaveChanges(testAggregate.Data, CancellationToken.None);
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task AggregateRepository_SaveChanges_ErrorsOnInsert_FailedResult()
    {
        Mock<IEventStoreContext> context = new Mock<IEventStoreContext>();
        IDomainEventFactory<IDomainEvent> factory = new DomainEventFactory();

        AggregateNameSetEvent aggregateNameSetEvent = new AggregateNameSetEvent(TestData.AggregateId, TestData.EventId, "Test");
        EventRecord r = TestData.CreateEventRecord<AggregateNameSetEvent>(aggregateNameSetEvent, "TestAggregate");
        List<ResolvedEvent> e = new List<ResolvedEvent>{
            new ResolvedEvent(r, null, null)
        };
        context.Setup(c => c.GetEventsBackward(It.IsAny<String>(), It.IsAny<Int32>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(e));
        context.Setup(c => c.InsertEvents(It.IsAny<String>(), It.IsAny<long>(), It.IsAny<List<EventData>>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Failure("error"));

        AggregateRepository<TestAggregate, DomainEvent> testAggregateRepository = new AggregateRepository<TestAggregate, DomainEvent>(context.Object, factory);
        Result<TestAggregate> testAggregate = await testAggregateRepository.GetLatestVersionFromLastEvent(TestData.AggregateId, CancellationToken.None);
        testAggregate.Data.SetAggregateName("New name", Guid.NewGuid());
        Result result = await testAggregateRepository.SaveChanges(testAggregate.Data, CancellationToken.None);
        result.IsFailed.ShouldBeTrue();
    }

    [Fact]
    public async Task AggregateRepository_GetLatestVersionFromLastEvent_GetEventsFailed_FailedResult()
    {
        Mock<IEventStoreContext> context = new Mock<IEventStoreContext>();
        IDomainEventFactory<IDomainEvent> factory = new DomainEventFactory();

        AggregateRepository<TestAggregate, DomainEvent> testAggregateRepository = new AggregateRepository<TestAggregate, DomainEvent>(context.Object, factory);

        AggregateNameSetEvent aggregateNameSetEvent = new AggregateNameSetEvent(TestData.AggregateId, TestData.EventId, "Test");
        EventRecord r = TestData.CreateEventRecord<AggregateNameSetEvent>(aggregateNameSetEvent, "TestAggregate");
        List<ResolvedEvent> e = new List<ResolvedEvent>{
            new ResolvedEvent(r, null, null)
        };
        context.Setup(c => c.GetEventsBackward(It.IsAny<String>(), It.IsAny<Int32>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Failure("error"));

        Result<TestAggregate> result = await testAggregateRepository.GetLatestVersionFromLastEvent(TestData.AggregateId, CancellationToken.None);
        result.IsFailed.ShouldBeTrue();
    }

    [Fact]
    public async Task AggregateRepository_SaveChanges_ChangesMade_ChangesAreSaved()
    {
        Mock<IEventStoreContext> context = new Mock<IEventStoreContext>();
        context.Setup(c => c.InsertEvents(It.IsAny<String>(), It.IsAny<long>(), It.IsAny<List<EventData>>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success);
        IDomainEventFactory<IDomainEvent> factory = new DomainEventFactory();

        AggregateNameSetEvent aggregateNameSetEvent = new AggregateNameSetEvent(TestData.AggregateId, TestData.EventId, "Test");
        EventRecord r = TestData.CreateEventRecord<AggregateNameSetEvent>(aggregateNameSetEvent, "TestAggregate");
        List<ResolvedEvent> e = new List<ResolvedEvent>{
            new ResolvedEvent(r, null, null)
        };
        context.Setup(c => c.GetEventsBackward(It.IsAny<String>(), It.IsAny<Int32>(), It.IsAny<CancellationToken>())).ReturnsAsync(e);

        AggregateRepository<TestAggregate, DomainEvent> testAggregateRepository = new AggregateRepository<TestAggregate, DomainEvent>(context.Object, factory);
        Result<TestAggregate> testAggregaterResult = await testAggregateRepository.GetLatestVersionFromLastEvent(TestData.AggregateId, CancellationToken.None);
        var testAggregate = testAggregaterResult.Data;
        testAggregate.SetAggregateName("New name", Guid.NewGuid());
        Result result = await testAggregateRepository.SaveChanges(testAggregate, CancellationToken.None);
        result.IsSuccess.ShouldBeTrue();
    }
}