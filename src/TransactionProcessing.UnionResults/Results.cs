
namespace TransactionProcessing.UnionResults;

public union Result(Success, Error, ErrorList);

public union Result<T>(Success<T>, Error, ErrorList);

public sealed record Success();

/// <summary>Provides access to a successful payload when its type is known only at runtime.</summary>
public interface IResultSuccess
{
    object? Value { get; }
}

public sealed record Success<T>(T Value) : IResultSuccess
{
    object? IResultSuccess.Value => Value;
}

public sealed record Error(ErrorType ErrorType, String Message);

public sealed class ErrorList : IReadOnlyList<Error> {
    private readonly Error[] Errors;

    public ErrorList(IEnumerable<Error> errors) {
        ArgumentNullException.ThrowIfNull(errors);
        this.Errors = errors.ToArray();
        if (this.Errors.Length == 0)
            throw new ArgumentException("An error list must contain at least one error.", nameof(errors));
        if (this.Errors.Any(static error => error is null))
            throw new ArgumentException("An error list cannot contain null errors.", nameof(errors));
        ErrorType = this.Errors[0].ErrorType;
        if (this.Errors.Any(error => error.ErrorType != ErrorType))
            throw new ArgumentException("All errors in an error list must have the same category.", nameof(errors));
    }

    public ErrorType ErrorType { get; }
    public int Count => this.Errors.Length;
    public Error this[int index] => this.Errors[index];
    public IEnumerator<Error> GetEnumerator() => ((IEnumerable<Error>)this.Errors).GetEnumerator();
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => this.Errors.GetEnumerator();
}

public enum ErrorType {
    Validation,
    NotFound,
    Unauthorized,
    Forbidden,
    Conflict,
    Failure,
    Critical,
    Unknown
}

public static class Results {
    internal static readonly Error Uninitialized = new(ErrorType.Critical, "An uninitialized result has no outcome.");
    internal static readonly Error MissingError = new(ErrorType.Critical, "A failed result requires an error.");

    public static Result Ok() => new Success();
    public static Result<T> Ok<T>(T value) => new Success<T>(value);
    public static Result Fail(Error? error) => error ?? MissingError;
    public static Result Fail(ErrorList? errors) => errors is null ? Fail(MissingError) : new Result(errors);
    public static Result Validation(String message) => new Error(ErrorType.Validation, message);
    public static Result Conflict(String message) => new Error(ErrorType.Conflict, message);
    public static Result Critical(String message) => new Error(ErrorType.Critical, message);
    public static Result Fail(String message) => new Error(ErrorType.Failure, message);
    public static Result Forbidden(String message) => new Error(ErrorType.Forbidden, message);
    public static Result NotFound(String message) => new Error(ErrorType.NotFound, message);
    public static Result Unauthorized(String message) => new Error(ErrorType.Unauthorized, message);
    public static Result Unknown(String message) => new Error(ErrorType.Unknown, message);
}

public static class ResultFunctionalExtensions {
    private static TResult Capture<TResult>(Func<TResult> callback, Func<Error, TResult> onError) {
        try {
            return callback();
        }
        catch (Exception exception) {
            return onError(new Error(ErrorType.Critical, exception.Message));
        }
    }

    private static async Task<TResult> CaptureAsync<TResult>(Func<Task<TResult>> callback, Func<Error, TResult> onError,
        CancellationToken cancellationToken) {
        cancellationToken.ThrowIfCancellationRequested();
        try {
            return await callback().ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested) {
            return onError(new Error(ErrorType.Critical, exception.Message));
        }
    }

    // MAP: transform a generic success value.
    public static Result<TOut> Map<TIn, TOut>(this Result<TIn> result, Func<TIn, TOut> map) {
        ArgumentNullException.ThrowIfNull(map);
        return result.Value switch {
            Success<TIn> success => Capture(() => Results.Ok(map(success.Value)), static error => error),
            Error error => error,
            ErrorList errors => errors,
            _ => Results.Uninitialized
        };
    }

