namespace Shared.EventStore.Tests.TestObjects;

using System;
using Aggregate;
using DomainDrivenDesign.EventSourcing;

public record TestAggregate : Aggregate
{
    public string AggregateName { get; private set; }

    public static TestAggregate Create(Guid aggregateId)
    {
        return new TestAggregate(aggregateId);
    }

    public TestAggregate()
    {

    }

    private TestAggregate(Guid aggregateId)
    {
        AggregateId = aggregateId;
    }

    protected override object GetMetadata()
    {
        return new object();
    }

    public override void PlayEvent(IDomainEvent domainEvent)
    {
        PlayEvent((dynamic)domainEvent);
    }

    private void PlayEvent(AggregateNameSetEvent domainEvent)
    {
        if (domainEvent.AggregateName == "Error")
            throw new Exception("Error Aggregate");
        AggregateName = domainEvent.AggregateName;
    }

    public void SetAggregateName(string aggregateName, Guid eventId)
    {
        AggregateNameSetEvent aggregateNameSetEvent = new(AggregateId, eventId, aggregateName);

        ApplyAndAppend(aggregateNameSetEvent);
    }

    public void ApplyAndAppendNullEvent()
    {
        AggregateNameSetEvent aggregateNameSetEvent = null;

        ApplyAndAppend(aggregateNameSetEvent);
    }
}