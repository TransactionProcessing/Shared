using KurrentDB.Client;
using TransactionProcessing.UnionResults;

namespace TransactionProcessing.Shared.EventStore.Aggregate;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DomainDrivenDesign.EventSourcing;
using EventStore;
using global::EventStore.Client;
using TransactionProcessing.Shared.TraceHandler;
using TransactionProcessing.Shared.Logger.TennantContext;

public sealed class AggregateRepository<TAggregate, TDomainEvent> : StandardTraceHandler, IAggregateRepository<TAggregate, TDomainEvent> where TAggregate : Aggregate, new()
    where TDomainEvent : IDomainEvent
{

    /// <summary>
    /// The convert to event
    /// </summary>
    internal readonly IEventStoreContext EventStoreContext;

    private readonly IDomainEventFactory<IDomainEvent> DomainEventFactory;



    public AggregateRepository(IEventStoreContext eventStoreContext, IDomainEventFactory<IDomainEvent> domainEventFactory)
    {
        this.EventStoreContext = eventStoreContext;
        DomainEventFactory = domainEventFactory;
    }



    public async Task<Result<TAggregate>> GetLatestVersion(Guid aggregateId,
                                                           CancellationToken cancellationToken)
    {
        TAggregate aggregate = new()
        {
            AggregateId = aggregateId
        };
        String streamName = AggregateRepository<TAggregate, TDomainEvent>.GetStreamName(aggregate.AggregateId);
            
        Result<List<ResolvedEvent>> readEventsResult = await this.EventStoreContext.ReadEvents(streamName, 0, cancellationToken);

        if (!readEventsResult.TryGetValueOrFailure(out var events, out var failure))
            return failure;

        return this.ProcessEvents(aggregate, events);
        
    }
        
    public async Task<Result<TAggregate>> GetLatestVersionFromLastEvent(Guid aggregateId,
                                                                        CancellationToken cancellationToken){
        TAggregate aggregate = new(){
            AggregateId = aggregateId
        };

        String streamName = AggregateRepository<TAggregate, TDomainEvent>.GetStreamName(aggregate.AggregateId);
        Result<List<ResolvedEvent>> getEventsResult = await this.EventStoreContext.GetEventsBackward(streamName, 1, cancellationToken);
        if (!getEventsResult.TryGetValueOrFailure(out var events, out var failure))
            return failure;

        return this.ProcessEvents(aggregate, events);
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
            return Results.Ok();

        String correlationId = TenantContext.CurrentTenant?.CorrelationId.ToString();
        List<EventData> events = new();

        foreach (IDomainEvent domainEvent in pendingEvents){
            EventData @event = String.IsNullOrWhiteSpace(correlationId)
                ? TypeMapConvertor.Convertor(domainEvent)
                : TypeMapConvertor.Convertor(domainEvent, correlationId);

            events.Add(@event);
        }

        Result result = await this.EventStoreContext.InsertEvents(streamName, aggregate.Version, events, cancellationToken);
        if (!result.IsSuccess())
            return result;

        aggregate.CommitPendingEvents();
        return Results.Ok();
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

                return Results.Ok(aggregate);
            }
            catch (Exception ex) {
                return ex.ToFailureErrors();
            }
        }

        return Results.Ok(aggregate);
    }

}
