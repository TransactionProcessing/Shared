using KurrentDB.Client;
using Shared.EventStore.Aggregate;
using Shared.EventStore.Tests.TestObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.EventStore.Tests;

using DomainDrivenDesign.EventSourcing;
using General;
using global::EventStore.Client;
using Shared.Serialisation;
using Shouldly;
using System.Reflection;
using System.Text.Json;
using System.Xml.Serialization;
using Xunit;

public class TypeMapConvertorTests
{
    public TypeMapConvertorTests() {
        StringSerialiser.Initialise(new SystemTextJsonSerializer(new JsonSerializerOptions()));
    }

    [Fact]
    public void TypeMapConvertor_Convertor_IDomainEvent_EventDataReturned(){
        AggregateNameSetEvent domainEvent  = new(TestData.AggregateId, TestData.EventId, TestData.EstateName);
        EventData result = TypeMapConvertor.Convertor(domainEvent);
        result.ShouldNotBeNull();
    }

    [Fact]
    public void TypeMapConvertor_Convertor_ResolvedEvent_EventDataReturned()
    {
        AggregateNameSetEvent domainEvent = new(TestData.AggregateId, TestData.EventId, TestData.EstateName);
        ResolvedEvent resolvedEvent = new(TestData.CreateEventRecord<AggregateNameSetEvent>(domainEvent, "TestStream"), null, null);

        IDomainEvent result = TypeMapConvertor.Convertor(TestData.AggregateId, resolvedEvent);
        result.ShouldNotBeNull();
    }

    [Fact]
    public void TypeMapConvertor_Convertor_ResolvedEvent_UnknownEvent_EventDataReturned()
    {
        UnknownEvent domainEvent = new(TestData.AggregateId, TestData.EventId, TestData.EstateName);
        ResolvedEvent resolvedEvent = new(TestData.CreateEventRecord<UnknownEvent>(domainEvent, "TestStream", false), null, null);

        Should.Throw<Exception>(() => TypeMapConvertor.Convertor(TestData.AggregateId, resolvedEvent));
    }
}

public class TypeProviderTests{
        
    [Fact]
    public void TypeProvider_LoadDomainEventsTypeDynamically(){
        Assembly assem = Assembly.GetAssembly(typeof(AggregateNameSetEventTest));
        TypeProvider.LoadDomainEventsTypeDynamically(new List<Assembly>{assem}.ToArray());
        var t = TypeMap.GetType("AggregateNameSetEventTest");
        t.ShouldNotBeNull();
    }
}

public record AggregateNameSetEventTest : DomainEvent
{
    public String AggregateName { get; init; }

    public AggregateNameSetEventTest(Guid aggregateId,
                                     Guid eventId,
                                     String aggregateName) : base(aggregateId, eventId)
    {
        this.AggregateName = aggregateName;
    }
}