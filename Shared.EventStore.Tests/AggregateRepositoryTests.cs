namespace Shared.EventStore.Tests;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aggregate;
using CSharpFunctionalExtensions;
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
        AggregateRepository<TestAggregate, DomainEvent> testAggregateRepository = new AggregateRepository<TestAggregate, DomainEvent>(context.Object);
        
        AggregateNameSetEvent aggregateNameSetEvent = new AggregateNameSetEvent(TestData.AggregateId,TestData.EventId, "Test");
        EventRecord r = TestData.CreateEventRecord<AggregateNameSetEvent>(aggregateNameSetEvent,"TestAggregate");
        List<ResolvedEvent> e = new List<ResolvedEvent>{
                                                           new ResolvedEvent(r, null, null)
                                                       };
        context.Setup(c => c.ReadEvents(It.IsAny<String>(), It.IsAny<Int64>(), It.IsAny<CancellationToken>())).ReturnsAsync(e);
        Result<TestAggregate> result = await testAggregateRepository.GetLatestVersion(TestData.AggregateId, CancellationToken.None);
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
    }
    
    [Fact]
    public async Task AggregateRepository_GetLatestVersion_ErrorApplyingEvents_ErrorThrown()
    {
        Mock<IEventStoreContext> context = new Mock<IEventStoreContext>();
        AggregateRepository<TestAggregate, DomainEvent> testAggregateRepository = new AggregateRepository<TestAggregate, DomainEvent>(context.Object);
        AggregateNameSetEvent aggregateNameSetEvent = new AggregateNameSetEvent(TestData.AggregateId, TestData.EventId, "Error");
        EventRecord r = TestData.CreateEventRecord<AggregateNameSetEvent>(aggregateNameSetEvent, "TestAggregate");

        List<ResolvedEvent> e = new List<ResolvedEvent>{
                                                           new ResolvedEvent(r, null, null)
                                                       };
        context.Setup(c => c.ReadEvents(It.IsAny<String>(), It.IsAny<Int64>(), It.IsAny<CancellationToken>())).ReturnsAsync(e);
        Result<TestAggregate> result = await testAggregateRepository.GetLatestVersion(TestData.AggregateId, CancellationToken.None);
        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public async Task AggregateRepository_GetLatestVersion_NoEvents_AggregateReturned()
    {
        Mock<IEventStoreContext> context = new Mock<IEventStoreContext>();
        AggregateRepository<TestAggregate, DomainEvent> testAggregateRepository = new AggregateRepository<TestAggregate, DomainEvent>(context.Object);
        Result<TestAggregate> result = await testAggregateRepository.GetLatestVersion(TestData.AggregateId, CancellationToken.None);
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
    }

    [Fact]
    public async Task AggregateRepository_GetLatestVersionFromLastEvent_AggregateReturned()
    {
        Mock<IEventStoreContext> context = new Mock<IEventStoreContext>();
        AggregateRepository<TestAggregate, DomainEvent> testAggregateRepository = new AggregateRepository<TestAggregate, DomainEvent>(context.Object);
        Result<TestAggregate> result = await testAggregateRepository.GetLatestVersionFromLastEvent(TestData.AggregateId, CancellationToken.None);
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
    }

    [Fact]
    public async Task AggregateRepository_GetLatestVersionFromLastEvent_ExceptionThrown_FailedResultReturned()
    {
        Mock<IEventStoreContext> context = new Mock<IEventStoreContext>();
        context.Setup(c => c.GetEventsBackward(It.IsAny<String>(), It.IsAny<Int32>(), It.IsAny<CancellationToken>())).Throws<Exception>();
        AggregateRepository<TestAggregate, DomainEvent> testAggregateRepository = new AggregateRepository<TestAggregate, DomainEvent>(context.Object);
        Result<TestAggregate> result = await testAggregateRepository.GetLatestVersionFromLastEvent(TestData.AggregateId, CancellationToken.None);
        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public async Task AggregateRepository_SaveChanges_NoChangesMade_ChangesAreSaved()
    {
        Mock<IEventStoreContext> context = new Mock<IEventStoreContext>();
        AggregateRepository<TestAggregate, DomainEvent> testAggregateRepository = new AggregateRepository<TestAggregate, DomainEvent>(context.Object);
        Result<TestAggregate> getResult = await testAggregateRepository.GetLatestVersionFromLastEvent(TestData.AggregateId, CancellationToken.None);
            
        Result saveResult = await testAggregateRepository.SaveChanges(getResult.Value, CancellationToken.None);
        saveResult.IsSuccess.ShouldBeTrue();
    }
    
    [Fact]
    public async Task AggregateRepository_SaveChanges_ChangesMade_ChangesAreSaved()
    {
        Mock<IEventStoreContext> context = new Mock<IEventStoreContext>();
        AggregateRepository<TestAggregate, DomainEvent> testAggregateRepository = new AggregateRepository<TestAggregate, DomainEvent>(context.Object);
        Result<TestAggregate> getResult = await testAggregateRepository.GetLatestVersionFromLastEvent(TestData.AggregateId, CancellationToken.None);
        getResult.Value.SetAggregateName("TestName");

        Result saveResult = await testAggregateRepository.SaveChanges(getResult.Value, CancellationToken.None);
        saveResult.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task AggregateRepository_SaveChanges_ExceptionThrown_FailedResultReturned()
    {
        Mock<IEventStoreContext> context = new Mock<IEventStoreContext>();
        context.Setup(c => c.InsertEvents(It.IsAny<String>(), It.IsAny<Int64>(), It.IsAny<List<EventData>>(), It.IsAny<CancellationToken>())).Throws<Exception>();
        AggregateRepository<TestAggregate, DomainEvent> testAggregateRepository = new AggregateRepository<TestAggregate, DomainEvent>(context.Object);
        Result<TestAggregate> getResult = await testAggregateRepository.GetLatestVersionFromLastEvent(TestData.AggregateId, CancellationToken.None);
        getResult.Value.SetAggregateName("TestName");

        Result saveResult = await testAggregateRepository.SaveChanges(getResult.Value, CancellationToken.None);
        saveResult.IsFailure.ShouldBeTrue();
    }
}