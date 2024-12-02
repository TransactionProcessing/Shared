using System;
using System.Collections.Generic;
using SimpleResults;

namespace Shared.Results;

public static class ResultHelpers
{
    public static Result<T> CreateFailedResult<T>(T resultData)
    {
        return new Result<T>
        {
            IsSuccess = false,
            Data = resultData
        };
    }

    public static Result CreateFailure(Result result) {
        return result.IsFailed switch {
            true => BuildResult(result.Status, result.Message, result.Errors),
            _ => throw new InvalidDataException("Cant create a failed result from a success")
        };
    }

    public static Result CreateFailure<T>(Result<T> result)
    {
        return result.IsFailed switch
        {
            true => BuildResult(result.Status, result.Message, result.Errors),
            _ => throw new InvalidDataException("Cant create a failed result from a success")
        };
    }

    private static Result BuildResult(ResultStatus status, String messageValue, IEnumerable<String> errorList)
    {
        return (status, messageValue, errorList) switch
        {
            // If the status is NotFound and there are errors, return the errors
            (ResultStatus.NotFound, _, List<string> errors) when errors is { Count: >= 0 } =>
                Result.NotFound(errors),

            // If the status is NotFound and the message is not null or empty, return the message
            (ResultStatus.NotFound, string message, _) when !string.IsNullOrEmpty(message) =>
                Result.NotFound(message),

            // If the status is Failure and there are errors, return the errors
            (ResultStatus.Failure, _, List<string> errors) when errors is { Count: >= 0 } =>
                Result.Failure(errors),

            // If the status is Failure and the message is not null or empty, return the message
            (ResultStatus.Failure, string message, _) when !string.IsNullOrEmpty(message) =>
                Result.Failure(message),

            // If the status is Forbidden and there are errors, return the errors
            (ResultStatus.Forbidden, _, List<string> errors) when errors is { Count: >= 0 } =>
                Result.Forbidden(errors),

            // If the status is Forbidden and the message is not null or empty, return the message
            (ResultStatus.Forbidden, string message, _) when !string.IsNullOrEmpty(message) =>
                Result.Forbidden(message),
            //###
            // If the status is Invalid and there are errors, return the errors
            (ResultStatus.Invalid, _, List<string> errors) when errors is { Count: >= 0 } =>
                Result.Invalid(errors),

            // If the status is Invalid and the message is not null or empty, return the message
            (ResultStatus.Invalid, string message, _) when !string.IsNullOrEmpty(message) =>
                Result.Invalid(message),

            // If the status is Unauthorized and there are errors, return the errors
            (ResultStatus.Unauthorized, _, List<string> errors) when errors is { Count: >= 0 } =>
                Result.Unauthorized(errors),

            // If the status is Unauthorized and the message is not null or empty, return the message
            (ResultStatus.Unauthorized, string message, _) when !string.IsNullOrEmpty(message) =>
                Result.Unauthorized(message),

            // If the status is Conflict and there are errors, return the errors
            (ResultStatus.Conflict, _, List<string> errors) when errors is { Count: >= 0 } =>
                Result.Conflict(errors),

            // If the status is Conflict and the message is not null or empty, return the message
            (ResultStatus.Conflict, string message, _) when !string.IsNullOrEmpty(message) =>
                Result.Conflict(message),

            // If the status is CriticalError and there are errors, return the errors
            (ResultStatus.CriticalError, _, List<string> errors) when errors is { Count: >= 0 } =>
                Result.CriticalError(errors),

            // If the status is CriticalError and the message is not null or empty, return the message
            (ResultStatus.CriticalError, string message, _) when !string.IsNullOrEmpty(message) =>
                Result.CriticalError(message),

            // Default case, return a generic failure message
            _ => Result.Failure("An unexpected error occurred.")
        };
    }
}