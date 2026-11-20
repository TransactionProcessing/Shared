#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using TransactionProcessing.Shared.Exceptions;
using TransactionProcessing.UnionResults;

namespace TransactionProcessing.Shared.EventStore;

internal static class EventStoreResultExtensions
{
    internal static bool TryGetValueOrFailure<T>(this Result<T> result,
                                                [MaybeNullWhen(false)] out T value,
                                                [NotNullWhen(false)] out ErrorList? failure)
    {
        if (result.Value is Success<T> { Value: not null } success)
        {
            value = success.Value;
            failure = null;
            return true;
        }

        value = default;
        failure = result.Value is Success<T>
            ? new ErrorList([new Error(ErrorType.Critical, "The result contained no value.")])
            : GetFailureErrors(result.Value);
        return false;
    }

    internal static ErrorList GetFailureErrors(object? value) => value switch
    {
        Error error => new ErrorList([error]),
        ErrorList errors => errors,
        null => new ErrorList([new Error(ErrorType.Critical, "An uninitialized result has no outcome.")]),
        _ => throw new InvalidOperationException("Only a failed result can be propagated as a failure.")
    };

    internal static ErrorList ToFailureErrors(this Exception exception)
    {
        Error[] errors = exception.GetExceptionMessages()
            .Where(message => !String.IsNullOrWhiteSpace(message))
            .Select(message => new Error(ErrorType.Failure, message))
            .ToArray();

        return new ErrorList(errors.Length == 0
            ? [new Error(ErrorType.Failure, "An unspecified error occurred.")]
            : errors);
    }
}
