using System;
using TransactionProcessing.UnionResults;

namespace TransactionProcessing.Shared.EventStore.Helpers;

public static class DomainServiceHelper
{
    public static Result<T> HandleGetAggregateResult<T>(Result<T> result, Guid aggregateId, bool isNotFoundError = true)
        where T : Aggregate.Aggregate, new()  // Constraint: T is a subclass of Aggregate and has a parameterless constructor
    {
        ErrorType? errorType = result.GetOutcome().Error?.ErrorType ?? result.GetOutcome().Errors?.ErrorType;
        if (result.IsFailed && errorType != ErrorType.NotFound) {
            return result;
        }

        if (errorType == ErrorType.NotFound && isNotFoundError) {
            return result;
        }

        T aggregate = errorType == ErrorType.NotFound
            ? new T { AggregateId = aggregateId }
            : result.Data;

        return Result.Success(aggregate);
    }
}
