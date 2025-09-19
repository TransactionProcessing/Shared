namespace Shared.EventStore.Aggregate;

using System;
using System.Collections.Generic;
using System.Linq;
using DomainDrivenDesign.EventSourcing;

public static class AggregateExtensions
{
    #region Methods

    /// <summary>
    /// Applies the specified historic event.
    /// </summary>
    /// <param name="aggregate">The aggregate.</param>
    /// <param name="historicEvent">The historic event.</param>
    internal static void Apply(this Aggregate aggregate,
                               IDomainEvent historicEvent)
    {
        aggregate.Version = historicEvent.EventNumber;

        if (aggregate.EventHistory.ContainsKey(historicEvent.EventId))
            return;

        aggregate.PlayEvent(historicEvent);
        aggregate.EventHistory.Add(historicEvent.EventId, historicEvent);
    }

    /// <summary>
    /// Commits the pending events.
    /// </summary>
    /// <param name="aggregate">The aggregate.</param>
    internal static void CommitPendingEvents(this Aggregate aggregate)
    {
        foreach (IDomainEvent pendingEvent in aggregate.PendingEvents)
            aggregate.EventHistory.Add(pendingEvent.EventId, pendingEvent);

        aggregate.PendingEvents.Clear();
    }

    /// <summary>
    /// Gets the historical events.
    /// </summary>
    /// <param name="aggregate">The aggregate.</param>
    /// <returns></returns>
    internal static IList<IDomainEvent> GetHistoricalEvents(this Aggregate aggregate)
    {
        return aggregate.EventHistory.Select(x => x.Value).ToList();
    }

    /// <summary>
    /// Gets the pending events.
    /// </summary>
    /// <param name="aggregate">The aggregate.</param>
    /// <returns></returns>
    internal static IList<IDomainEvent> GetPendingEvents(this Aggregate aggregate)
    {
        return aggregate.PendingEvents;
    }

    /// <summary>
    /// Determines whether [is event duplicate] [the specified event identifier].
    /// </summary>
    /// <param name="aggregate">The aggregate.</param>
    /// <param name="eventId">The event identifier.</param>
    /// <returns>
    ///   <c>true</c> if [is event duplicate] [the specified event identifier]; otherwise, <c>false</c>.
    /// </returns>
    internal static Boolean IsEventDuplicate(this Aggregate aggregate,
                                             Guid eventId)
    {
        return aggregate.EventHistory.ContainsKey(eventId) || aggregate.PendingEvents.Any(x => x.EventId == eventId);
    }

    #endregion
}