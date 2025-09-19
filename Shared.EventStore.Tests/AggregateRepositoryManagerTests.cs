using Shared.EventStore.Tests.TestObjects;

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
        Mock<IEventStoreContextManager> eventStoreContextManager = new();
        IDomainEventFactory<IDomainEvent> factory = new DomainEventFactory();
        AggregateRepositoryManager aggregateRepositoryManager = new(eventStoreContextManager.Object, factory);
        IAggregateRepository<TestAggregate, DomainEvent> r = aggregateRepositoryManager.GetAggregateRepository<TestAggregate, DomainEvent>(Guid.NewGuid());
        r.ShouldNotBeNull();

    }
}