namespace TransactionProcessing.UnionResults;

public static class ResultExtensions
{
    private const string DefaultResultHasNoValue = "A default Result has no value.";
    private const string DefaultGenericResultHasNoValue = "A default Result<T> has no value.";

    public static bool IsSuccess(this Result result) => result switch
    {
        TransactionProcessing.UnionResults.Ok or TransactionProcessing.UnionResults.NoContent => true,
        Error or ErrorList => false,
        null => throw new InvalidOperationException(DefaultResultHasNoValue),
    };

    public static bool IsFailure(this Result result) => !result.IsSuccess();

    public static bool IsSuccess<T>(this Result<T> result) => result switch
    {
        Ok<T> or Created<T> or NoContent => true,
        Error or ErrorList => false,
        null => throw new InvalidOperationException(DefaultGenericResultHasNoValue),
    };

    public static bool IsFailure<T>(this Result<T> result) => !result.IsSuccess();

    public static T GetValueOrThrow<T>(this Result<T> result) => result switch
    {
        Ok<T> ok => ok.Value,
        Created<T> created => created.Value,
        NoContent => throw new InvalidOperationException("A NoContent result does not contain a value."),
        Error error => throw new InvalidOperationException(error.Message),
        ErrorList errors => throw new InvalidOperationException(string.Join(Environment.NewLine, errors.Select(static error => error.Message))),
        null => throw new InvalidOperationException(DefaultGenericResultHasNoValue),
    };

    public static string? GetFailureMessage(this Result result) => result switch
    {
        Error error => error.Message,
        ErrorList errors => errors[0].Message,
        TransactionProcessing.UnionResults.Ok or TransactionProcessing.UnionResults.NoContent => null,
        null => throw new InvalidOperationException(DefaultResultHasNoValue),
    };

    public static string? GetFailureMessage<T>(this Result<T> result) => result switch
    {
        Error error => error.Message,
        ErrorList errors => errors[0].Message,
        Ok<T> or Created<T> or NoContent => null,
        null => throw new InvalidOperationException(DefaultGenericResultHasNoValue),
    };

    public static Result ToResult<T>(this Result<T> result) => result switch
    {
        Ok<T> => Result.Ok(),
        Created<T> => Result.Ok(),
        NoContent => Result.NoContent(),
        Error error => Result.Fail(error),
        ErrorList errors => Result.Fail(errors),
        null => throw new InvalidOperationException(DefaultGenericResultHasNoValue),
    };

    public static TResult Match<TResult>(this Result result, Func<TResult> ok, Func<TResult> noContent, Func<Error, TResult> error, Func<ErrorList, TResult> errors) => result switch
    {
        Ok => ok(), NoContent => noContent(), Error e => error(e), ErrorList es => errors(es),
        null => throw new InvalidOperationException("A default Result has no value."),
    };

    public static TResult Match<T, TResult>(this Result<T> result, Func<T, TResult> ok, Func<T, string?, TResult> created, Func<TResult> noContent, Func<Error, TResult> error, Func<ErrorList, TResult> errors) => result switch
    {
        Ok<T> value => ok(value.Value), Created<T> value => created(value.Value, value.Location), NoContent => noContent(), Error e => error(e), ErrorList es => errors(es),
        null => throw new InvalidOperationException(DefaultGenericResultHasNoValue),
    };

    public static Result<TResult> Map<T, TResult>(this Result<T> result, Func<T, TResult> map) => result switch
    {
        Ok<T> value => Result<TResult>.Ok(map(value.Value)), Created<T> value => Result<TResult>.Created(map(value.Value), value.Location),
        NoContent => Result<TResult>.NoContent(), Error e => Result<TResult>.Fail(e), ErrorList es => Result<TResult>.Fail(es),
        null => throw new InvalidOperationException("A default Result<T> has no value."),
    };

    public static Result<TResult> Bind<T, TResult>(this Result<T> result, Func<T, Result<TResult>> bind) => result switch
    {
        Ok<T> value => bind(value.Value), Created<T> value => bind(value.Value), NoContent => Result<TResult>.NoContent(),
        Error e => Result<TResult>.Fail(e), ErrorList es => Result<TResult>.Fail(es),
        null => throw new InvalidOperationException("A default Result<T> has no value."),
    };

    public static Result Bind(this Result result, Func<Result> bind) => result switch
    {
        Ok => bind(), NoContent => Result.NoContent(), Error e => Result.Fail(e), ErrorList es => Result.Fail(es),
        null => throw new InvalidOperationException(DefaultResultHasNoValue),
    };

    public static Result Tap(this Result result, Action action) => result switch
    {
        Ok or NoContent => TapOk(result, action), Error or ErrorList => result,
        null => throw new InvalidOperationException(DefaultResultHasNoValue),
    };

    public static Result TapError(this Result result, Action<Error> action) => result switch
    {
        Error e => TapErrorResult(result, e, action), ErrorList es => TapErrorsResult(result, es, action),
        Ok or NoContent => result,
        null => throw new InvalidOperationException("A default Result has no value."),
    };

    public static Result<T> Tap<T>(this Result<T> result, Action<T> action) => result switch
    {
        Ok<T> value => TapOk(result, value.Value, action), Created<T> value => TapCreated(result, value.Value, action),
        NoContent or Error or ErrorList => result,
        null => throw new InvalidOperationException(DefaultGenericResultHasNoValue),
    };

    public static Result<T> TapError<T>(this Result<T> result, Action<Error> action) => result switch
    {
        Error e => TapErrorResult(result, e, action), ErrorList es => TapErrorsResult(result, es, action),
        Ok<T> or Created<T> or NoContent => result,
        null => throw new InvalidOperationException(DefaultGenericResultHasNoValue),
    };

