namespace Shared.EventStore.Aggregate
{
    using System.Collections.Generic;
    using DomainDrivenDesign.EventSourcing;
    using global::EventStore.Client;

    /// <summary>
    /// 
    /// </summary>
    public interface IEventDataFactory
    {
        #region Methods

        /// <summary>
        /// Creates the event data.
        /// </summary>
        /// <param name="domainEvent">The domain event.</param>
        /// <returns></returns>
        EventData CreateEventData(IDomainEvent domainEvent);

        /// <summary>
        /// Creates the event data.
        /// </summary>
        /// <param name="domainEvents">The domain events.</param>
        /// <returns></returns>
        EventData[] CreateEventDataList(IList<IDomainEvent> domainEvents);

        #endregion
    }
}