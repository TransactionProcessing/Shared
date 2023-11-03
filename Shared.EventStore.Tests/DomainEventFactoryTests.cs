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
            ((AggregateNameSetEvent)newEvent).AggregateName.ShouldBe(aggregateNameSetEvent.AggregateName);
        }

        [Fact]
        public void DomainEventFactory_CreateDomainEvent_GuidAndResolvedEvent_DomainEventCreated()
        {
            AggregateNameSetEvent aggregateNameSetEvent = new AggregateNameSetEvent(TestData.AggregateId, TestData.EventId, "Test");
            ResolvedEvent resolvedEvent = new ResolvedEvent(TestData.CreateEventRecord<AggregateNameSetEvent>(aggregateNameSetEvent, "TestStream"),null,null);

            DomainEventFactory factory = new DomainEventFactory();
            DomainEvent newEvent = factory.CreateDomainEvent(TestData.AggregateId, resolvedEvent);
            ((AggregateNameSetEvent)newEvent).AggregateName.ShouldBe(aggregateNameSetEvent.AggregateName);
        }

        [Fact]
        public void DomainEventFactory_CreateDomainEvents_GuidAndResolvedEventList_DomainEventCreated()
        {
            AggregateNameSetEvent aggregateNameSetEvent = new AggregateNameSetEvent(TestData.AggregateId, TestData.EventId, "Test");
            List<ResolvedEvent> resolvedEventList = new List<ResolvedEvent>{
                                                                               new ResolvedEvent(TestData.CreateEventRecord<AggregateNameSetEvent>(aggregateNameSetEvent, "TestStream"), null, null)
                                                                           };

            DomainEventFactory factory = new DomainEventFactory();
            DomainEvent[] newEvent = factory.CreateDomainEvents(TestData.AggregateId, resolvedEventList);
            ((AggregateNameSetEvent)newEvent.Single()).AggregateName.ShouldBe(aggregateNameSetEvent.AggregateName);
        }
    }

    public class EventDataFactoryTests{
        [Fact]
        public void EventDataFactory_CreateEventData_EventDataCreated(){
            EventDataFactory factory = new EventDataFactory();
            AggregateNameSetEvent aggregateNameSetEvent = new AggregateNameSetEvent(TestData.AggregateId, TestData.EventId, "Test");
            EventData eventData = factory.CreateEventData(aggregateNameSetEvent);
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
            
            EventData[] eventData = factory.CreateEventDataList(events);
            eventData[0].EventId.ToGuid().ShouldBe(aggregateNameSetEvent1.EventId);
            eventData[1].EventId.ToGuid().ShouldBe(aggregateNameSetEvent2.EventId);
        }
    }
}