    // BIND: generic -> generic, generic -> non-generic, non-generic -> generic, non-generic -> non-generic.
    public static Result<TOut> Bind<TIn, TOut>(this Result<TIn> result, Func<TIn, Result<TOut>> bind) {
        ArgumentNullException.ThrowIfNull(bind);
        return result.Value switch {
            Success<TIn> success => Capture(() => bind(success.Value), static error => error),
            Error error => error,
            ErrorList errors => errors,
            _ => Results.Uninitialized
        };
    }

    public static Result Bind<TIn>(this Result<TIn> result, Func<TIn, Result> bind) {
        ArgumentNullException.ThrowIfNull(bind);
        return result.Value switch {
            Success<TIn> success => Capture(() => bind(success.Value), static error => error),
            Error error => error,
            ErrorList errors => errors,
            _ => Results.Uninitialized
        };
    }

    public static Result<TOut> Bind<TOut>(this Result result, Func<Result<TOut>> bind) {
        ArgumentNullException.ThrowIfNull(bind);
        return result.Value switch {
            Success => Capture(bind, static error => error),
            Error error => error,
            ErrorList errors => errors,
            _ => Results.Uninitialized
        };
    }

    public static Result Bind(this Result result, Func<Result> bind) {
        ArgumentNullException.ThrowIfNull(bind);
        return result.Value switch {
            Success => Capture(bind, static error => error),
            Error error => error,
            ErrorList errors => errors,
            _ => Results.Uninitialized
        };
    }

