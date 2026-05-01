using KurrentDB.Client;
using Shared.Serialisation;

namespace Shared.EventStore.Aggregate;

using System;
using System.Collections.Generic;
using System.Linq;
using DomainDrivenDesign.EventSourcing;
using General;

public class DomainEventFactory : IDomainEventFactory<DomainEvent> {

    private SerialiserOptions SerialiserOptions = new SerialiserOptions(SerialiserPropertyFormat.CamelCase, IgnoreNullValues: true, WriteIndented: true);

    public DomainEvent CreateDomainEvent(Guid aggregateId,
                                         ResolvedEvent @event) {
        String json = @event.GetResolvedEventDataAsString();
        Type eventType = null;

        try {
            eventType = TypeMap.GetType(@event.Event.EventType);
        }
        catch (Exception) {
            // ignored
        }

        if (eventType == null)
            throw new ArgumentException($"Failed to find a domain event with type {@event.Event.EventType}");

        DomainEvent domainEvent = StringSerialiser.DeserializeObject<DomainEvent>(json, eventType, SerialiserOptions);

        domainEvent = domainEvent with {
            AggregateId = aggregateId,
            AggregateVersion = @event.Event.EventNumber.ToInt64(),
            EventNumber = @event.Event.EventNumber.ToInt64(),
            EventType = @event.Event.EventType,
            EventId = @event.Event.EventId.ToGuid(),
            EventTimestamp = @event.Event.Created,
        };

        return domainEvent;
    }

    public DomainEvent CreateDomainEvent(String json,
                                         Type eventType) {
        DomainEvent domainEvent;

        try {
            domainEvent = StringSerialiser.DeserializeObject<DomainEvent>(json, eventType, SerialiserOptions);
        }
        catch (Exception e) {
            ApplicationException ex = new($"Failed to convert json event {json} into a domain event. EventType was {eventType.Name}", e);
            throw ex;
        }

        return domainEvent;
    }

    public DomainEvent[] CreateDomainEvents(Guid aggregateId,
                                            IList<ResolvedEvent> @event) {
        return @event.Select(e => this.CreateDomainEvent(aggregateId, e)).ToArray();
    }
}