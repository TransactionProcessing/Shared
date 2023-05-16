using System;
using System.Collections.Generic;
using System.Text;

namespace Driver
{
    using Newtonsoft.Json;
    using Shared.DomainDrivenDesign.EventSourcing;
    using Shared.EventStore.Aggregate;
    using Shared.EventStore.EventStore;

    public class TestAggregate1 : Aggregate
    {
        public String AggregateName { get; private set; }

        public static TestAggregate1 Create(Guid aggregateId)
        {
            return new TestAggregate1(aggregateId);
        }

        public TestAggregate1()
        {

        }

        private TestAggregate1(Guid aggregateId)
        {
            this.AggregateId = aggregateId;
        }

        protected override Object GetMetadata()
        {
            return null;
        }

        public override void PlayEvent(IDomainEvent domainEvent)
        {
            this.PlayEvent((dynamic)domainEvent);
        }
        
        private void PlayEvent(AggregateNameSetEvent domainEvent){
            if (AggregateName == "Error")
                throw new Exception("Error Aggregate");
            this.AggregateName = domainEvent.AggregateName;
        }

        public void SetAggregateName(String aggregateName)
        {
            AggregateNameSetEvent aggregateNameSetEvent = AggregateNameSetEvent.Create(this.AggregateId, aggregateName);

            this.ApplyAndAppend(aggregateNameSetEvent);
        }
    }

    public record AggregateNameSetEvent : DomainEvent
    {
        [JsonProperty]
        public String AggregateName { get; private set; }

        private AggregateNameSetEvent(Guid aggregateId,
                                      Guid eventId,
                                      String aggregateName) : base(aggregateId, eventId)
        {
            this.AggregateName = aggregateName;
        }

        public static AggregateNameSetEvent Create(Guid aggregateId,
                                                   String aggregateName)
        {
            return new AggregateNameSetEvent(aggregateId, Guid.NewGuid(), aggregateName);
        }
    }
}
