namespace TransactionProcessing.UnionResults;

public static class ResultExtensions {
        public static bool IsSuccess(this Result result)
            => result.Value is Success;

        public static bool IsSuccess<T>(this Result<T> result)
            => result.Value is Success<T>;

        public static IReadOnlyList<Error> GetErrors(this Result result)
            => ExtractErrors(result.Value);

        public static IReadOnlyList<Error> GetErrors<T>(this Result<T> result)
            => ExtractErrors(result.Value);

        public static string? GetErrorMessage(this Result result)
            => FormatErrors(result.GetErrors());

        public static string? GetErrorMessage<T>(this Result<T> result)
            => FormatErrors(result.GetErrors());

        private static IReadOnlyList<Error> ExtractErrors(object? value)
            => value switch {
                   Error error => [error],
                   ErrorList errors => errors,
                   null => [Results.Uninitialized],
                   _ => []
               };

        private static string? FormatErrors(IReadOnlyList<Error> errors)
            => errors.Count == 0
                ? null
                : string.Join("; ", errors.Select(e => e.Message));


    /*public static bool IsFailure(this Result result) => result.IsFailed;

    public static bool IsSuccess<T>(this Result<T> result) => result.IsSuccess;

    public static bool IsFailure<T>(this Result<T> result) => result.IsFailed;

    public static string? GetFailureMessage(this Result result) => result.FailureMessage;

    public static string? GetFailureMessage<T>(this Result<T> result) => result.GetFailureMessage();

    /// <summary>Gets the payload or the errors to propagate when the caller requires a payload.</summary>
    /// <remarks>A <see cref="NoContent"/> result produces a critical error.</remarks>
    public static bool TryGetValueOrFailure<T>(
        this Result<T> result,
        [MaybeNullWhen(false)] out T value,
        [NotNullWhen(false)] out ErrorList? failure)
    {
        if (result.TryGetValue(out value))
        {
            failure = null;
            return true;
        }

        failure = result.IsFailed
            ? ResultHelpers.CreateFailure(result)
            : new ErrorList([Error.Critical("The result contained no value.")]);

        return false;
    }

    public static Result ToResult<T>(this Result<T> result) => result.ToResult();

    public static Result<TResult> Match<TResult>(this Result result, Func<TResult> ok, Func<TResult> noContent, Func<Error, TResult> error, Func<ErrorList, TResult> errors) =>
        result.Match(ok, noContent, error, errors);

    public static Result<TResult> Match<T, TResult>(this Result<T> result, Func<T, TResult> ok, Func<T, string?, TResult> created, Func<TResult> noContent, Func<Error, TResult> error, Func<ErrorList, TResult> errors) =>
        result.Match(ok, created, noContent, error, errors);

    public static Result<TResult> Map<T, TResult>(this Result<T> result, Func<T, TResult> map) => result.Map(map);

    public static Result<TResult> Bind<T, TResult>(this Result<T> result, Func<T, Result<TResult>> bind) => result.Bind(bind);

    public static Result Bind(this Result result, Func<Result> bind) => result.BindCore(bind);

    public static Result Tap(this Result result, Action action) => result.TapCore(action);

    public static Result TapError(this Result result, Action<Error> action) => result.TapErrorCore(action);

    public static Result<T> Tap<T>(this Result<T> result, Action<T> action) => result.Tap(action);

    public static Result<T> TapError<T>(this Result<T> result, Action<Error> action) => result.TapError(action);

    public static Task<Result<TResult>> MatchAsync<T, TResult>(this Task<Result<T>> task, Func<T, Task<TResult>> ok, Func<T, string?, Task<TResult>> created, Func<Task<TResult>> noContent, Func<Error, Task<TResult>> error, Func<ErrorList, Task<TResult>> errors, CancellationToken cancellationToken = default) =>
        task.MatchAsync(
            (value, _) => ok(value),
            (value, location, _) => created(value, location),
            _ => noContent(),
            (value, _) => error(value),
            (value, _) => errors(value),
            cancellationToken);

    public static Task<Result<TResult>> MatchAsync<T, TResult>(this Task<Result<T>> task, Func<T, CancellationToken, Task<TResult>> ok, Func<T, string?, CancellationToken, Task<TResult>> created, Func<CancellationToken, Task<TResult>> noContent, Func<Error, CancellationToken, Task<TResult>> error, Func<ErrorList, CancellationToken, Task<TResult>> errors, CancellationToken cancellationToken = default) =>
        ApplyAsync(task, async result =>
        {
            var value = await result.MatchCore(
                value => ok(value, cancellationToken),
                (value, location) => created(value, location, cancellationToken),
                () => noContent(cancellationToken),
                value => error(value, cancellationToken),
                value => errors(value, cancellationToken)).ConfigureAwait(false);

            return Result<TResult>.Ok(value);
        }, cancellationToken);

    public static Task<Result<TResult>> MapAsync<T, TResult>(this Task<Result<T>> task, Func<T, CancellationToken, Task<TResult>> map, CancellationToken cancellationToken = default) =>
        ApplyAsync(task, result => result.MatchCore(
            async value => Result<TResult>.Ok(await map(value, cancellationToken).ConfigureAwait(false)),
            async (value, location) => Result<TResult>.Created(await map(value, cancellationToken).ConfigureAwait(false), location),
            () => Task.FromResult(Result<TResult>.NoContent()),
            error => Task.FromResult(Result<TResult>.Fail(error)),
            errors => Task.FromResult(Result<TResult>.Fail(errors))), cancellationToken);

    public static Task<Result<TResult>> BindAsync<T, TResult>(this Task<Result<T>> task, Func<T, CancellationToken, Task<Result<TResult>>> bind, CancellationToken cancellationToken = default) =>
        ApplyAsync(task, result => result.MatchCore(
            value => bind(value, cancellationToken),
            (value, _) => bind(value, cancellationToken),
            () => Task.FromResult(Result<TResult>.NoContent()),
            error => Task.FromResult(Result<TResult>.Fail(error)),
            errors => Task.FromResult(Result<TResult>.Fail(errors))), cancellationToken);

    public static Task<Result<T>> TapAsync<T>(this Task<Result<T>> task, Func<T, CancellationToken, Task> action, CancellationToken cancellationToken = default) =>
        ApplyAsync(task, result =>
        {
            async Task<Result<T>> TapValueAsync(T value)
            {
                await action(value, cancellationToken).ConfigureAwait(false);
                return result;
            }

            return result.MatchCore(
                TapValueAsync,
                (value, _) => TapValueAsync(value),
                () => Task.FromResult(result),
                error => Task.FromResult(Result<T>.Fail(error)),
                errors => Task.FromResult(Result<T>.Fail(errors)));
        }, cancellationToken);

    public static Task<Result<T>> TapErrorAsync<T>(this Task<Result<T>> task, Func<Error, CancellationToken, Task> action, CancellationToken cancellationToken = default) =>
        ApplyAsync(task, result => result.MatchCore(
            _ => Task.FromResult(result),
            (_, _) => Task.FromResult(result),
            () => Task.FromResult(result),
            async error =>
            {
                await action(error, cancellationToken).ConfigureAwait(false);
                return Result<T>.Fail(error);
            },
            async errors =>
            {
                foreach (var error in errors)
                    await action(error, cancellationToken).ConfigureAwait(false);

                return Result<T>.Fail(errors);
            }), cancellationToken);

    private static Task<Result<TResult>> ApplyAsync<T, TResult>(
        Task<Result<T>> task, Func<Result<T>, Task<Result<TResult>>> action, CancellationToken cancellationToken) =>
        Result<TResult>.CaptureAsync(async () =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = await task.WaitAsync(cancellationToken).ConfigureAwait(false);
            return await action(result).ConfigureAwait(false);
        }, cancellationToken);*/
}
