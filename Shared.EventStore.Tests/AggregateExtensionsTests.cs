using Shared.EventStore.Tests.TestObjects;

namespace Shared.EventStore.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using Aggregate;
using DomainDrivenDesign.EventSourcing;
using Shouldly;
using Xunit;

public class AggregateExtensionsTests{
    [Fact]
    public void AggregateExtensions_Apply_EventIsApplied()
    {
        TestAggregate t = TestAggregate.Create(TestData.AggregateId);
        AggregateNameSetEvent aggregateNameSetEvent = new AggregateNameSetEvent(TestData.AggregateId, TestData.EventId, "Test");
        t.Apply(aggregateNameSetEvent);
        t.AggregateName.ShouldBe("Test");
    }

    [Fact]
    public void AggregateExtensions_Apply_DuplicateEvent_EventIsSilentlyHandled()
    {
        TestAggregate t = TestAggregate.Create(TestData.AggregateId);
        AggregateNameSetEvent aggregateNameSetEvent = new AggregateNameSetEvent(TestData.AggregateId, TestData.EventId, "Test");
        t.Apply(aggregateNameSetEvent);
        t.AggregateName.ShouldBe("Test");
        t.Apply(aggregateNameSetEvent);
    }

    [Fact]
    public void AggregateExtensions_GetHistoricalEvents_EventsReturned()
    {
        TestAggregate t = TestAggregate.Create(TestData.AggregateId);
        AggregateNameSetEvent aggregateNameSetEvent = new AggregateNameSetEvent(TestData.AggregateId, TestData.EventId, "Test");
        t.Apply(aggregateNameSetEvent);
        t.AggregateName.ShouldBe("Test");
        IList<IDomainEvent> events = t.GetHistoricalEvents();
        events.Count.ShouldBe(1);
        events.Single().EventId.ShouldBe(TestData.EventId);
    }

    [Fact]
    public void AggregateExtensions_GetPendingEvents_EventsReturned()
    {
        TestAggregate t = TestAggregate.Create(TestData.AggregateId);
        t.SetAggregateName("Test", Guid.NewGuid());
        IList<IDomainEvent> events = t.GetPendingEvents();
        events.Count.ShouldBe(1);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AggregateExtensions_IsEventDuplicate_EventsIsADuplicate(Boolean commitPending)
    {
        TestAggregate t = TestAggregate.Create(TestData.AggregateId);
        t.SetAggregateName("Test", TestData.EventId);
        if (commitPending) { t.CommitPendingEvents(); }
        var eventIsDuplicate = t.IsEventDuplicate(TestData.EventId);
        eventIsDuplicate.ShouldBeTrue();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AggregateExtensions_IsEventDuplicate_EventsIsNotADuplicate(Boolean commitPending)
    {
        TestAggregate t = TestAggregate.Create(TestData.AggregateId);
        t.SetAggregateName("Test", Guid.NewGuid());
        if (commitPending) { t.CommitPendingEvents(); }
        var eventIsDuplicate = t.IsEventDuplicate(Guid.NewGuid());
        eventIsDuplicate.ShouldBeFalse();
    }



    [Fact]
    public void AggregateExtensions_GetPendingEvents_EventIsApplied()
    {
        TestAggregate t = TestAggregate.Create(TestData.AggregateId);
        t.CommitPendingEvents();
        IList<IDomainEvent> events = t.GetPendingEvents();
        events.Count.ShouldBe(0);
    }
}