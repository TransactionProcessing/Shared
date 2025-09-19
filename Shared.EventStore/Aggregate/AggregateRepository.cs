using Shared.Exceptions;
using Shared.Results;
using SimpleResults;

namespace Shared.EventStore.Aggregate;

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

    public async Task<Result<TAggregate>> GetLatestVersion(Guid aggregateId,
                                                           CancellationToken cancellationToken)
    {
        TAggregate aggregate = new()
        {
            AggregateId = aggregateId
        };
        String streamName = AggregateRepository<TAggregate, TDomainEvent>.GetStreamName(aggregate.AggregateId);
            
        Result<List<ResolvedEvent>> readEventsResult = await this.EventStoreContext.ReadEvents(streamName, 0, cancellationToken);

        if (readEventsResult.IsFailed)
            return ResultHelpers.CreateFailure(readEventsResult);

        return this.ProcessEvents(aggregate, readEventsResult.Data);
        
    }
        
    public async Task<Result<TAggregate>> GetLatestVersionFromLastEvent(Guid aggregateId,
                                                                        CancellationToken cancellationToken){
        TAggregate aggregate = new(){
            AggregateId = aggregateId
        };

        String streamName = AggregateRepository<TAggregate, TDomainEvent>.GetStreamName(aggregate.AggregateId);
        Result<List<ResolvedEvent>> getEventsResult = await this.EventStoreContext.GetEventsBackward(streamName, 1, cancellationToken);
        if (getEventsResult.IsFailed)
            return ResultHelpers.CreateFailure(getEventsResult);
        return this.ProcessEvents(aggregate, getEventsResult.Data);
    }

    public static String GetStreamName(Guid aggregateId)
    {
        return typeof(TAggregate).Name + "-" + aggregateId.ToString().Replace("-", string.Empty);
    }

    public async Task<Result> SaveChanges(TAggregate aggregate,
                                          CancellationToken cancellationToken){
        String streamName = AggregateRepository<TAggregate, TDomainEvent>.GetStreamName(aggregate.AggregateId);
        IList<IDomainEvent> pendingEvents = aggregate.GetPendingEvents();


        if (!pendingEvents.Any())
            return Result.Success();

        List<EventData> events = new();

        foreach (IDomainEvent domainEvent in pendingEvents){
            EventData @event = TypeMapConvertor.Convertor(domainEvent);

            events.Add(@event);
        }

        Result result = await this.EventStoreContext.InsertEvents(streamName, aggregate.Version, events, cancellationToken);
        if (result.IsFailed)
            return ResultHelpers.CreateFailure(result);

        aggregate.CommitPendingEvents();
        return Result.Success();
    }

    private Result<TAggregate> ProcessEvents(TAggregate aggregate,
                                             IList<ResolvedEvent> resolvedEvents) {

        if (resolvedEvents != null && resolvedEvents.Count > 0) {
            List<IDomainEvent> domainEvents = new();

            foreach (ResolvedEvent resolvedEvent in resolvedEvents) {
                IDomainEvent domainEvent =
                    TypeMapConvertor.Convertor(DomainEventFactory, aggregate.AggregateId, resolvedEvent);

                domainEvents.Add(domainEvent);
            }

            try {
                domainEvents.Aggregate(aggregate, (aggregate1,
                                                   @event) => {
                    try {
                        aggregate1.Apply(@event);
                        return aggregate1;
                    }
                    catch (Exception e) {
                        Exception ex =
                            new(
                                $"Failed to apply domain event {@event.EventType} to Aggregate {aggregate.GetType()} ",
                                e);
                        throw ex;
                    }
                });

                return Result.Success(aggregate);
            }
            catch (Exception ex) {
                return Result.Failure(ex.GetExceptionMessages());
            }
        }

        return Result.Success(aggregate);
    }

    #endregion
}