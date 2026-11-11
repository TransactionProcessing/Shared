using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using KurrentDB.Client;
using TransactionProcessing.Shared.DomainDrivenDesign.EventSourcing;
using TransactionProcessing.Shared.EventStore.Aggregate;
using TransactionProcessing.Shared.EventStore.Tests.TestObjects;
using TransactionProcessing.Shared.Logger.TennantContext;
using TransactionProcessing.Shared.Serialisation;
using Shouldly;
using Xunit;

namespace TransactionProcessing.Shared.EventStore.Tests;

public class EventDataFactoryTests{
    public EventDataFactoryTests() {
        StringSerialiser.Initialise(new SystemTextJsonSerializer(new JsonSerializerOptions()));
    }

    [Fact]
    public void EventDataFactory_CreateEventData_EventDataCreated(){
        EventDataFactory factory = new();
        AggregateNameSetEvent aggregateNameSetEvent = new(TestData.AggregateId, TestData.EventId, "Test");
        EventData eventData = factory.CreateEventData(aggregateNameSetEvent);
        eventData.EventId.ToGuid().ShouldBe(aggregateNameSetEvent.EventId);
        eventData.Metadata.IsEmpty.ShouldBeTrue();
    }

    [Fact]
    public void EventDataFactory_CreateEventData_CorrelationIdIncludedInMetadata()
    {
        EventDataFactory factory = new();
        AggregateNameSetEvent domainEvent = new(TestData.AggregateId, TestData.EventId, "Test");
        Guid correlationId = Guid.NewGuid();

        EventData eventData = factory.CreateEventData(domainEvent, correlationId.ToString("D"));

        eventData.Metadata.IsEmpty.ShouldBeFalse();
        using JsonDocument metadata = JsonDocument.Parse(eventData.Metadata);
        metadata.RootElement.GetProperty(TenantContext.KeyNameCorrelationId).GetString().ShouldBe(correlationId.ToString("D"));
    }

    [Fact]
    public void EventDataFactory_CreateEventData_NullEvent_ErrorThrown()
    {
        EventDataFactory factory = new();
            
        Should.Throw<ArgumentNullException>(() => factory.CreateEventData(null));
    }

    [Fact]
    public void EventDataFactory_CreateEventDataList_EventDataCreated()
    {
        EventDataFactory factory = new();
        List<IDomainEvent> events = new();
        AggregateNameSetEvent aggregateNameSetEvent1 = new(TestData.AggregateId, Guid.NewGuid(), "Test");
        AggregateNameSetEvent aggregateNameSetEvent2 = new(TestData.AggregateId, Guid.NewGuid(), "Test");
        events.Add(aggregateNameSetEvent1);
        events.Add(aggregateNameSetEvent2);
            
        EventData[] eventData = factory.CreateEventDataList(events);
        eventData[0].EventId.ToGuid().ShouldBe(aggregateNameSetEvent1.EventId);
        eventData[1].EventId.ToGuid().ShouldBe(aggregateNameSetEvent2.EventId);
    }

    [Fact]
    public void EventDataFactory_CreateEventData_InterfaceReference_IncludesDerivedProperties()
    {
        EventDataFactory factory = new();
        IDomainEvent domainEvent = new AggregateNameSetEvent(TestData.AggregateId, Guid.NewGuid(), "Test");

        EventData eventData = factory.CreateEventData(domainEvent);
        string payload = Encoding.Default.GetString(eventData.Data.ToArray());

        payload.ShouldContain("\"aggregateName\": \"Test\"");
    }
}
