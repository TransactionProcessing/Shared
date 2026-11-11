using System.Collections.Generic;
using System.IO;
using SimpleResults;

namespace TransactionProcessing.ClientProxyBase;

internal static class FailureResultFactory
{
    public static Result Create(Result result) => result.IsFailed
        ? Build(result.Status, result.Message, result.Errors)
        : throw new InvalidDataException("Cannot create a failed result from a success.");

    public static Result Create<T>(Result<T> result) => result.IsFailed
        ? Build(result.Status, result.Message, result.Errors)
        : throw new InvalidDataException("Cannot create a failed result from a success.");

    private static Result Build(ResultStatus status, string message, IEnumerable<string> errors) => (status, message, errors) switch
    {
        (ResultStatus.NotFound, _, List<string> values) when values.Count > 0 => Result.NotFound(values),
        (ResultStatus.NotFound, not null and not "", _) => Result.NotFound(message),
        (ResultStatus.Failure, _, List<string> values) when values.Count > 0 => Result.Failure(values),
        (ResultStatus.Failure, not null and not "", _) => Result.Failure(message),
        (ResultStatus.Forbidden, _, List<string> values) when values.Count > 0 => Result.Forbidden(values),
        (ResultStatus.Forbidden, not null and not "", _) => Result.Forbidden(message),
        (ResultStatus.Invalid, _, List<string> values) when values.Count > 0 => Result.Invalid(values),
        (ResultStatus.Invalid, not null and not "", _) => Result.Invalid(message),
        (ResultStatus.Unauthorized, _, List<string> values) when values.Count > 0 => Result.Unauthorized(values),
        (ResultStatus.Unauthorized, not null and not "", _) => Result.Unauthorized(message),
        (ResultStatus.Conflict, _, List<string> values) when values.Count > 0 => Result.Conflict(values),
        (ResultStatus.Conflict, not null and not "", _) => Result.Conflict(message),
        (ResultStatus.CriticalError, _, List<string> values) when values.Count > 0 => Result.CriticalError(values),
        (ResultStatus.CriticalError, not null and not "", _) => Result.CriticalError(message),
        _ => Result.Failure("An unexpected error occurred.")
    };
}
