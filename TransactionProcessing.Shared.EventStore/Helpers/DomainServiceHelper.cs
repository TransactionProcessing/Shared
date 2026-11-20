using System;
using TransactionProcessing.UnionResults;

namespace TransactionProcessing.Shared.EventStore.Helpers;

public static class DomainServiceHelper
{
    public static Result<T> HandleGetAggregateResult<T>(Result<T> result, Guid aggregateId, bool isNotFoundError = true)
        where T : Aggregate.Aggregate, new()  // Constraint: T is a subclass of Aggregate and has a parameterless constructor
    {
        if (result.TryGetValueOrFailure(out var aggregate, out var failure))
            return Result.Success(aggregate);

        if (failure.ErrorType == ErrorType.NotFound && !isNotFoundError)
            return Result.Success(new T { AggregateId = aggregateId });

        return failure;
    }
}
