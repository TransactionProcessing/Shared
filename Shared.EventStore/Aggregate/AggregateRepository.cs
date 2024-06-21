namespace Shared.EventStore.Aggregate
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using DomainDrivenDesign.EventSourcing;
    using EventStore;
    using global::EventStore.Client;
    using Shared.TraceHandler;

    public sealed class AggregateRepository<TAggregate, TDomainEvent> : StandardTraceHandler, IAggregateRepository<TAggregate, TDomainEvent> where TAggregate : Aggregate, new()
                                                                                                                                           where TDomainEvent : IDomainEvent
    {
        #region Fields

        /// <summary>
        /// The convert to event
        /// </summary>
        internal readonly IEventStoreContext EventStoreContext;

        private readonly IDomainEventFactory<IDomainEvent> DomainEventFactory;

        #endregion

        #region Constructors

        public AggregateRepository(IEventStoreContext eventStoreContext, IDomainEventFactory<IDomainEvent> domainEventFactory)
        {
            this.EventStoreContext = eventStoreContext;
            DomainEventFactory = domainEventFactory;
        }

        #endregion

        #region Methods

        public async Task<TAggregate> GetLatestVersion(Guid aggregateId,
                                                               CancellationToken cancellationToken)
        {
            TAggregate aggregate = new()
                                   {
                                       AggregateId = aggregateId
                                   };
            String streamName = AggregateRepository<TAggregate, TDomainEvent>.GetStreamName(aggregate.AggregateId);
            
            List<ResolvedEvent> resolvedEvents = await this.EventStoreContext.ReadEvents(streamName, 0, cancellationToken);

            return this.ProcessEvents(aggregate, resolvedEvents);
        
        }

        public async Task<TAggregate> GetLatestVersionFromLastEvent(Guid aggregateId,
                                                                    CancellationToken cancellationToken){
            TAggregate aggregate = new(){
                                            AggregateId = aggregateId
                                        };

            String streamName = AggregateRepository<TAggregate, TDomainEvent>.GetStreamName(aggregate.AggregateId);
            IList<ResolvedEvent> events = await this.EventStoreContext.GetEventsBackward(streamName, 1, cancellationToken);

            return this.ProcessEvents(aggregate, events);
        }

        public static String GetStreamName(Guid aggregateId)
        {
            return typeof(TAggregate).Name + "-" + aggregateId.ToString().Replace("-", string.Empty);
        }

        public async Task SaveChanges(TAggregate aggregate,
                                      CancellationToken cancellationToken){
            String streamName = AggregateRepository<TAggregate, TDomainEvent>.GetStreamName(aggregate.AggregateId);
            IList<IDomainEvent> pendingEvents = aggregate.GetPendingEvents();


            if (!pendingEvents.Any())
                return;

            List<EventData> events = new();

            foreach (IDomainEvent domainEvent in pendingEvents){
                EventData @event = TypeMapConvertor.Convertor(domainEvent);

                events.Add(@event);
            }

            await this.EventStoreContext.InsertEvents(streamName, aggregate.Version, events, cancellationToken);
            aggregate.CommitPendingEvents();
        }

        private TAggregate ProcessEvents(TAggregate aggregate,
                                         IList<ResolvedEvent> resolvedEvents)
        {
            if (resolvedEvents != null && resolvedEvents.Count > 0)
            {
                List<IDomainEvent> domainEvents = new();

                foreach (ResolvedEvent resolvedEvent in resolvedEvents)
                {
                    IDomainEvent domainEvent = TypeMapConvertor.Convertor(DomainEventFactory, aggregate.AggregateId, resolvedEvent);

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
}