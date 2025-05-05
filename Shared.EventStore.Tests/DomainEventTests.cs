using Shared.EventStore.Tests.TestObjects;

namespace Shared.EventStore.Tests;

using System;
using DomainDrivenDesign.EventSourcing;
using Shouldly;
using Xunit;

public class DomainEventTests{
    [Fact]
    public void DomainEvent_CanBeCreated_IsCreated(){
        DomainEvent d = new DomainEvent(TestData.AggregateId, TestData.EventId);
        d.AggregateId.ShouldBe(TestData.AggregateId);
        d.EventId.ShouldBe(TestData.EventId);
        d.EventType.ShouldBe("DomainEvent");
        d.EventNumber.ShouldBe(0);
        d.EventTimestamp.ShouldBe(DateTimeOffset.MinValue);
        d.AggregateVersion.ShouldBe(0);
    }
}