    // MATCH: inspect success and failure. Error is normalized to a one-item read-only list.
    public static TOut Match<TIn, TOut>(this Result<TIn> result,
        Func<TIn, TOut> onSuccess, Func<IReadOnlyList<Error>, TOut> onFailure) {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);
        return result.Value switch {
            Success<TIn> success => onSuccess(success.Value),
            Error error => onFailure([error]),
            ErrorList errors => onFailure(errors),
            _ => onFailure([Results.Uninitialized])
        };
    }

    public static TOut Match<TOut>(this Result result,
        Func<TOut> onSuccess, Func<IReadOnlyList<Error>, TOut> onFailure) {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);
        return result.Value switch {
            Success => onSuccess(),
            Error error => onFailure([error]),
            ErrorList errors => onFailure(errors),
            _ => onFailure([Results.Uninitialized])
        };
    }

    // TAP: execute a success-only side effect, preserving the result.
    public static Result<T> Tap<T>(this Result<T> result, Action<T> action) {
        ArgumentNullException.ThrowIfNull(action);
        return result.Value switch {
            Success<T> success => Capture(() => {
                action(success.Value);
                return result;
            }, static error => error),
            Error or ErrorList => result,
            _ => Results.Uninitialized
        };
    }

    public static Result Tap(this Result result, Action action) {
        ArgumentNullException.ThrowIfNull(action);
        return result.Value switch {
            Success => Capture(() => {
                action();
                return result;
            }, static error => error),
            Error or ErrorList => result,
            _ => Results.Uninitialized
        };
    }

    // ENSURE: turn a successful value into an error when a predicate fails.
    public static Result<T> Ensure<T>(this Result<T> result, Func<T, bool> predicate, Error error) {
        ArgumentNullException.ThrowIfNull(predicate);
        return result.Value switch {
            Success<T> success when predicate(success.Value) => result,
            Success<T> => error ?? Results.MissingError,
            Error => result,
            ErrorList => result,
            _ => Results.Uninitialized
        };
    }

    public static Result Ensure(this Result result, Func<bool> predicate, Error error) {
        ArgumentNullException.ThrowIfNull(predicate);
        return result.Value switch {
            Success when predicate() => result,
            Success => error ?? Results.MissingError,
            Error => result,
            ErrorList => result,
            _ => Results.Uninitialized
        };
    }

    /// <summary>Transforms errors without changing the single-error or error-list case.</summary>
    /// <exception cref="ArgumentException">Mapped errors in a list have different categories or include null entries.</exception>
    public static Result<T> MapError<T>(this Result<T> result, Func<Error, Error> map) {
        ArgumentNullException.ThrowIfNull(map);
        return result.Value switch {
            Success<T> => result,
            Error error => map(error) ?? Results.MissingError,
            ErrorList errors => new ErrorList(errors.Select(map).ToArray()),
            _ => Results.Uninitialized
        };
    }

    /// <inheritdoc cref="MapError{T}(Result{T}, Func{Error, Error})"/>
    public static Result MapError(this Result result, Func<Error, Error> map) {
        ArgumentNullException.ThrowIfNull(map);
        return result.Value switch {
            Success => result,
            Error error => map(error) ?? Results.MissingError,
            ErrorList errors => new ErrorList(errors.Select(map).ToArray()),
            _ => Results.Uninitialized
        };
    }

    // ASYNC OPERATORS: asynchronous delegates on synchronous Result receivers.
    public static async Task<Result<TOut>> MapAsync<TIn, TOut>(this Result<TIn> result,
        Func<TIn, Task<TOut>> map, CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(map);
        cancellationToken.ThrowIfCancellationRequested();
        return result.Value switch {
            Success<TIn> success => await CaptureAsync(async () => Results.Ok(await map(success.Value).ConfigureAwait(false)),
                static error => error, cancellationToken).ConfigureAwait(false),
            Error error => error,
            ErrorList errors => errors,
            _ => Results.Uninitialized
        };
    }

    public static async Task<Result<TOut>> BindAsync<TIn, TOut>(this Result<TIn> result,
        Func<TIn, Task<Result<TOut>>> bind, CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(bind);
        cancellationToken.ThrowIfCancellationRequested();
        return result.Value switch {
            Success<TIn> success => await CaptureAsync(() => bind(success.Value), static error => error, cancellationToken).ConfigureAwait(false),
            Error error => error,
            ErrorList errors => errors,
            _ => Results.Uninitialized
        };
    }

    public static async Task<Result> BindAsync<TIn>(this Result<TIn> result,
        Func<TIn, Task<Result>> bind, CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(bind);
        cancellationToken.ThrowIfCancellationRequested();
        return result.Value switch {
            Success<TIn> success => await CaptureAsync(() => bind(success.Value), static error => error, cancellationToken).ConfigureAwait(false),
            Error error => error,
            ErrorList errors => errors,
            _ => Results.Uninitialized
        };
    }

    public static async Task<Result<TOut>> BindAsync<TOut>(this Result result,
        Func<Task<Result<TOut>>> bind, CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(bind);
        cancellationToken.ThrowIfCancellationRequested();
        return result.Value switch {
            Success => await CaptureAsync(bind, static error => error, cancellationToken).ConfigureAwait(false),
            Error error => error,
            ErrorList errors => errors,
            _ => Results.Uninitialized
        };
    }

    public static async Task<Result> BindAsync(this Result result, Func<Task<Result>> bind, CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(bind);
        cancellationToken.ThrowIfCancellationRequested();
        return result.Value switch {
            Success => await CaptureAsync(bind, static error => error, cancellationToken).ConfigureAwait(false),
            Error error => error,
            ErrorList errors => errors,
            _ => Results.Uninitialized
        };
    }

    public static async Task<TOut> MatchAsync<TIn, TOut>(this Result<TIn> result,
        Func<TIn, Task<TOut>> onSuccess, Func<IReadOnlyList<Error>, Task<TOut>> onFailure) {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);
        return result.Value switch {
            Success<TIn> success => await onSuccess(success.Value).ConfigureAwait(false),
            Error error => await onFailure([error]).ConfigureAwait(false),
            ErrorList errors => await onFailure(errors).ConfigureAwait(false),
            _ => await onFailure([Results.Uninitialized]).ConfigureAwait(false)
        };
    }

    public static async Task<TOut> MatchAsync<TOut>(this Result result,
        Func<Task<TOut>> onSuccess, Func<IReadOnlyList<Error>, Task<TOut>> onFailure) {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);
        return result.Value switch {
            Success => await onSuccess().ConfigureAwait(false),
            Error error => await onFailure([error]).ConfigureAwait(false),
            ErrorList errors => await onFailure(errors).ConfigureAwait(false),
            _ => await onFailure([Results.Uninitialized]).ConfigureAwait(false)
        };
    }

    public static async Task<Result<T>> TapAsync<T>(this Result<T> result, Func<T, Task> action,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(action);
        cancellationToken.ThrowIfCancellationRequested();
        return result.Value switch {
            Success<T> success => await CaptureAsync(async () => {
                await action(success.Value).ConfigureAwait(false);
                return result;
            }, static error => error, cancellationToken).ConfigureAwait(false),
            Error or ErrorList => result,
            _ => Results.Uninitialized
        };
    }

    public static async Task<Result> TapAsync(this Result result, Func<Task> action,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(action);
        cancellationToken.ThrowIfCancellationRequested();
        return result.Value switch {
            Success => await CaptureAsync(async () => {
                await action().ConfigureAwait(false);
                return result;
            }, static error => error, cancellationToken).ConfigureAwait(false),
            Error or ErrorList => result,
            _ => Results.Uninitialized
        };
    }

    public static async Task<Result<T>> EnsureAsync<T>(this Result<T> result,
        Func<T, Task<bool>> predicate, Error error) {
        ArgumentNullException.ThrowIfNull(predicate);
        return result.Value switch {
            Success<T> success => await predicate(success.Value).ConfigureAwait(false) ? result : error ?? Results.MissingError,
            Error => result,
            ErrorList => result,
            _ => Results.Uninitialized
        };
    }

    public static async Task<Result> EnsureAsync(this Result result,
        Func<Task<bool>> predicate, Error error) {
        ArgumentNullException.ThrowIfNull(predicate);
        return result.Value switch {
            Success => await predicate().ConfigureAwait(false) ? result : error ?? Results.MissingError,
            Error => result,
            ErrorList => result,
            _ => Results.Uninitialized
        };
    }

    /// <summary>Transforms errors asynchronously while preserving their order.</summary>
    /// <exception cref="ArgumentException">Mapped errors in a list have different categories or include null entries.</exception>
    public static async Task<Result<T>> MapErrorAsync<T>(this Result<T> result,
        Func<Error, Task<Error>> map) {
        ArgumentNullException.ThrowIfNull(map);
        return result.Value switch {
            Success<T> => result,
            Error error => await map(error).ConfigureAwait(false) ?? Results.MissingError,
            ErrorList errors => await MapErrorListAsync(errors, map).ConfigureAwait(false),
            _ => Results.Uninitialized
        };
    }

    /// <inheritdoc cref="MapErrorAsync{T}(Result{T}, Func{Error, Task{Error}})"/>
    public static async Task<Result> MapErrorAsync(this Result result,
        Func<Error, Task<Error>> map) {
        ArgumentNullException.ThrowIfNull(map);
        return result.Value switch {
            Success => result,
            Error error => await map(error).ConfigureAwait(false) ?? Results.MissingError,
            ErrorList errors => await MapErrorListAsync(errors, map).ConfigureAwait(false),
            _ => Results.Uninitialized
        };
    }

    private static async Task<ErrorList> MapErrorListAsync(ErrorList errors, Func<Error, Task<Error>> map) {
        var mapped = await Task.WhenAll(errors.Select(async error => await map(error).ConfigureAwait(false)))
            .ConfigureAwait(false);
        return new ErrorList(mapped);
    }

    // TASK-BASED CHAINING: allows awaitable pipelines without awaiting at every step.
    public static async Task<Result<TOut>> Map<TIn, TOut>(this Task<Result<TIn>> result,
        Func<TIn, TOut> map, CancellationToken cancellationToken = default)
        => (await WaitForInput(result, cancellationToken).ConfigureAwait(false)).Map(map);

    public static async Task<Result<TOut>> Bind<TIn, TOut>(this Task<Result<TIn>> result,
        Func<TIn, Result<TOut>> bind, CancellationToken cancellationToken = default)
        => (await WaitForInput(result, cancellationToken).ConfigureAwait(false)).Bind(bind);

    public static async Task<Result> Bind<TIn>(this Task<Result<TIn>> result,
        Func<TIn, Result> bind, CancellationToken cancellationToken = default)
        => (await WaitForInput(result, cancellationToken).ConfigureAwait(false)).Bind(bind);

    public static async Task<Result<TOut>> Bind<TOut>(this Task<Result> result,
        Func<Result<TOut>> bind, CancellationToken cancellationToken = default)
        => (await WaitForInput(result, cancellationToken).ConfigureAwait(false)).Bind(bind);

    public static async Task<Result> Bind(this Task<Result> result,
        Func<Result> bind, CancellationToken cancellationToken = default)
        => (await WaitForInput(result, cancellationToken).ConfigureAwait(false)).Bind(bind);

    public static async Task<TOut> Match<TIn, TOut>(this Task<Result<TIn>> result,
        Func<TIn, TOut> onSuccess, Func<IReadOnlyList<Error>, TOut> onFailure, CancellationToken cancellationToken = default)
        => (await WaitForInput(result, cancellationToken).ConfigureAwait(false)).Match(onSuccess, onFailure);

    public static async Task<TOut> Match<TOut>(this Task<Result> result,
        Func<TOut> onSuccess, Func<IReadOnlyList<Error>, TOut> onFailure, CancellationToken cancellationToken = default)
        => (await WaitForInput(result, cancellationToken).ConfigureAwait(false)).Match(onSuccess, onFailure);

    public static async Task<Result<T>> Tap<T>(this Task<Result<T>> result,
        Action<T> action, CancellationToken cancellationToken = default)
        => (await WaitForInput(result, cancellationToken).ConfigureAwait(false)).Tap(action);

    public static async Task<Result> Tap(this Task<Result> result,
        Action action, CancellationToken cancellationToken = default)
        => (await WaitForInput(result, cancellationToken).ConfigureAwait(false)).Tap(action);

    public static async Task<Result<T>> Ensure<T>(this Task<Result<T>> result,
        Func<T, bool> predicate, Error error, CancellationToken cancellationToken = default)
        => (await WaitForInput(result, cancellationToken).ConfigureAwait(false)).Ensure(predicate, error);

    public static async Task<Result> Ensure(this Task<Result> result,
        Func<bool> predicate, Error error, CancellationToken cancellationToken = default)
        => (await WaitForInput(result, cancellationToken).ConfigureAwait(false)).Ensure(predicate, error);

    public static async Task<Result<T>> MapError<T>(this Task<Result<T>> result,
        Func<Error, Error> map, CancellationToken cancellationToken = default)
        => (await WaitForInput(result, cancellationToken).ConfigureAwait(false)).MapError(map);

    public static async Task<Result> MapError(this Task<Result> result,
        Func<Error, Error> map, CancellationToken cancellationToken = default)
        => (await WaitForInput(result, cancellationToken).ConfigureAwait(false)).MapError(map);

    public static async Task<Result<TOut>> MapAsync<TIn, TOut>(this Task<Result<TIn>> result,
        Func<TIn, Task<TOut>> map, CancellationToken cancellationToken = default)
        => await (await WaitForInput(result, cancellationToken).ConfigureAwait(false)).MapAsync(map, cancellationToken).ConfigureAwait(false);

    public static async Task<Result<TOut>> BindAsync<TIn, TOut>(this Task<Result<TIn>> result,
        Func<TIn, Task<Result<TOut>>> bind, CancellationToken cancellationToken = default)
        => await (await WaitForInput(result, cancellationToken).ConfigureAwait(false)).BindAsync(bind, cancellationToken).ConfigureAwait(false);

    public static async Task<Result> BindAsync<TIn>(this Task<Result<TIn>> result,
        Func<TIn, Task<Result>> bind, CancellationToken cancellationToken = default)
        => await (await WaitForInput(result, cancellationToken).ConfigureAwait(false)).BindAsync(bind, cancellationToken).ConfigureAwait(false);

    public static async Task<Result<TOut>> BindAsync<TOut>(this Task<Result> result,
        Func<Task<Result<TOut>>> bind, CancellationToken cancellationToken = default)
        => await (await WaitForInput(result, cancellationToken).ConfigureAwait(false)).BindAsync(bind, cancellationToken).ConfigureAwait(false);

    public static async Task<Result> BindAsync(this Task<Result> result,
        Func<Task<Result>> bind, CancellationToken cancellationToken = default)
        => await (await WaitForInput(result, cancellationToken).ConfigureAwait(false)).BindAsync(bind, cancellationToken).ConfigureAwait(false);

    public static async Task<TOut> MatchAsync<TIn, TOut>(this Task<Result<TIn>> result,
        Func<TIn, Task<TOut>> onSuccess, Func<IReadOnlyList<Error>, Task<TOut>> onFailure, CancellationToken cancellationToken = default)
        => await (await WaitForInput(result, cancellationToken).ConfigureAwait(false)).MatchAsync(onSuccess, onFailure).ConfigureAwait(false);

    public static async Task<TOut> MatchAsync<TOut>(this Task<Result> result,
        Func<Task<TOut>> onSuccess, Func<IReadOnlyList<Error>, Task<TOut>> onFailure, CancellationToken cancellationToken = default)
        => await (await WaitForInput(result, cancellationToken).ConfigureAwait(false)).MatchAsync(onSuccess, onFailure).ConfigureAwait(false);

    public static async Task<Result<T>> TapAsync<T>(this Task<Result<T>> result,
        Func<T, Task> action, CancellationToken cancellationToken = default)
        => await (await WaitForInput(result, cancellationToken).ConfigureAwait(false)).TapAsync(action, cancellationToken).ConfigureAwait(false);

    public static async Task<Result> TapAsync(this Task<Result> result,
        Func<Task> action, CancellationToken cancellationToken = default)
        => await (await WaitForInput(result, cancellationToken).ConfigureAwait(false)).TapAsync(action, cancellationToken).ConfigureAwait(false);

    public static async Task<Result<T>> EnsureAsync<T>(this Task<Result<T>> result,
        Func<T, Task<bool>> predicate, Error error, CancellationToken cancellationToken = default)
        => await (await WaitForInput(result, cancellationToken).ConfigureAwait(false)).EnsureAsync(predicate, error).ConfigureAwait(false);

    public static async Task<Result> EnsureAsync(this Task<Result> result,
        Func<Task<bool>> predicate, Error error, CancellationToken cancellationToken = default)
        => await (await WaitForInput(result, cancellationToken).ConfigureAwait(false)).EnsureAsync(predicate, error).ConfigureAwait(false);

    public static async Task<Result<T>> MapErrorAsync<T>(this Task<Result<T>> result,
        Func<Error, Task<Error>> map, CancellationToken cancellationToken = default)
        => await (await WaitForInput(result, cancellationToken).ConfigureAwait(false)).MapErrorAsync(map).ConfigureAwait(false);

    public static async Task<Result> MapErrorAsync(this Task<Result> result,
        Func<Error, Task<Error>> map, CancellationToken cancellationToken = default)
        => await (await WaitForInput(result, cancellationToken).ConfigureAwait(false)).MapErrorAsync(map).ConfigureAwait(false);

    public static Task<Result<TOut>> MapAsync<TIn, TOut>(this Task<Result<TIn>> result,
        Func<TIn, CancellationToken, Task<TOut>> map, CancellationToken cancellationToken) {
        ArgumentNullException.ThrowIfNull(map);
        return result.MapAsync(value => map(value, cancellationToken), cancellationToken);
    }

    public static Task<Result<TOut>> BindAsync<TIn, TOut>(this Task<Result<TIn>> result,
        Func<TIn, CancellationToken, Task<Result<TOut>>> bind, CancellationToken cancellationToken) {
        ArgumentNullException.ThrowIfNull(bind);
        return result.BindAsync(value => bind(value, cancellationToken), cancellationToken);
    }

    public static Task<Result<T>> TapAsync<T>(this Task<Result<T>> result,
        Func<T, CancellationToken, Task> action, CancellationToken cancellationToken) {
        ArgumentNullException.ThrowIfNull(action);
        return result.TapAsync(value => action(value, cancellationToken), cancellationToken);
    }

    public static Task<TOut> MatchAsync<TIn, TOut>(this Task<Result<TIn>> result,
        Func<TIn, CancellationToken, Task<TOut>> onSuccess,
        Func<IReadOnlyList<Error>, CancellationToken, Task<TOut>> onFailure, CancellationToken cancellationToken) {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);
        return result.MatchAsync(value => onSuccess(value, cancellationToken),
            errors => onFailure(errors, cancellationToken), cancellationToken);
    }

    private static Task<T> WaitForInput<T>(Task<T> input, CancellationToken cancellationToken) {
        cancellationToken.ThrowIfCancellationRequested();
        return input.WaitAsync(cancellationToken);
    }
}
