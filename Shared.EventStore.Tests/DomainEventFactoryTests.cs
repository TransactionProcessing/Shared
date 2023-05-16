using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.EventStore.Tests
{
    using Aggregate;
    using DomainDrivenDesign.EventSourcing;
    using global::EventStore.Client;
    using Newtonsoft.Json;
    using Shouldly;
    using Xunit;

    public class DomainEventFactoryTests
    {
        [Fact]
        public void DomainEventFactory_CreateDomainEvent_StringAndType_DomainEventCreated(){
            AggregateNameSetEvent aggregateNameSetEvent = new AggregateNameSetEvent(TestData.AggregateId, TestData.EventId, "Test");
            String eventData = JsonConvert.SerializeObject(aggregateNameSetEvent);
            DomainEventFactory factory = new DomainEventFactory();
            DomainEvent newEvent = factory.CreateDomainEvent(eventData, typeof(AggregateNameSetEvent));
            newEvent.AggregateId.ShouldBe(aggregateNameSetEvent.AggregateId);
            newEvent.EventId.ShouldBe(aggregateNameSetEvent.EventId);
            ((AggregateNameSetEvent)newEvent).AggregateName.ShouldBe(aggregateNameSetEvent.AggregateName);
        }

        [Fact]
        public void DomainEventFactory_CreateDomainEvent_GuidAndResolvedEvent_DomainEventCreated()
        {
            AggregateNameSetEvent aggregateNameSetEvent = new AggregateNameSetEvent(TestData.AggregateId, TestData.EventId, "Test");
            String eventData = JsonConvert.SerializeObject(aggregateNameSetEvent);
            ResolvedEvent resolvedEvent = new ResolvedEvent(TestData.CreateEventRecord<AggregateNameSetEvent>(aggregateNameSetEvent, "TestStream"),null,null);

            DomainEventFactory factory = new DomainEventFactory();
            var newEvent = factory.CreateDomainEvent(TestData.AggregateId, resolvedEvent);
            newEvent.AggregateId.ShouldBe(aggregateNameSetEvent.AggregateId);
            newEvent.EventId.ShouldBe(aggregateNameSetEvent.EventId);
            ((AggregateNameSetEvent)newEvent).AggregateName.ShouldBe(aggregateNameSetEvent.AggregateName);
        }

        [Fact]
        public void DomainEventFactory_CreateDomainEvents_GuidAndResolvedEventList_DomainEventCreated()
        {
            AggregateNameSetEvent aggregateNameSetEvent = new AggregateNameSetEvent(TestData.AggregateId, TestData.EventId, "Test");
            String eventData = JsonConvert.SerializeObject(aggregateNameSetEvent);
            List<ResolvedEvent> resolvedEventList = new List<ResolvedEvent>();
            resolvedEventList.Add(new ResolvedEvent(TestData.CreateEventRecord<AggregateNameSetEvent>(aggregateNameSetEvent, "TestStream"), null, null));

            DomainEventFactory factory = new DomainEventFactory();
            var newEvent = factory.CreateDomainEvents(TestData.AggregateId, resolvedEventList);
            newEvent.Single().AggregateId.ShouldBe(aggregateNameSetEvent.AggregateId);
            newEvent.Single().EventId.ShouldBe(aggregateNameSetEvent.EventId);
            ((AggregateNameSetEvent)newEvent.Single()).AggregateName.ShouldBe(aggregateNameSetEvent.AggregateName);
        }
    }

    public class EventDataFactoryTests{
        [Fact]
        public void EventDataFactory_CreateEventData_EventDataCreated(){
            EventDataFactory factory = new EventDataFactory();
            AggregateNameSetEvent aggregateNameSetEvent = new AggregateNameSetEvent(TestData.AggregateId, TestData.EventId, "Test");
            var eventData = factory.CreateEventData(aggregateNameSetEvent);
            eventData.EventId.ToGuid().ShouldBe(aggregateNameSetEvent.EventId);
        }

        [Fact]
        public void EventDataFactory_CreateEventData_NullEvent_ErrorThrown()
        {
            EventDataFactory factory = new EventDataFactory();
            
            Should.Throw<ArgumentNullException>(() => {
                                                    factory.CreateEventData(null);
                                                });
        }

        [Fact]
        public void EventDataFactory_CreateEventDataList_EventDataCreated()
        {
            EventDataFactory factory = new EventDataFactory();
            List<IDomainEvent> events = new List<IDomainEvent>();
            AggregateNameSetEvent aggregateNameSetEvent1 = new AggregateNameSetEvent(TestData.AggregateId, Guid.NewGuid(), "Test");
            AggregateNameSetEvent aggregateNameSetEvent2 = new AggregateNameSetEvent(TestData.AggregateId, Guid.NewGuid(), "Test");
            events.Add(aggregateNameSetEvent1);
            events.Add(aggregateNameSetEvent2);
            
            var eventData = factory.CreateEventDataList(events);
            eventData[0].EventId.ToGuid().ShouldBe(aggregateNameSetEvent1.EventId);
            eventData[1].EventId.ToGuid().ShouldBe(aggregateNameSetEvent2.EventId);
        }
    }
}
