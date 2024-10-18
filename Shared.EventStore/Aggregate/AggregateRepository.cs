using Shared.Exceptions;
using SimpleResults;

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
                                new Exception(
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

    public static class ResultHelpers{
        
        public static Result CreateFailure(Result result) {
            if (result.IsFailed) {
                return BuildResult(result.Status, result.Message, result.Errors);
            }
            return Result.Failure("Unknown Failure");
        }

        public static Result CreateFailure<T>(Result<T> result)
        {
            if (result.IsFailed) {
                return BuildResult(result.Status, result.Message, result.Errors);
            }
            return Result.Failure("Unknown Failure");
        }

        private static Result BuildResult(ResultStatus status, String messageValue, IEnumerable<String> errorList) {
            return (status, messageValue, errorList) switch
            {
                // If the status is NotFound and there are errors, return the errors
                (ResultStatus.NotFound, _, List<string> errors) when errors is { Count: > 0 } =>
                    Result.NotFound(errors),

                // If the status is NotFound and the message is not null or empty, return the message
                (ResultStatus.NotFound, string message, _) when !string.IsNullOrEmpty(message) =>
                    Result.NotFound(message),

                // If the status is Failure and there are errors, return the errors
                (ResultStatus.Failure, _, List<string> errors) when errors is { Count: > 0 } =>
                    Result.Failure(errors),

                // If the status is Failure and the message is not null or empty, return the message
                (ResultStatus.Failure, string message, _) when !string.IsNullOrEmpty(message) =>
                    Result.Failure(message),

                // If the status is Forbidden and there are errors, return the errors
                (ResultStatus.Forbidden, _, List<string> errors) when errors is { Count: > 0 } =>
                    Result.Forbidden(errors),

                // If the status is Forbidden and the message is not null or empty, return the message
                (ResultStatus.Forbidden, string message, _) when !string.IsNullOrEmpty(message) =>
                    Result.NotFound(message),
                //###
                // If the status is Invalid and there are errors, return the errors
                (ResultStatus.Invalid, _, List<string> errors) when errors is { Count: > 0 } =>
                    Result.Invalid(errors),

                // If the status is Invalid and the message is not null or empty, return the message
                (ResultStatus.Invalid, string message, _) when !string.IsNullOrEmpty(message) =>
                    Result.Invalid(message),

                // If the status is Unauthorized and there are errors, return the errors
                (ResultStatus.Unauthorized, _, List<string> errors) when errors is { Count: > 0 } =>
                    Result.Unauthorized(errors),

                // If the status is Unauthorized and the message is not null or empty, return the message
                (ResultStatus.Unauthorized, string message, _) when !string.IsNullOrEmpty(message) =>
                    Result.Unauthorized(message),

                // If the status is Conflict and there are errors, return the errors
                (ResultStatus.Conflict, _, List<string> errors) when errors is { Count: > 0 } =>
                    Result.Conflict(errors),

                // If the status is Conflict and the message is not null or empty, return the message
                (ResultStatus.Conflict, string message, _) when !string.IsNullOrEmpty(message) =>
                    Result.Conflict(message),

                // If the status is CriticalError and there are errors, return the errors
                (ResultStatus.CriticalError, _, List<string> errors) when errors is { Count: > 0 } =>
                    Result.CriticalError(errors),

                // If the status is CriticalError and the message is not null or empty, return the message
                (ResultStatus.CriticalError, string message, _) when !string.IsNullOrEmpty(message) =>
                    Result.CriticalError(message),

                // Default case, return a generic failure message
                _ => Result.Failure("An unexpected error occurred.")
            };
        }
    }
}
