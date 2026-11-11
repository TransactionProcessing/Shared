using System;
using SimpleResults;

namespace TransactionProcessing.Shared.EventStore.Helpers;

public static class DomainServiceHelper
{
    public static Result<T> HandleGetAggregateResult<T>(Result<T> result, Guid aggregateId, bool isNotFoundError = true)
        where T : Aggregate.Aggregate, new()  // Constraint: T is a subclass of Aggregate and has a parameterless constructor
    {
        if (result.IsFailed && result.Status != ResultStatus.NotFound) {
            return FailureResultFactory.Create(result);
        }

        if (result.Status == ResultStatus.NotFound && isNotFoundError) {
            return FailureResultFactory.Create(result);
        }

        T aggregate = result.Status switch
        {
            ResultStatus.NotFound => new T { AggregateId = aggregateId },  // Set AggregateId when creating a new instance
            _ => result.Data
        };

        return Result.Success(aggregate);
    }
}