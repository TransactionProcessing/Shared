namespace Shared.EventStore.Aggregate
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using DomainDrivenDesign.EventSourcing;
    using General;
    using global::EventStore.Client;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Serialisation;

    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="Shared.EventStore.Aggregate.IDomainEventFactory&lt;Shared.DomainDrivenDesign.EventSourcing.DomainEventRecord.DomainEvent&gt;" />
    public class DomainEventRecordFactory : IDomainEventFactory<DomainEventRecord.DomainEvent>
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="DomainEventFactory2" /> class.
        /// </summary>
        public DomainEventRecordFactory()
        {
            JsonIgnoreAttributeIgnorerContractResolver jsonIgnoreAttributeIgnorerContractResolver = new JsonIgnoreAttributeIgnorerContractResolver();

            JsonConvert.DefaultSettings = () =>
                                          {
                                              return new JsonSerializerSettings
                                                     {
                                                         ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                                                         TypeNameHandling = TypeNameHandling.All,
                                                         Formatting = Formatting.Indented,
                                                         DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                                                         ContractResolver = jsonIgnoreAttributeIgnorerContractResolver
                                                     };
                                          };
        }

        #endregion

        #region Methods

        /// <summary>
        /// Creates the agge aggregate snapshot.
        /// </summary>
        /// <param name="aggregateId"></param>
        /// <param name="event">The event.</param>
        /// <returns></returns>
        /// <exception cref="System.Exception">Failed to find a domain event with type {@event.Event.EventType}</exception>
        public DomainEventRecord.DomainEvent CreateDomainEvent(Guid aggregateId,
                                                               ResolvedEvent @event)
        {
            String json = @event.GetResolvedEventDataAsString();

            Type eventType = TypeMap.GetType(@event.Event.EventType);

            if (eventType == null)
                throw new Exception($"Failed to find a domain event with type {@event.Event.EventType}");

            DomainEventRecord.DomainEvent domainEvent = (DomainEventRecord.DomainEvent)JsonConvert.DeserializeObject(json, eventType);

            domainEvent = domainEvent with
                          {
                              AggregateId = aggregateId,
                              AggregateVersion = @event.Event.EventNumber.ToInt64(),
                              EventNumber = @event.Event.EventNumber.ToInt64(),
                              EventType = @event.Event.EventType,
                              EventId = @event.Event.EventId.ToGuid(),
                              EventTimestamp = @event.Event.Created,
                          };

            return domainEvent;
        }

        /// <summary>
        /// Creates the domain event.
        /// </summary>
        /// <param name="json">The json.</param>
        /// <param name="eventType">Type of the event.</param>
        /// <returns></returns>
        public DomainEventRecord.DomainEvent CreateDomainEvent(String json,
                                                               Type eventType)
        {
            DomainEventRecord.DomainEvent domainEvent;
            JObject jObject = JObject.Parse(json);

            try
            {
                domainEvent = (DomainEventRecord.DomainEvent)JsonConvert.DeserializeObject(json, eventType);
            }
            catch(Exception e)
            {
                Exception ex = new($"Failed to convert json event {json} into a domain event. EventType was {eventType.Name}", e);
                throw ex;
            }

            return domainEvent;
        }

        /// <summary>
        /// Creates the domain events.
        /// </summary>
        /// <param name="aggregateId"></param>
        /// <param name="event">The event.</param>
        /// <returns></returns>
        public DomainEventRecord.DomainEvent[] CreateDomainEvents(Guid aggregateId,
                                                                  IList<ResolvedEvent> @event)
        {
            return @event.Select(e => this.CreateDomainEvent(aggregateId, e)).ToArray();
        }

        #endregion

        //public EventStoreMetadata GetEventStoreMetadata(ResolvedEvent @event)
        //{
        //    return this.JsonSerialiser.Deserialise<EventStoreMetadata>(Encoding.Default.GetString(@event.Event.Metadata, 0, @event.Event.Metadata.Length)) ?? new EventStoreMetadata();
        //}
    }
}