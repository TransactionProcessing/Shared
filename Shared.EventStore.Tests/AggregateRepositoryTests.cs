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
using Shared.EventStore.Tests.TestObjects;
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

        Result result = await testAggregateRepository.SaveChanges(testAggregate, CancellationToken.None);
        result.IsSuccess.ShouldBeTrue();
    }
    
    [Fact]
    public async Task AggregateRepository_SaveChanges_ChangesMade_ChangesAreSaved()
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
        TestAggregate testAggregate = await testAggregateRepository.GetLatestVersionFromLastEvent(TestData.AggregateId, CancellationToken.None);
        testAggregate.SetAggregateName("New name");
        Result result = await testAggregateRepository.SaveChanges(testAggregate, CancellationToken.None);
        result.IsSuccess.ShouldBeTrue();
    }
}