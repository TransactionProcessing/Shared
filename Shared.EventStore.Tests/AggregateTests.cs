namespace Shared.EventStore.Tests;

using System;
using Aggregate;
using Shouldly;
using Xunit;

public class AggregateTests{
    [Fact]
    public void Aggregate_CanBeCreated_IsCreated(){
        TestAggregate t = TestAggregate.Create(TestData.AggregateId);
        
        t.AggregateId.ShouldBe(TestData.AggregateId);
        t.Version.ShouldBe(AggregateVersion.CreateFrom(-1));
        t.EventHistory.Count.ShouldBe(0);
        t.PendingEvents.Count.ShouldBe(0);
    }

    [Fact]
    public void Aggregate_ApplyAndAppend_EventIsAppended()
    {
        TestAggregate t = TestAggregate.Create(TestData.AggregateId);
        t.SetAggregateName("Test");

        t.AggregateId.ShouldBe(TestData.AggregateId);
        t.Version.ShouldBe(AggregateVersion.CreateFrom(-1));
        t.EventHistory.Count.ShouldBe(0);
        t.PendingEvents.Count.ShouldBe(1);
    }

    [Fact]
    public void Aggregate_ApplyAndAppend_DuplicateEvent_EventIsAppended()
    {
        TestAggregate t = TestAggregate.Create(TestData.AggregateId);
        t.SetAggregateName("Test");
        t.SetAggregateName("Test");

        t.AggregateId.ShouldBe(TestData.AggregateId);
        t.Version.ShouldBe(AggregateVersion.CreateFrom(-1));
        t.EventHistory.Count.ShouldBe(0);
        t.PendingEvents.Count.ShouldBe(1);
    }

    [Fact]
    public void Aggregate_ApplyAndAppend_NullEvent_NoEventIsAppended()
    {
        TestAggregate t = TestAggregate.Create(TestData.AggregateId);
        t.ApplyAndAppendNullEvent();

        t.AggregateId.ShouldBe(TestData.AggregateId);
        t.Version.ShouldBe(AggregateVersion.CreateFrom(-1));
        t.EventHistory.Count.ShouldBe(0);
        t.PendingEvents.Count.ShouldBe(0);
    }

    [Fact]
    public void Aggregate_ApplyAndAppend_ErrorInApplyAndAppend_ExceptionIsThrown()
    {
        TestAggregate t = TestAggregate.Create(TestData.AggregateId);
        Should.Throw<Exception>(() => {
                                    t.SetAggregateName("Error");
                                });
    }

    [Fact]
    public void Aggregate_GetAggregateMetadata_MetadataReturned()
    {
        TestAggregate t = TestAggregate.Create(TestData.AggregateId);
        var metadata = t.GetAggregateMetadata();
        metadata.ShouldNotBeNull();
    }
}