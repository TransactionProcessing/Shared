using System;
using System.Collections.Generic;
using System.Linq;

namespace Shared.EventStore.Tests
{
    using Aggregate;
    using DomainDrivenDesign.EventSourcing;
    using Shared.EventStore.EventHandling;
    using Shouldly;
    using Xunit;

    public class DomainEventHandlerResolverTests
    {
        [Fact]
        public void DomainEventHandlerResolver_GetDomainEventHandlers_HandlerResoved(){
            Dictionary<String, String[]> eventHandlerConfiguration= new Dictionary<String, String[]>();
            Func<Type, IDomainEventHandler> createEventHandlerFunc = (t) => {
                                                                         return new TestDomainEventHandler();
                                                                     };

            List<String> handlers = new List<String>();
            handlers.Add("Shared.EventStore.Tests.TestDomainEventHandler, Shared.EventStore.Tests");
            eventHandlerConfiguration.Add("EstateCreatedEvent", handlers.ToArray());

            DomainEventHandlerResolver r = new DomainEventHandlerResolver(eventHandlerConfiguration, createEventHandlerFunc);
            List<IDomainEventHandler> result  = r.GetDomainEventHandlers(new EstateCreatedEvent(TestData.AggregateId, TestData.EstateName));
            result.Count.ShouldBe(1);
            result.Single().ShouldBeOfType(typeof(TestDomainEventHandler));
        }

        [Fact]
        public void DomainEventHandlerResolver_EventNotConfigured_NullReturned()
        {
            Dictionary<String, String[]> eventHandlerConfiguration = new Dictionary<String, String[]>();
            Func<Type, IDomainEventHandler> createEventHandlerFunc = (t) => {
                                                                         return new TestDomainEventHandler();
                                                                     };

            List<String> handlers = new List<String>();

            DomainEventHandlerResolver r = new DomainEventHandlerResolver(eventHandlerConfiguration, createEventHandlerFunc);
            List<IDomainEventHandler> result = r.GetDomainEventHandlers(new EstateCreatedEvent(TestData.AggregateId, TestData.EstateName));
            result.ShouldBeNull();
        }

        [Fact]
        public void DomainEventHandlerResolver_GetDomainEventHandlers_NoHandlerFound_ErrorThrown()
        {
            Dictionary<String, String[]> eventHandlerConfiguration = new Dictionary<String, String[]>();
            Func<Type, IDomainEventHandler> createEventHandlerFunc = (t) => {
                                                                         return new TestDomainEventHandler();
                                                                     };

            List<String> handlers = new List<String>();
            handlers.Add("Shared.EventStore.Tests.TestDomainEventHandler1, Shared.EventStore.Tests");
            eventHandlerConfiguration.Add("EstateCreatedEvent", handlers.ToArray());

            Should.Throw<NotSupportedException>(() => {
                                                    DomainEventHandlerResolver r = new DomainEventHandlerResolver(eventHandlerConfiguration, createEventHandlerFunc);
                                                });
        }
    }

    public record TestAggregate : Aggregate
    {
        public String AggregateName { get; private set; }

        public static TestAggregate Create(Guid aggregateId)
        {
            return new TestAggregate(aggregateId);
        }

        public TestAggregate()
        {

        }

        private TestAggregate(Guid aggregateId)
        {
            this.AggregateId = aggregateId;
        }

        protected override Object GetMetadata()
        {
            return new Object();
        }

        public override void PlayEvent(IDomainEvent domainEvent)
        {
            this.PlayEvent((dynamic)domainEvent);
        }

        private void PlayEvent(AggregateNameSetEvent domainEvent)
        {
            if (domainEvent.AggregateName == "Error")
                throw new Exception("Error Aggregate");
            this.AggregateName = domainEvent.AggregateName;
        }

        public void SetAggregateName(String aggregateName)
        {
            AggregateNameSetEvent aggregateNameSetEvent = new AggregateNameSetEvent(this.AggregateId,TestData.EventId, aggregateName);

            this.ApplyAndAppend(aggregateNameSetEvent);
        }

        public void ApplyAndAppendNullEvent(){
            AggregateNameSetEvent aggregateNameSetEvent = null;

            this.ApplyAndAppend(aggregateNameSetEvent);
        }
    }

    public record AggregateNameSetEvent : DomainEvent
    {
        public String AggregateName { get; init; }

        public AggregateNameSetEvent(Guid aggregateId,
                                      Guid eventId,
                                      String aggregateName) : base(aggregateId, eventId)
        {
            this.AggregateName = aggregateName;
        }
    }
}
