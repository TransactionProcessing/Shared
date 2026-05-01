using KurrentDB.Client;
using Shared.EventStore.Tests.TestObjects;
using Shared.Serialisation;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Shared.EventStore.Tests;

using Aggregate;
using DomainDrivenDesign.EventSourcing;
using Shouldly;
using System.Text.Json;
using Xunit;

public class DomainEventFactoryTests
{
    public DomainEventFactoryTests() {
        StringSerialiser.Initialise(new SystemTextJsonSerializer(new JsonSerializerOptions()));
    }

    [Fact]
    public void DomainEventFactory_CreateDomainEvent_StringAndType_DomainEventCreated(){
        AggregateNameSetEvent aggregateNameSetEvent = new(TestData.AggregateId, TestData.EventId, "Test");
        String eventData = StringSerialiser.Serialise(aggregateNameSetEvent, new SerialiserOptions(SerialiserPropertyFormat.CamelCase, IgnoreNullValues: true, WriteIndented: true));
        DomainEventFactory factory = new();
        DomainEvent newEvent = factory.CreateDomainEvent(eventData, typeof(AggregateNameSetEvent));
        ((AggregateNameSetEvent)newEvent).AggregateName.ShouldBe(aggregateNameSetEvent.AggregateName);
    }

    [Fact]
    public void DomainEventFactory_CreateDomainEvent_StringAndType_InvalidJson_ExceptionThrown()
    {
        AggregateNameSetEvent aggregateNameSetEvent = new(TestData.AggregateId, TestData.EventId, "Test");
        String eventData = StringSerialiser.Serialise(aggregateNameSetEvent);
        eventData = eventData.Replace(":", "");
        DomainEventFactory factory = new();
        Should.Throw<Exception>(() => factory.CreateDomainEvent(eventData, typeof(AggregateNameSetEvent)));
    }

    [Fact]
    public void DomainEventFactory_CreateDomainEvent_GuidAndResolvedEvent_DomainEventCreated()
    {
        AggregateNameSetEvent aggregateNameSetEvent = new(TestData.AggregateId, TestData.EventId, "Test");
        ResolvedEvent resolvedEvent = new(TestData.CreateEventRecord<AggregateNameSetEvent>(aggregateNameSetEvent, "TestStream"), null, null);

        DomainEventFactory factory = new();
        DomainEvent newEvent = factory.CreateDomainEvent(TestData.AggregateId, resolvedEvent);
        ((AggregateNameSetEvent)newEvent).AggregateName.ShouldBe(aggregateNameSetEvent.AggregateName);
    }

    [Fact]
    public void DomainEventFactory_CreateDomainEvent_GuidAndResolvedEvent_UnknownEventType_ExceptionThrown()
    {
        UnknownEvent unknownEvent = new(TestData.AggregateId, TestData.EventId, "Test");
        ResolvedEvent resolvedEvent = new(TestData.CreateEventRecord<UnknownEvent>(unknownEvent, "TestStream", false), null, null);
            
        DomainEventFactory factory = new();
        Should.Throw<Exception>(() => factory.CreateDomainEvent(TestData.AggregateId, resolvedEvent));
    }

    [Fact]
    public void DomainEventFactory_CreateDomainEvents_GuidAndResolvedEventList_DomainEventCreated()
    {
        AggregateNameSetEvent aggregateNameSetEvent = new(TestData.AggregateId, TestData.EventId, "Test");
        List<ResolvedEvent> resolvedEventList = new() {
            new ResolvedEvent(TestData.CreateEventRecord<AggregateNameSetEvent>(aggregateNameSetEvent, "TestStream"), null, null)
        };

        DomainEventFactory factory = new();
        DomainEvent[] newEvent = factory.CreateDomainEvents(TestData.AggregateId, resolvedEventList);
        ((AggregateNameSetEvent)newEvent.Single()).AggregateName.ShouldBe(aggregateNameSetEvent.AggregateName);
    }
}