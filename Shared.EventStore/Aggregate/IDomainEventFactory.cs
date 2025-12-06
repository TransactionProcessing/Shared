using KurrentDB.Client;

namespace Shared.EventStore.Aggregate;

using System;
using System.Collections.Generic;
using DomainDrivenDesign.EventSourcing;
using global::EventStore.Client;

/// <summary>
/// 
/// </summary>
/// <typeparam name="TDomainEvent">The type of the domain event.</typeparam>
public interface IDomainEventFactory<out TDomainEvent> where TDomainEvent : IDomainEvent
{
    #region Methods

    /// <summary>
    /// Creates the agge aggregate snapshot.
    /// </summary>
    /// <param name="aggregateId">The aggregate identifier.</param>
    /// <param name="event">The event.</param>
    /// <returns></returns>
    TDomainEvent CreateDomainEvent(Guid aggregateId,
                                   ResolvedEvent @event);

    /// <summary>
    /// Creates the domain event.
    /// </summary>
    /// <param name="json">The json.</param>
    /// <param name="eventType">Type of the event.</param>
    /// <returns></returns>
    TDomainEvent CreateDomainEvent(String json,
                                   Type eventType);

    /// <summary>
    /// Creates the domain events.
    /// </summary>
    /// <param name="aggregateId">The aggregate identifier.</param>
    /// <param name="event">The event.</param>
    /// <returns></returns>
    TDomainEvent[] CreateDomainEvents(Guid aggregateId,
                                      IList<ResolvedEvent> @event);

    #endregion
}