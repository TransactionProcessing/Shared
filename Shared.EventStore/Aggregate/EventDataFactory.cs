using KurrentDB.Client;
using Shared.Serialisation;

namespace Shared.EventStore.Aggregate;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using DomainDrivenDesign.EventSourcing;
using Shared.Logger.TennantContext;

public class EventDataFactory : IEventDataFactory
{
    private SerialiserOptions SerialiserOptions = new SerialiserOptions(SerialiserPropertyFormat.CamelCase, IgnoreNullValues: true, WriteIndented: true);

    public EventData CreateEventData(IDomainEvent domainEvent)
    {
        return this.CreateEventData(domainEvent, null);
    }

    public EventData CreateEventData(IDomainEvent domainEvent, String correlationId)
    {
        this.GuardAgainstNoDomainEvent(domainEvent);    

        Byte[] data = Encoding.Default.GetBytes(StringSerialiser.Serialise(domainEvent, SerialiserOptions));
        ReadOnlyMemory<byte>? metadata = null;
        if (!String.IsNullOrWhiteSpace(correlationId))
        {
            Byte[] metadataBytes = JsonSerializer.SerializeToUtf8Bytes(new Dictionary<String, String>
            {
                [TenantContext.KeyNameCorrelationId] = correlationId
            });
            metadata = new ReadOnlyMemory<Byte>(metadataBytes);
        }

        EventData eventData = new(Uuid.FromGuid(domainEvent.EventId), domainEvent.EventType, data, metadata);

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
