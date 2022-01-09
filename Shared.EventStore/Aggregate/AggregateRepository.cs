using ClientAPIResolvedEvent = EventStore.ClientAPI.ResolvedEvent;

namespace Shared.EventStore.Aggregate
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using DomainDrivenDesign.EventSourcing;
    using EventStore;
    using General;
    using global::EventStore.Client;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Newtonsoft.Json.Serialization;
    using Serialisation;

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TAggregate">The type of the aggregate.</typeparam>
    /// <typeparam name="TDomainEvent">The type of the domain event.</typeparam>
    /// <seealso cref="Shared.EventStore.Aggregate.IAggregateRepository&lt;TAggregate, TDomainEvent&gt;" />
    /// <seealso cref="Shared.EventStore.EventStore.IAggregateRepository{T}" />
    public sealed class AggregateRepository<TAggregate, TDomainEvent> : IAggregateRepository<TAggregate, TDomainEvent> where TAggregate : Aggregate, new()
        where TDomainEvent : IDomainEvent
    {
        #region Fields

        /// <summary>
        /// The convert to event
        /// </summary>
        internal readonly IEventStoreContext EventStoreContext;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="AggregateRepository{TAggregate, TDomainEvent}"/> class.
        /// </summary>
        /// <param name="eventStoreContext">The event store context.</param>
        public AggregateRepository(IEventStoreContext eventStoreContext)
        {
            this.EventStoreContext = eventStoreContext;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets the latest version.
        /// </summary>
        /// <param name="aggregateId">The aggregate identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public async Task<TAggregate> GetLatestVersion(Guid aggregateId,
                                                       CancellationToken cancellationToken)
        {
            TAggregate aggregate = new()
                                   {
                                       AggregateId = aggregateId
                                   };

            String streamName = AggregateRepository<TAggregate, TDomainEvent>.GetStreamName(aggregate.AggregateId);

            var resolvedEvents = await this.EventStoreContext.ReadEvents(streamName, 0, cancellationToken);

            return this.ProcessEvents(aggregate, resolvedEvents);
        }

        /// <summary>
        /// Gets the name of the stream.
        /// </summary>
        /// <param name="aggregateId">The aggregate identifier.</param>
        /// <returns></returns>
        public static String GetStreamName(Guid aggregateId)
        {
            return typeof(TAggregate).Name + "-" + aggregateId.ToString().Replace("-", string.Empty);
        }

        /// <summary>
        /// Saves the changes.
        /// </summary>
        /// <param name="aggregate">The aggregate.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public async Task SaveChanges(TAggregate aggregate,
                                      CancellationToken cancellationToken)
        {
            String streamName = AggregateRepository<TAggregate, TDomainEvent>.GetStreamName(aggregate.AggregateId);
            IList<IDomainEvent> pendingEvents = aggregate.GetPendingEvents();

            if (!pendingEvents.Any())
                return;

            List<EventData> events = new();

            foreach (IDomainEvent domainEvent in pendingEvents)
            {
                EventData @event = TypeMapConvertor.Convertor(domainEvent);

                events.Add(@event);
            }

            await this.EventStoreContext.InsertEvents(streamName, aggregate.Version, events, cancellationToken);
            aggregate.CommitPendingEvents();
        }

        /// <summary>
        /// Processes the events.
        /// </summary>
        /// <param name="aggregate">The aggregate.</param>
        /// <param name="resolvedEvents">The resolved events.</param>
        /// <returns></returns>
        private TAggregate ProcessEvents(TAggregate aggregate,
                                         IList<ResolvedEvent> resolvedEvents)
        {
            if (resolvedEvents != null && resolvedEvents.Count > 0)
            {
                List<IDomainEvent> domainEvents = new();

                foreach (var resolvedEvent in resolvedEvents)
                {
                    IDomainEvent domainEvent = TypeMapConvertor.Convertor(aggregate.AggregateId, resolvedEvent);

                    domainEvents.Add(domainEvent);
                }

                return domainEvents.Aggregate(aggregate,
                                              (aggregate1,
                                               @event) =>
                                              {
                                                  try
                                                  {
                                                      aggregate1.Apply(@event);
                                                      return aggregate1;
                                                  }
                                                  catch(Exception e)
                                                  {
                                                      Exception ex = new Exception($"Failed to apply domain event {@event.EventType} to Aggregate {aggregate.GetType()} ",
                                                                                   e);
                                                      throw ex;
                                                  }
                                              });
            }

            return aggregate;
        }

        #endregion
    }

    /// <summary>
    /// 
    /// </summary>
    public static class ResolvedEventExtensions
    {
        #region Methods

        /// <summary>
        /// Gets the resolved event data as string.
        /// </summary>
        /// <param name="resolvedEvent">The resolved event.</param>
        /// <returns></returns>
        public static String GetResolvedEventDataAsString(this ResolvedEvent resolvedEvent)
        {
            return Encoding.Default.GetString(resolvedEvent.Event.Data.ToArray(), 0, resolvedEvent.Event.Data.Length);
        }

        /// <summary>
        /// Gets the resolved event data as string.
        /// </summary>
        /// <param name="resolvedEvent">The resolved event.</param>
        /// <returns></returns>
        public static String GetResolvedEventDataAsString(this ClientAPIResolvedEvent resolvedEvent)
        {
            return Encoding.Default.GetString(resolvedEvent.Event.Data.ToArray(), 0, resolvedEvent.Event.Data.Length);
        }

        #endregion
    }

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

    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="Shared.EventStore.Aggregate.IDomainEventFactory&lt;Shared.DomainDrivenDesign.EventSourcing.DomainEventRecord.DomainEvent&gt;" />
    public class DomainEventRecordFactory : IDomainEventFactory<DomainEventRecord.DomainEvent>
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="DomainEventFactory2" /> class.
        /// </summary>
        public DomainEventRecordFactory()
        {
            JsonIgnoreAttributeIgnorerContractResolver jsonIgnoreAttributeIgnorerContractResolver = new JsonIgnoreAttributeIgnorerContractResolver();

            JsonConvert.DefaultSettings = () =>
                                          {
                                              return new JsonSerializerSettings
                                                     {
                                                         ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                                                         TypeNameHandling = TypeNameHandling.All,
                                                         Formatting = Formatting.Indented,
                                                         DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                                                         ContractResolver = jsonIgnoreAttributeIgnorerContractResolver
                                                     };
                                          };
        }

        #endregion

        #region Methods

        /// <summary>
        /// Creates the agge aggregate snapshot.
        /// </summary>
        /// <param name="aggregateId"></param>
        /// <param name="event">The event.</param>
        /// <returns></returns>
        /// <exception cref="System.Exception">Failed to find a domain event with type {@event.Event.EventType}</exception>
        public DomainEventRecord.DomainEvent CreateDomainEvent(Guid aggregateId,
                                                               ResolvedEvent @event)
        {
            String json = @event.GetResolvedEventDataAsString();

            Type eventType = TypeMap.GetType(@event.Event.EventType);

            if (eventType == null)
                throw new Exception($"Failed to find a domain event with type {@event.Event.EventType}");

            DomainEventRecord.DomainEvent domainEvent = (DomainEventRecord.DomainEvent)JsonConvert.DeserializeObject(json, eventType);

            domainEvent = domainEvent with
                          {
                              AggregateId = aggregateId,
                              AggregateVersion = @event.Event.EventNumber.ToInt64(),
                              EventNumber = @event.Event.EventNumber.ToInt64(),
                              EventType = @event.Event.EventType,
                              EventId = @event.Event.EventId.ToGuid(),
                              EventTimestamp = @event.Event.Created,
                          };

            return domainEvent;
        }

        /// <summary>
        /// Creates the domain event.
        /// </summary>
        /// <param name="json">The json.</param>
        /// <param name="eventType">Type of the event.</param>
        /// <returns></returns>
        public DomainEventRecord.DomainEvent CreateDomainEvent(String json,
                                                               Type eventType)
        {
            DomainEventRecord.DomainEvent domainEvent;
            JObject jObject = JObject.Parse(json);

            try
            {
                domainEvent = (DomainEventRecord.DomainEvent)JsonConvert.DeserializeObject(json, eventType);
            }
            catch(Exception e)
            {
                Exception ex = new($"Failed to convert json event {json} into a domain event. EventType was {eventType.Name}", e);
                throw ex;
            }

            return domainEvent;
        }

        /// <summary>
        /// Creates the domain events.
        /// </summary>
        /// <param name="aggregateId"></param>
        /// <param name="event">The event.</param>
        /// <returns></returns>
        public DomainEventRecord.DomainEvent[] CreateDomainEvents(Guid aggregateId,
                                                                  IList<ResolvedEvent> @event)
        {
            return @event.Select(e => this.CreateDomainEvent(aggregateId, e)).ToArray();
        }

        #endregion

        //public EventStoreMetadata GetEventStoreMetadata(ResolvedEvent @event)
        //{
        //    return this.JsonSerialiser.Deserialise<EventStoreMetadata>(Encoding.Default.GetString(@event.Event.Metadata, 0, @event.Event.Metadata.Length)) ?? new EventStoreMetadata();
        //}
    }

    /// <summary>
    /// 
    /// </summary>
    public static class TypeProvider
    {
        #region Fields

        /// <summary>
        /// The default assembly filters
        /// </summary>
        private static readonly List<String> DefaultAssemblyFilters = new List<String>
                                                                      {
                                                                          "Microsoft"
                                                                      };

        #endregion

        #region Methods

        /// <summary>
        /// Loads the domain events type dynamically.
        /// </summary>
        /// <param name="assemblyFilters">The assembly filters.</param>
        public static void LoadDomainEventsTypeDynamically(List<String> assemblyFilters = null)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            if (assemblyFilters == null)
                assemblyFilters = TypeProvider.DefaultAssemblyFilters;

            IEnumerable<Type> allTypes = null;
            foreach (String filter in assemblyFilters)
            {
                allTypes = assemblies.Where(a => a.FullName.Contains(filter) == false).SelectMany(a => a.GetTypes());
            }

            var filteredTypes = allTypes.Where(t => t.IsSubclassOf(typeof(DomainEvent)) || t.IsSubclassOf(typeof(DomainEventRecord.DomainEvent))).OrderBy(e => e.Name)
                                        .ToList();

            foreach (Type type in filteredTypes)
            {
                TypeMap.AddType(type, type.Name);
            }
        }

        #endregion
    }

    /// <summary>
    /// 
    /// </summary>
    public static class TypeMapConvertor
    {
        #region Fields

        /// <summary>
        /// The domain event factory
        /// </summary>
        private static readonly DomainEventFactory domainEventFactory = new DomainEventFactory();

        /// <summary>
        /// The domain event record factory
        /// </summary>
        private static readonly DomainEventRecordFactory domainEventRecordFactory = new DomainEventRecordFactory();

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

            if (eventType.IsSubclassOf(typeof(DomainEventRecord.DomainEvent)))
            {
                return TypeMapConvertor.GetDomainEventRecord(aggregateId, @event);
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
        /// Gets the domain event.
        /// </summary>
        /// <param name="aggregateId">The aggregate identifier.</param>
        /// <param name="event">The event.</param>
        /// <returns></returns>
        private static IDomainEvent GetDomainEvent(Guid aggregateId,
                                                   ResolvedEvent @event)
        {
            return TypeMapConvertor.domainEventFactory.CreateDomainEvent(aggregateId, @event);
        }

        /// <summary>
        /// Gets the domain event record.
        /// </summary>
        /// <param name="aggregateId">The aggregate identifier.</param>
        /// <param name="event">The event.</param>
        /// <returns></returns>
        private static IDomainEvent GetDomainEventRecord(Guid aggregateId,
                                                         ResolvedEvent @event)
        {
            return TypeMapConvertor.domainEventRecordFactory.CreateDomainEvent(aggregateId, @event);
        }

        #endregion
    }

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