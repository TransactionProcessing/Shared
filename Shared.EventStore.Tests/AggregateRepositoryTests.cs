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
        var testAggregate = await testAggregateRepository.GetLatestVersion(TestData.AggregateId, CancellationToken.None);
        testAggregate.ShouldNotBeNull();
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
        Should.Throw<Exception>(async () => {
                                    await testAggregateRepository.GetLatestVersion(TestData.AggregateId, CancellationToken.None);
                                });
    }

    [Fact]
    public async Task AggregateRepository_GetLatestVersion_NoEvents_AggregateReturned()
    {
        Mock<IEventStoreContext> context = new Mock<IEventStoreContext>();
        IDomainEventFactory<IDomainEvent> factory = new DomainEventFactory();
        AggregateRepository<TestAggregate, DomainEvent> testAggregateRepository = new AggregateRepository<TestAggregate, DomainEvent>(context.Object, factory);
        var testAggregate = await testAggregateRepository.GetLatestVersion(TestData.AggregateId, CancellationToken.None);
        testAggregate.ShouldNotBeNull();
    }

    [Fact]
    public async Task AggregateRepository_GetLatestVersionFromLastEvent_AggregateReturned()
    {
        Mock<IEventStoreContext> context = new Mock<IEventStoreContext>();
        IDomainEventFactory<IDomainEvent> factory = new DomainEventFactory();
        AggregateRepository<TestAggregate, DomainEvent> testAggregateRepository = new AggregateRepository<TestAggregate, DomainEvent>(context.Object, factory);
        var testAggregate = await testAggregateRepository.GetLatestVersionFromLastEvent(TestData.AggregateId, CancellationToken.None);
        testAggregate.ShouldNotBeNull();
    }
    
    [Fact]
    public async Task AggregateRepository_SaveChanges_NoChangesMade_ChangesAreSaved()
    {
        Mock<IEventStoreContext> context = new Mock<IEventStoreContext>();
        IDomainEventFactory<IDomainEvent> factory = new DomainEventFactory();
        AggregateRepository<TestAggregate, DomainEvent> testAggregateRepository = new AggregateRepository<TestAggregate, DomainEvent>(context.Object, factory);
        var testAggregate = await testAggregateRepository.GetLatestVersionFromLastEvent(TestData.AggregateId, CancellationToken.None);

        Should.NotThrow(async () => {
                            await testAggregateRepository.SaveChanges(testAggregate, CancellationToken.None);
                        });
    }
    
    [Fact]
    public async Task AggregateRepository_SaveChanges_ChangesMade_ChangesAreSaved()
    {
        Mock<IEventStoreContext> context = new Mock<IEventStoreContext>();
        IDomainEventFactory<IDomainEvent> factory = new DomainEventFactory();
        AggregateRepository<TestAggregate, DomainEvent> testAggregateRepository = new AggregateRepository<TestAggregate, DomainEvent>(context.Object, factory);
        TestAggregate testAggregate = await testAggregateRepository.GetLatestVersionFromLastEvent(TestData.AggregateId, CancellationToken.None);

        Should.NotThrow(async () => {
                            await testAggregateRepository.SaveChanges(testAggregate, CancellationToken.None);
                        });
    }
}