    public static async Task<TResult> MatchAsync<T, TResult>(this Task<Result<T>> task, Func<T, Task<TResult>> ok, Func<T, string?, Task<TResult>> created, Func<Task<TResult>> noContent, Func<Error, Task<TResult>> error, Func<ErrorList, Task<TResult>> errors, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ok);
        ArgumentNullException.ThrowIfNull(created);
        ArgumentNullException.ThrowIfNull(noContent);
        ArgumentNullException.ThrowIfNull(error);
        ArgumentNullException.ThrowIfNull(errors);
        return await task.MatchAsync(
            (value, _) => ok(value),
            (value, location, _) => created(value, location),
            _ => noContent(),
            (value, _) => error(value),
            (value, _) => errors(value),
            cancellationToken).ConfigureAwait(false);
    }

    public static async Task<TResult> MatchAsync<T, TResult>(this Task<Result<T>> task, Func<T, CancellationToken, Task<TResult>> ok, Func<T, string?, CancellationToken, Task<TResult>> created, Func<CancellationToken, Task<TResult>> noContent, Func<Error, CancellationToken, Task<TResult>> error, Func<ErrorList, CancellationToken, Task<TResult>> errors, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(task);
        ArgumentNullException.ThrowIfNull(ok);
        ArgumentNullException.ThrowIfNull(created);
        ArgumentNullException.ThrowIfNull(noContent);
        ArgumentNullException.ThrowIfNull(error);
        ArgumentNullException.ThrowIfNull(errors);
        cancellationToken.ThrowIfCancellationRequested();
        var result = await task.WaitAsync(cancellationToken).ConfigureAwait(false);
        return await result.Match(
            value => ok(value, cancellationToken),
            (value, location) => created(value, location, cancellationToken),
            () => noContent(cancellationToken),
            value => error(value, cancellationToken),
            value => errors(value, cancellationToken)).ConfigureAwait(false);
    }

    public static async Task<Result<TResult>> MapAsync<T, TResult>(this Task<Result<T>> task, Func<T, CancellationToken, Task<TResult>> map, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var result = await task.WaitAsync(cancellationToken).ConfigureAwait(false);
        return await result.Match(
            async value => Result<TResult>.Ok(await map(value, cancellationToken).ConfigureAwait(false)),
            async (value, location) => Result<TResult>.Created(await map(value, cancellationToken).ConfigureAwait(false), location),
            () => Task.FromResult(Result<TResult>.NoContent()),
            error => Task.FromResult(Result<TResult>.Fail(error)),
            errors => Task.FromResult(Result<TResult>.Fail(errors))).ConfigureAwait(false);
    }

    public static async Task<Result<TResult>> BindAsync<T, TResult>(this Task<Result<T>> task, Func<T, CancellationToken, Task<Result<TResult>>> bind, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var result = await task.WaitAsync(cancellationToken).ConfigureAwait(false);
        return await result.Match(
            value => bind(value, cancellationToken),
            (value, _) => bind(value, cancellationToken),
            () => Task.FromResult(Result<TResult>.NoContent()),
            error => Task.FromResult(Result<TResult>.Fail(error)),
            errors => Task.FromResult(Result<TResult>.Fail(errors))).ConfigureAwait(false);
    }

    public static async Task<Result<T>> TapAsync<T>(this Task<Result<T>> task, Func<T, CancellationToken, Task> action, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var result = await task.WaitAsync(cancellationToken).ConfigureAwait(false);
        return await result.Match(
            async value => { await action(value, cancellationToken).ConfigureAwait(false); return Result<T>.Ok(value); },
            async (value, location) => { await action(value, cancellationToken).ConfigureAwait(false); return Result<T>.Created(value, location); },
            () => Task.FromResult(Result<T>.NoContent()),
            error => Task.FromResult(Result<T>.Fail(error)),
            errors => Task.FromResult(Result<T>.Fail(errors))).ConfigureAwait(false);
    }

    public static async Task<Result<T>> TapErrorAsync<T>(this Task<Result<T>> task, Func<Error, CancellationToken, Task> action, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var result = await task.WaitAsync(cancellationToken).ConfigureAwait(false);
        return await result.Match(
            value => Task.FromResult(Result<T>.Ok(value)),
            (value, location) => Task.FromResult(Result<T>.Created(value, location)),
            () => Task.FromResult(Result<T>.NoContent()),
            async error => { await action(error, cancellationToken).ConfigureAwait(false); return Result<T>.Fail(error); },
            async errors =>
            {
                foreach (var error in errors)
                {
                    await action(error, cancellationToken).ConfigureAwait(false);
                }

                return Result<T>.Fail(errors);
            }).ConfigureAwait(false);
    }

    private static Result<T> TapOk<T>(Result<T> result, T value, Action<T> action) { action(value); return result; }
    private static Result<T> TapCreated<T>(Result<T> result, T value, Action<T> action) { action(value); return result; }
    private static Result<T> TapErrorResult<T>(Result<T> result, Error error, Action<Error> action) { action(error); return result; }
    private static Result<T> TapErrorsResult<T>(Result<T> result, ErrorList errors, Action<Error> action)
    {
        foreach (var error in errors)
        {
            action(error);
        }

        return result;
    }
    private static Result TapOk(Result result, Action action) { action(); return result; }
    private static Result TapErrorResult(Result result, Error error, Action<Error> action) { action(error); return result; }
    private static Result TapErrorsResult(Result result, ErrorList errors, Action<Error> action)
    {
        foreach (var error in errors)
        {
            action(error);
        }

        return result;
    }
}
