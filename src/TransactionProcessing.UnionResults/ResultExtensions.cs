namespace TransactionProcessing.UnionResults;

public static class ResultExtensions
{
    public static TResult Match<TResult>(this Result result, Func<TResult> ok, Func<TResult> noContent, Func<Error, TResult> error, Func<ErrorList, TResult> errors) => result switch
    {
        Ok => ok(), NoContent => noContent(), Error e => error(e), ErrorList es => errors(es),
        null => throw new InvalidOperationException("A default Result has no value."),
    };

    public static TResult Match<T, TResult>(this Result<T> result, Func<T, TResult> ok, Func<T, string?, TResult> created, Func<TResult> noContent, Func<Error, TResult> error, Func<ErrorList, TResult> errors) => result switch
    {
        Ok<T> value => ok(value.Value), Created<T> value => created(value.Value, value.Location), NoContent => noContent(), Error e => error(e), ErrorList es => errors(es),
        null => throw new InvalidOperationException("A default Result<T> has no value."),
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
        null => throw new InvalidOperationException("A default Result has no value."),
    };

    public static Result Tap(this Result result, Action action) => result switch
    {
        Ok => TapOk(result, action), NoContent or Error or ErrorList => result,
        null => throw new InvalidOperationException("A default Result has no value."),
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
        null => throw new InvalidOperationException("A default Result<T> has no value."),
    };

    public static Result<T> TapError<T>(this Result<T> result, Action<Error> action) => result switch
    {
        Error e => TapErrorResult(result, e, action), ErrorList es => TapErrorsResult(result, es, action),
        Ok<T> or Created<T> or NoContent => result,
        null => throw new InvalidOperationException("A default Result<T> has no value."),
    };

    public static async Task<TResult> MatchAsync<T, TResult>(this Task<Result<T>> task, Func<T, Task<TResult>> ok, Func<T, string?, Task<TResult>> created, Func<Task<TResult>> noContent, Func<Error, Task<TResult>> error, Func<ErrorList, Task<TResult>> errors, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await (await task.WaitAsync(cancellationToken).ConfigureAwait(false)).Match(ok, created, noContent, error, errors).ConfigureAwait(false);
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
            async errors => { foreach (var error in errors) await action(error, cancellationToken).ConfigureAwait(false); return Result<T>.Fail(errors); }).ConfigureAwait(false);
    }

    private static Result<T> TapOk<T>(Result<T> result, T value, Action<T> action) { action(value); return result; }
    private static Result<T> TapCreated<T>(Result<T> result, T value, Action<T> action) { action(value); return result; }
    private static Result<T> TapErrorResult<T>(Result<T> result, Error error, Action<Error> action) { action(error); return result; }
    private static Result<T> TapErrorsResult<T>(Result<T> result, ErrorList errors, Action<Error> action) { foreach (var error in errors) action(error); return result; }
    private static Result TapOk(Result result, Action action) { action(); return result; }
    private static Result TapErrorResult(Result result, Error error, Action<Error> action) { action(error); return result; }
    private static Result TapErrorsResult(Result result, ErrorList errors, Action<Error> action) { foreach (var error in errors) action(error); return result; }
}
