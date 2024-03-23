using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.EventStore.Tests
{
    using System.Xml.Serialization;
    using DomainDrivenDesign.EventSourcing;
    using global::EventStore.Client;
    using Shared.EventStore.Aggregate;
    using Shared.EventStore.Tests.TestObjects;
    using Shouldly;
    using Xunit;

    public class TypeMapConvertorTests
    {
        [Fact]
        public void TypeMapConvertor_Convertor_IDomainEvent_EventDataReturned(){
            AggregateNameSetEvent domainEvent  = new AggregateNameSetEvent(TestData.AggregateId, TestData.EventId, TestData.EstateName);
            EventData result = TypeMapConvertor.Convertor(domainEvent);
            result.ShouldNotBeNull();
        }

        [Fact]
        public void TypeMapConvertor_Convertor_ResolvedEvent_EventDataReturned()
        {
            AggregateNameSetEvent domainEvent = new AggregateNameSetEvent(TestData.AggregateId, TestData.EventId, TestData.EstateName);
            ResolvedEvent resolvedEvent = new ResolvedEvent(TestData.CreateEventRecord<AggregateNameSetEvent>(domainEvent, "TestStream"), null, null);

            IDomainEvent result = TypeMapConvertor.Convertor(TestData.AggregateId, resolvedEvent);
            result.ShouldNotBeNull();
        }

        [Fact]
        public void TypeMapConvertor_Convertor_ResolvedEvent_UnknownEvent_EventDataReturned()
        {
            UnknownEvent domainEvent = new UnknownEvent(TestData.AggregateId, TestData.EventId, TestData.EstateName);
            ResolvedEvent resolvedEvent = new ResolvedEvent(TestData.CreateEventRecord<UnknownEvent>(domainEvent, "TestStream", false), null, null);

            Should.Throw<Exception>(() => {
                                        TypeMapConvertor.Convertor(TestData.AggregateId, resolvedEvent);
                                    });
        }
    }

    public class TypeProviderTests{
        [Fact]
        public void TypeProvider_LoadDomainEventsTypeDynamically_NoFilters(){
            TypeProvider.LoadDomainEventsTypeDynamically();
        }

        [Fact]
        public void TypeProvider_LoadDomainEventsTypeDynamically_WithFilters(){
            AggregateNameSetEventTest domainEvent = new AggregateNameSetEventTest(TestData.AggregateId, TestData.EventId, TestData.EstateName);
            List<String> assemblyFilters = new List<String>();
            assemblyFilters.Add("Test");
            TypeProvider.LoadDomainEventsTypeDynamically(assemblyFilters);
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
}
