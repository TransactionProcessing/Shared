namespace Shared.EventStore.Aggregate
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using CSharpFunctionalExtensions;
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

        #endregion

        #region Constructors

        public AggregateRepository(IEventStoreContext eventStoreContext)
        {
            this.EventStoreContext = eventStoreContext;
        }

        #endregion

        #region Methods

        public async Task<Result<TAggregate>> GetLatestVersion(Guid aggregateId,
                                                               CancellationToken cancellationToken)
        {
            TAggregate aggregate = new()
                                   {
                                       AggregateId = aggregateId
                                   };
            String streamName = AggregateRepository<TAggregate, TDomainEvent>.GetStreamName(aggregate.AggregateId);
            try
            {
                List<ResolvedEvent> resolvedEvents = await this.EventStoreContext.ReadEvents(streamName, 0, cancellationToken);

                aggregate = this.ProcessEvents(aggregate, resolvedEvents);
                return Result.Success(aggregate);
            }
            catch (Exception ex){
                this.LogError(ex);
                return Result.Failure<TAggregate>($"Failed to get latest version of aggregate stream {streamName}. Exception [{ex}]");
            }
        }

        public async Task<Result<TAggregate>> GetLatestVersionFromLastEvent(Guid aggregateId,
                                                                            CancellationToken cancellationToken){
            TAggregate aggregate = new(){
                                            AggregateId = aggregateId
                                        };

            String streamName = AggregateRepository<TAggregate, TDomainEvent>.GetStreamName(aggregate.AggregateId);
            try{
                IList<ResolvedEvent> events = await this.EventStoreContext.GetEventsBackward(streamName, 1, cancellationToken);

                aggregate = this.ProcessEvents(aggregate, events);

                return Result.Success(aggregate);
            }
            catch(Exception ex){
                this.LogError(ex);
                return Result.Failure<TAggregate>($"Failed to get latest version of aggregate stream {streamName} from last event. Exception [{ex}]");
            }
        }

        public static String GetStreamName(Guid aggregateId)
        {
            return typeof(TAggregate).Name + "-" + aggregateId.ToString().Replace("-", string.Empty);
        }

        public async Task<Result> SaveChanges(TAggregate aggregate,
                                      CancellationToken cancellationToken)
        {
            String streamName = AggregateRepository<TAggregate, TDomainEvent>.GetStreamName(aggregate.AggregateId);
            IList<IDomainEvent> pendingEvents = aggregate.GetPendingEvents();

            try{
                
                if (!pendingEvents.Any())
                    return Result.Success();

                List<EventData> events = new();

                foreach (IDomainEvent domainEvent in pendingEvents){
                    EventData @event = TypeMapConvertor.Convertor(domainEvent);

                    events.Add(@event);
                }

                await this.EventStoreContext.InsertEvents(streamName, aggregate.Version, events, cancellationToken);
                aggregate.CommitPendingEvents();

                return Result.Success(aggregate);
            }
            catch (Exception ex)
            {
                this.LogError(ex);
                return Result.Failure<TAggregate>($"Failed to save events to aggregate stream {streamName}. Exception [{ex}]");
            }
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
}