using System;
using System.Collections.Generic;
using System.Text;

namespace Driver
{
    using Newtonsoft.Json;
    using Shared.DomainDrivenDesign.EventSourcing;
    using Shared.EventStore.Aggregate;
    using Shared.EventStore.EventStore;

    public class TestAggregate : Aggregate
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
            return null;
        }

        public override void PlayEvent(IDomainEvent domainEvent)
        {
            this.PlayEvent((dynamic)domainEvent);
        }
        
        public Object GetAggregateMetadata()
        {
            return null;
        }

        private void PlayEvent(AggregateNameSetEvent domainEvent)
        {
            this.AggregateName = domainEvent.AggregateName;
        }

        public void SetAggregateName(String aggregateName)
        {
            AggregateNameSetEvent aggregateNameSetEvent = AggregateNameSetEvent.Create(this.AggregateId, aggregateName);

            this.ApplyAndAppend(aggregateNameSetEvent);
        }
    }

    public class AggregateNameSetEvent : DomainEvent
    {
        [JsonProperty]
        public String AggregateName { get; private set; }

        public AggregateNameSetEvent()
        {
            //We need this for serialisation, so just embrace the DDD crime
        }

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
