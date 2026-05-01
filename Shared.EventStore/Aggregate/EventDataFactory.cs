using KurrentDB.Client;
using Shared.Serialisation;

namespace Shared.EventStore.Aggregate;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DomainDrivenDesign.EventSourcing;

public class EventDataFactory : IEventDataFactory
{
    private SerialiserOptions SerialiserOptions = new SerialiserOptions(SerialiserPropertyFormat.CamelCase, IgnoreNullValues: true, WriteIndented: true);

    public EventData CreateEventData(IDomainEvent domainEvent)
    {
        this.GuardAgainstNoDomainEvent(domainEvent);    

        Byte[] data = Encoding.Default.GetBytes(StringSerialiser.Serialise(domainEvent, SerialiserOptions));

        EventData eventData = new(Uuid.FromGuid(domainEvent.EventId), domainEvent.EventType, data);

        return eventData;
    }

    public EventData[] CreateEventDataList(IList<IDomainEvent> domainEvents)
    {
        return domainEvents.Select(this.CreateEventData).ToArray();
    }

    private void GuardAgainstNoDomainEvent(IDomainEvent @event)
    {
        if (@event == null)
        {
            throw new ArgumentNullException(nameof(@event), "No domain event provided");
        }
    }

}