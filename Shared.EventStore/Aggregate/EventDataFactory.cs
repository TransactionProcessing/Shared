namespace Shared.EventStore.Aggregate
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using DomainDrivenDesign.EventSourcing;
    using global::EventStore.Client;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;

    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="Shared.EventStore.Aggregate.IEventDataFactory" />
    public class EventDataFactory : IEventDataFactory
    {
        #region Fields

        /// <summary>
        /// The json options function
        /// </summary>
        private static readonly Func<JsonSerializerSettings> jsonOptionsFunc = () => new JsonSerializerSettings
                                                                                     {
                                                                                         ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                                                                                         TypeNameHandling = TypeNameHandling.None,
                                                                                         Formatting = Formatting.None,
                                                                                         ContractResolver = new CamelCasePropertyNamesContractResolver(),
                                                                                         DefaultValueHandling = DefaultValueHandling.Ignore
                                                                                     };

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="EventDataFactory" /> class.
        /// </summary>
        public EventDataFactory()
        {
            //this.Serialiser = new JsonSerialiser(jsonOptionsFunc);
            JsonConvert.DefaultSettings = EventDataFactory.jsonOptionsFunc;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Creates the event data.
        /// </summary>
        /// <param name="domainEvent">The domain event.</param>
        /// <returns>
        /// EventData.
        /// </returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public EventData CreateEventData(IDomainEvent domainEvent)
        {
            this.GuardAgainstNoDomainEvent(domainEvent);

            Byte[] data = Encoding.Default.GetBytes(JsonConvert.SerializeObject(domainEvent));

            EventData eventData = new EventData(Uuid.FromGuid(domainEvent.EventId), domainEvent.EventType, data);

            return eventData;
        }

        /// <summary>
        /// Creates the event data.
        /// </summary>
        /// <param name="domainEvents">The domain events.</param>
        /// <returns></returns>
        public EventData[] CreateEventDataList(IList<IDomainEvent> domainEvents)
        {
            return domainEvents.Select(this.CreateEventData).ToArray();
        }

        /// <summary>
        /// Guards the against no domain event.
        /// </summary>
        /// <param name="event">The event.</param>
        /// <exception cref="System.ArgumentNullException">@event;No domain event provided</exception>
        private void GuardAgainstNoDomainEvent(IDomainEvent @event)
        {
            if (@event == null)
            {
                throw new ArgumentNullException(nameof(@event), "No domain event provided");
            }
        }

        #endregion
    }
}