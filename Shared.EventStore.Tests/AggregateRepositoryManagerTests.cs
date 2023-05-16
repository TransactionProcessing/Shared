namespace Shared.EventStore.Tests;

using System;
using Aggregate;
using DomainDrivenDesign.EventSourcing;
using EventStore;
using Moq;
using Shouldly;
using Xunit;

public class AggregateRepositoryManagerTests{
    [Fact]
    public void AggregateRepositoryManager_GetAggregateRepository_AggregateRepositoryReturned(){
        Mock<IEventStoreContextManager> eventStoreContextManager = new Mock<IEventStoreContextManager>();
        AggregateRepositoryManager aggregateRepositoryManager = new AggregateRepositoryManager(eventStoreContextManager.Object);
        IAggregateRepository<TestAggregate, DomainEvent> r = aggregateRepositoryManager.GetAggregateRepository<TestAggregate, DomainEvent>(Guid.NewGuid());
        r.ShouldNotBeNull();

    }
}