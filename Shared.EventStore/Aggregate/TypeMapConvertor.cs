using System.Runtime.CompilerServices;
using KurrentDB.Client;
using Shared.Exceptions;

namespace Shared.EventStore.Aggregate;

using System;
using DomainDrivenDesign.EventSourcing;
using General;
using global::EventStore.Client;

/// <summary>
/// 
/// </summary>
public static class TypeMapConvertor
{
    #region Fields

    /// <summary>
    /// The domain event record factory
    /// </summary>
    private static readonly DomainEventFactory StandardDomainEventFactory = new();

    private static IDomainEventFactory<IDomainEvent> OverrideDomainEventFactory = null;

    #endregion

    #region Methods

    public static IDomainEvent Convertor(IDomainEventFactory<IDomainEvent> overrideFactory, Guid aggregateId, ResolvedEvent @event)
    {
        OverrideDomainEventFactory = overrideFactory;
        return Convertor(aggregateId, @event);
    }

    public static IDomainEvent Convertor(Guid aggregateId, ResolvedEvent @event)
    {
        Type eventType = null;

        try
        {
            eventType = TypeMap.GetType(@event.Event.EventType);
        }
        catch (Exception)
        {
            // Nothing here
        }

        if (eventType == null)
            throw new NotFoundException($"Could not find EventType {@event.Event.EventType} in mapping list.");

        if (eventType.IsSubclassOf(typeof(DomainEvent)))
        {
            return TypeMapConvertor.GetDomainEvent(aggregateId, @event);
        }

        return default;
    }

    /// <summary>
    /// Convertors the specified event.
    /// </summary>
    /// <param name="event">The event.</param>
    /// <returns></returns>
    public static EventData Convertor(IDomainEvent @event)
    {
        EventDataFactory eventDataFactory = new();
        return eventDataFactory.CreateEventData(@event);
    }

    /// <summary>
    /// Convertors the specified event.
    /// </summary>
    /// <param name="event">The event.</param>
    /// <returns></returns>
    public static IDomainEvent Convertor(ResolvedEvent @event) => TypeMapConvertor.Convertor(Guid.Empty, @event);

    /// <summary>
    /// Gets the domain event record.
    /// </summary>
    /// <param name="aggregateId">The aggregate identifier.</param>
    /// <param name="event">The event.</param>
    /// <returns></returns>
    private static IDomainEvent GetDomainEvent(Guid aggregateId,
                                               ResolvedEvent @event)
    {
        if (OverrideDomainEventFactory != null)
        {
            return OverrideDomainEventFactory.CreateDomainEvent(aggregateId, @event);
        }
        return TypeMapConvertor.StandardDomainEventFactory.CreateDomainEvent(aggregateId, @event);
    }

    #endregion
}