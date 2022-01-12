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
    /// <seealso cref="Shared.EventStore.Aggregate.IDomainEventFactory&lt;Shared.DomainDrivenDesign.EventSourcing.DomainEvent&gt;" />
    public class DomainEventFactory : IDomainEventFactory<DomainEvent>
    {
        #region Fields

        /// <summary>
        /// The json serializer
        /// </summary>
        private readonly JsonSerializer JsonSerializer;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="DomainEventFactory"/> class.
        /// </summary>
        public DomainEventFactory()
        {
            JsonIgnoreAttributeIgnorerContractResolver jsonIgnoreAttributeIgnorerContractResolver = new JsonIgnoreAttributeIgnorerContractResolver();
            this.JsonSerializer = new JsonSerializer
                                  {
                                      ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                                      TypeNameHandling = TypeNameHandling.All,
                                      Formatting = Formatting.Indented,
                                      DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                                      ContractResolver = jsonIgnoreAttributeIgnorerContractResolver
                                  };
        }

        #endregion

        #region Methods

        /// <summary>
        /// Creates the domain event.
        /// </summary>
        /// <param name="aggregateId">The aggregate identifier.</param>
        /// <param name="event">The event.</param>
        /// <returns></returns>
        /// <exception cref="System.Exception">Failed to find a domain event with type {@event.Event.EventType}</exception>
        public DomainEvent CreateDomainEvent(Guid aggregateId,
                                             ResolvedEvent @event)
        {
            String json = @event.GetResolvedEventDataAsString();
            DomainEvent domainEvent;
            JObject jObject = JObject.Parse(json);

            try
            {
                if (json.Contains("$type"))
                {
                    //Handle $type (legacy) and new approach
                    domainEvent = jObject.ToObject<DomainEvent>(this.JsonSerializer);
                }
                else
                {
                    var eventType = TypeMap.GetType(@event.Event.EventType);

                    if (eventType == null)
                        throw new Exception($"Failed to find a domain event with type {@event.Event.EventType}");

                    jObject.Add("AggregateId", aggregateId);
                    jObject.Add("AggregateVersion", @event.Event.EventNumber.ToInt64());
                    jObject.Add("EventNumber", @event.Event.EventNumber.ToInt64());
                    jObject.Add("EventType", @event.Event.EventType);
                    jObject.Add("EventId", @event.Event.EventId.ToGuid());
                    jObject.Add("EventTimestamp", @event.Event.Created);

                    domainEvent = (DomainEvent)jObject.ToObject(eventType, this.JsonSerializer);
                }
            }
            catch(Exception e)
            {
                Exception ex = new($"Failed to convert json event {json} into a domain event. EventType was {@event.Event.EventType}", e);
                throw ex;
            }

            return domainEvent;
        }

        /// <summary>
        /// Creates the domain event.
        /// </summary>
        /// <param name="json">The json.</param>
        /// <param name="eventType">Type of the event.</param>
        /// <returns></returns>
        public DomainEvent CreateDomainEvent(String json,
                                             Type eventType)
        {
            DomainEvent domainEvent;
            JObject jObject = JObject.Parse(json);

            try
            {
                if (json.Contains("$type"))
                {
                    //Handle $type (legacy) and new approach
                    domainEvent = jObject.ToObject<DomainEvent>(this.JsonSerializer);
                }
                else
                {
                    jObject.Add("EventType", eventType.Name);

                    domainEvent = (DomainEvent)jObject.ToObject(eventType, this.JsonSerializer);
                }
            }
            catch(Exception e)
            {
                Exception ex = new($"Failed to convert json event {json} into a domain event. EventType was {eventType.Name}", e);
                throw ex;
            }

            return domainEvent;
        }

        /// <summary>
        /// Creates the domain event.
        /// </summary>
        /// <param name="aggregateId">The aggregate identifier.</param>
        /// <param name="event">The event.</param>
        /// <returns></returns>
        public DomainEvent[] CreateDomainEvents(Guid aggregateId,
                                                IList<ResolvedEvent> @event)
        {
            return @event.Select(e => this.CreateDomainEvent(aggregateId, e)).ToArray();
        }

        #endregion
    }
}