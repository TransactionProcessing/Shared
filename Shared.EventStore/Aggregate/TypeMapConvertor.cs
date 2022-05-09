namespace Shared.EventStore.Aggregate
{
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
        private static readonly DomainEventFactory domainEventFactory = new DomainEventFactory();

        #endregion

        #region Methods

        /// <summary>
        /// Convertors the specified aggregate identifier.
        /// </summary>
        /// <param name="aggregateId">The aggregate identifier.</param>
        /// <param name="event">The event.</param>
        /// <returns></returns>
        /// <exception cref="System.Exception">Could not find EventType {eventType.Name} in mapping list.</exception>
        public static IDomainEvent Convertor(Guid aggregateId,
                                             ResolvedEvent @event)
        {
            //TODO: We could pass this Type into GetDomainEvent(s)
            var eventType = TypeMap.GetType(@event.Event.EventType);

            if (eventType.IsSubclassOf(typeof(DomainEvent)))
            {
                return TypeMapConvertor.GetDomainEvent(aggregateId, @event);
            }

            throw new Exception($"Could not find EventType {eventType.Name} in mapping list.");
        }

        /// <summary>
        /// Convertors the specified event.
        /// </summary>
        /// <param name="event">The event.</param>
        /// <returns></returns>
        public static EventData Convertor(IDomainEvent @event)
        {
            EventDataFactory eventDataFactory = new EventDataFactory();
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
            return TypeMapConvertor.domainEventFactory.CreateDomainEvent(aggregateId, @event);
        }

        #endregion
    }
}