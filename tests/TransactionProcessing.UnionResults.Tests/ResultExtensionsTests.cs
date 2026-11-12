using TransactionProcessing.UnionResults;
using Xunit;

namespace TransactionProcessing.UnionResults.Tests;

public sealed class ResultExtensionsTests
{
    private const string MissingMessage = "missing";
    private const string NoContentLabel = "no content";
    private const string EmptyLabel = "empty";
    private const string ItemTwoLocation = "/items/2";

    private static readonly Error Missing = Error.NotFound(MissingMessage);
    private static readonly ErrorList MissingErrors = new([Missing, Error.NotFound("also missing")]);

    [Fact]
    public void Non_generic_helpers_handle_success_failure_and_default_results()
    {
        Assert.True(Result.Ok().IsSuccess());
        Assert.False(Result.Ok().IsFailure());
        Assert.True(Result.NoContent().IsSuccess());
        Assert.False(Result.Fail(Missing).IsSuccess());
        Assert.True(Result.Fail(MissingErrors).IsFailure());
        Assert.Equal(MissingMessage, Result.Fail(MissingErrors).GetFailureMessage());
        Assert.Equal(MissingMessage, Result.Fail(Missing).GetFailureMessage());
        Assert.Null(Result.Ok().GetFailureMessage());
        Assert.Null(Result.NoContent().GetFailureMessage());
        Assert.Throws<InvalidOperationException>(() => default(Result).IsSuccess());
        Assert.Throws<InvalidOperationException>(() => default(Result).GetFailureMessage());
    }

    [Fact]
    public void Generic_helpers_handle_success_failure_values_and_default_results()
    {
        Result<int> success = Result<int>.Ok(42);
        Result<int> created = Result<int>.Created(43, "/items/43");
        Result<int> noContent = Result<int>.NoContent();
        Result<int> failure = Result<int>.Fail(Missing);
        Result<int> failures = Result<int>.Fail(MissingErrors);

        Assert.True(success.IsSuccess());
        Assert.True(created.IsSuccess());
        Assert.False(noContent.IsFailure());
        Assert.False(failure.IsSuccess());
        Assert.True(failures.IsFailure());
        Assert.Equal(42, success.GetValueOrThrow());
        Assert.Equal(43, created.GetValueOrThrow());
        Assert.Equal(MissingMessage, failures.GetFailureMessage());
        Assert.Null(success.GetFailureMessage());
        Assert.Null(noContent.GetFailureMessage());
        Assert.Throws<InvalidOperationException>(() => noContent.GetValueOrThrow());
        Assert.Throws<InvalidOperationException>(() => failure.GetValueOrThrow());
        Assert.Equal(MissingMessage + Environment.NewLine + "also missing", Assert.Throws<InvalidOperationException>(() => failures.GetValueOrThrow()).Message);
        Assert.Throws<InvalidOperationException>(() => default(Result<int>).IsSuccess());
        Assert.Throws<InvalidOperationException>(() => default(Result<int>).GetFailureMessage());
    }

    [Fact]
    public void To_result_projects_each_generic_case()
    {
        Assert.Equal(ResultKind.Ok, Result<int>.Ok(1).ToResult().GetOutcome().Kind);
        Assert.Equal(ResultKind.Ok, Result<int>.Created(1, "/items/1").ToResult().GetOutcome().Kind);
        Assert.Equal(ResultKind.NoContent, Result<int>.NoContent().ToResult().GetOutcome().Kind);
        Assert.Equal(ErrorType.NotFound, Result<int>.Fail(Missing).ToResult().Errors[0].ErrorType);
        Assert.Equal(2, Result<int>.Fail(MissingErrors).ToResult().Errors.Count);
        Assert.Throws<InvalidOperationException>(() => default(Result<int>).ToResult());
    }

    [Fact]
    public void Match_dispatches_to_each_non_generic_case()
    {
        static string Match(Result result) => result.Match(
            () => "ok",
            () => NoContentLabel,
            error => error.Message,
            errors => $"{errors.Count} errors");

        Assert.Equal("ok", Match(Result.Ok()));
        Assert.Equal(NoContentLabel, Match(Result.NoContent()));
        Assert.Equal(MissingMessage, Match(Result.Fail(Missing)));
        Assert.Equal("2 errors", Match(Result.Fail(MissingErrors)));
        Assert.Throws<InvalidOperationException>(() => Match(default));
    }

    [Fact]
    public void Match_dispatches_to_each_generic_case()
    {
        static string Match(Result<int> result) => result.Match(
            value => $"ok:{value}",
            (value, location) => $"created:{value}:{location}",
            () => NoContentLabel,
            error => error.Message,
            errors => $"{errors.Count} errors");

        Assert.Equal("ok:1", Match(Result<int>.Ok(1)));
        Assert.Equal("created:2:/items/2", Match(Result<int>.Created(2, ItemTwoLocation)));
        Assert.Equal("created:2:", Match(Result<int>.Created(2)));
        Assert.Equal(NoContentLabel, Match(Result<int>.NoContent()));
        Assert.Equal(MissingMessage, Match(Result<int>.Fail(Missing)));
        Assert.Equal("2 errors", Match(Result<int>.Fail(MissingErrors)));
        Assert.Throws<InvalidOperationException>(() => Match(default));
    }

    [Fact]
    public void Map_and_bind_preserve_generic_result_cases()
    {
        var mapped = Result<int>.Ok(2).Map(value => value * 2);
        Assert.Equal(4, mapped.Data);
        var mappedCreated = Result<int>.Created(3, "/items/3").Map(value => value * 2);
        Assert.Equal(6, mappedCreated.Data);
        Assert.Equal("/items/3", Assert.IsType<Created<int>>(mappedCreated.Value).Location);
        Assert.Equal(ResultKind.NoContent, Result<int>.NoContent().Map(value => value * 2).GetOutcome().Kind);
        Assert.Same(Missing, Result<int>.Fail(Missing).Map(value => value * 2).Errors[0]);
        Assert.Equal(2, Result<int>.Fail(MissingErrors).Map(value => value * 2).Errors.Count);

        Assert.Equal("3", Result<int>.Ok(2).Bind(value => Result<string>.Ok((value + 1).ToString())).Data);
        Assert.Equal("3", Result<int>.Created(2, ItemTwoLocation).Bind(value => Result<string>.Ok((value + 1).ToString())).Data);
        Assert.Equal(ResultKind.NoContent, Result<int>.NoContent().Bind(value => Result<string>.Ok(value.ToString())).GetOutcome().Kind);
        Assert.Same(Missing, Result<int>.Fail(Missing).Bind(value => Result<string>.Ok(value.ToString())).Errors[0]);
        Assert.Equal(2, Result<int>.Fail(MissingErrors).Bind(value => Result<string>.Ok(value.ToString())).Errors.Count);
        Assert.Throws<InvalidOperationException>(() => default(Result<int>).Map(value => value).GetOutcome());
        Assert.Throws<InvalidOperationException>(() => default(Result<int>).Bind(value => Result<int>.Ok(value)).GetOutcome());
    }

    [Fact]
    public void Non_generic_bind_preserves_no_content_and_failures()
    {
        Assert.Equal(ResultKind.Ok, Result.Ok().Bind(() => Result.Ok()).GetOutcome().Kind);
        Assert.Equal(ResultKind.NoContent, Result.NoContent().Bind(() => Result.Ok()).GetOutcome().Kind);
        Assert.Same(Missing, Result.Fail(Missing).Bind(() => Result.Ok()).Errors[0]);
        Assert.Equal(2, Result.Fail(MissingErrors).Bind(() => Result.Ok()).Errors.Count);
        Assert.Throws<InvalidOperationException>(() => default(Result).Bind(() => Result.Ok()));
    }

    [Fact]
    public void Tap_and_tap_error_invoke_only_the_matching_callbacks()
    {
        var successCount = 0;
        var errorCount = 0;
        var noContent = Result.NoContent();
        var failure = Result.Fail(MissingErrors);

        var success = Result.Ok();
        Assert.Same(success.Value, success.Tap(() => successCount++).Value);
        noContent.Tap(() => successCount++);
        failure.Tap(() => successCount++);
        Result.Ok().TapError(_ => errorCount++);
        noContent.TapError(_ => errorCount++);
        Result.Fail(Missing).TapError(_ => errorCount++);
        failure.TapError(_ => errorCount++);

        Assert.Equal(2, successCount);
        Assert.Equal(3, errorCount);
        Assert.Throws<InvalidOperationException>(() => default(Result).Tap(() => { }));
        Assert.Throws<InvalidOperationException>(() => default(Result).TapError(_ => { }));
    }

    [Fact]
    public void Error_list_supports_generic_and_non_generic_enumeration()
    {
        var errors = new ErrorList([Missing, Error.NotFound("also missing")]);

        var enumerated = new List<Error>();
        System.Collections.IEnumerable nonGenericErrors = errors;
        var enumerator = nonGenericErrors.GetEnumerator();
        while (enumerator.MoveNext()) enumerated.Add(Assert.IsType<Error>(enumerator.Current));
        Assert.Equal(errors.ToArray(), enumerated);
    }

    [Fact]
    public void Generic_tap_and_tap_error_invoke_callbacks_for_values_and_errors()
    {
        var values = new List<int>();
        var errors = new List<Error>();
        Result<int>.Ok(1).Tap(values.Add);
        Result<int>.Created(2, ItemTwoLocation).Tap(values.Add);
        Result<int>.NoContent().Tap(values.Add);
        Result<int>.Fail(Missing).Tap(values.Add);
        Result<int>.Ok(3).TapError(errors.Add);
        Result<int>.Created(4).TapError(errors.Add);
        Result<int>.NoContent().TapError(errors.Add);
        Result<int>.Fail(Missing).TapError(errors.Add);
        Result<int>.Fail(MissingErrors).TapError(errors.Add);

        Assert.Equal(new[] { 1, 2 }, values);
        Assert.Equal(new[] { Missing, Missing, MissingErrors[1] }, errors);
        Assert.Throws<InvalidOperationException>(() => default(Result<int>).Tap(_ => { }));
        Assert.Throws<InvalidOperationException>(() => default(Result<int>).TapError(_ => { }));
    }

    [Fact]
    public async Task Async_match_map_bind_tap_and_tap_error_cover_success_failure_and_cancellation()
    {
        var token = new CancellationTokenSource().Token;
        var matchOk = await Task.FromResult(Result<int>.Ok(2)).MatchAsync(
            value => Task.FromResult($"ok:{value}"),
            (value, location) => Task.FromResult($"created:{value}:{location}"),
            () => Task.FromResult(EmptyLabel),
            error => Task.FromResult(error.Message),
            errors => Task.FromResult($"{errors.Count} errors"), token);
        Assert.Equal("ok:2", matchOk);
        Assert.Equal("created:2:/items/2", await Task.FromResult(Result<int>.Created(2, ItemTwoLocation)).MatchAsync(
            value => Task.FromResult($"ok:{value}"),
            (value, location) => Task.FromResult($"created:{value}:{location}"),
            () => Task.FromResult(EmptyLabel),
            error => Task.FromResult(error.Message),
            errors => Task.FromResult($"{errors.Count} errors"), token));
        Assert.Equal(EmptyLabel, await Task.FromResult(Result<int>.NoContent()).MatchAsync(
            value => Task.FromResult($"ok:{value}"),
            (value, location) => Task.FromResult($"created:{value}:{location}"),
            () => Task.FromResult(EmptyLabel),
            error => Task.FromResult(error.Message),
            errors => Task.FromResult($"{errors.Count} errors"), token));
        Assert.Equal(MissingMessage, await Task.FromResult(Result<int>.Fail(Missing)).MatchAsync(
            value => Task.FromResult($"ok:{value}"),
            (value, location) => Task.FromResult($"created:{value}:{location}"),
            () => Task.FromResult(EmptyLabel),
            error => Task.FromResult(error.Message),
            errors => Task.FromResult($"{errors.Count} errors"), token));
        Assert.Equal("2 errors", await Task.FromResult(Result<int>.Fail(MissingErrors)).MatchAsync(
            value => Task.FromResult($"ok:{value}"),
            (value, location) => Task.FromResult($"created:{value}:{location}"),
            () => Task.FromResult(EmptyLabel),
            error => Task.FromResult(error.Message),
            errors => Task.FromResult($"{errors.Count} errors"), token));

        Assert.Equal(4, (await Task.FromResult(Result<int>.Ok(2)).MapAsync((value, _) => Task.FromResult(value * 2), token)).Data);
        Assert.Equal(ItemTwoLocation, Assert.IsType<Created<int>>((await Task.FromResult(Result<int>.Created(2, ItemTwoLocation)).MapAsync((value, _) => Task.FromResult(value * 2), token)).Value).Location);
        Assert.Equal(ResultKind.NoContent, (await Task.FromResult(Result<int>.NoContent()).MapAsync((value, _) => Task.FromResult(value * 2), token)).GetOutcome().Kind);
        Assert.Same(Missing, (await Task.FromResult(Result<int>.Fail(Missing)).MapAsync((value, _) => Task.FromResult(value * 2), token)).Errors[0]);
        Assert.Equal(2, (await Task.FromResult(Result<int>.Fail(MissingErrors)).MapAsync((value, _) => Task.FromResult(value * 2), token)).Errors.Count);

        Assert.Equal("2", (await Task.FromResult(Result<int>.Ok(2)).BindAsync((value, _) => Task.FromResult(Result<string>.Ok(value.ToString())), token)).Data);
        Assert.Equal("2", (await Task.FromResult(Result<int>.Created(2, ItemTwoLocation)).BindAsync((value, _) => Task.FromResult(Result<string>.Ok(value.ToString())), token)).Data);
        Assert.Equal(ResultKind.NoContent, (await Task.FromResult(Result<int>.NoContent()).BindAsync((value, _) => Task.FromResult(Result<string>.Ok(value.ToString())), token)).GetOutcome().Kind);
        Assert.Same(Missing, (await Task.FromResult(Result<int>.Fail(Missing)).BindAsync((value, _) => Task.FromResult(Result<string>.Ok(value.ToString())), token)).Errors[0]);
        Assert.Equal(2, (await Task.FromResult(Result<int>.Fail(MissingErrors)).BindAsync((value, _) => Task.FromResult(Result<string>.Ok(value.ToString())), token)).Errors.Count);

        var tappedValues = new List<int>();
        await Task.FromResult(Result<int>.Ok(1)).TapAsync((value, _) => { tappedValues.Add(value); return Task.CompletedTask; }, token);
        await Task.FromResult(Result<int>.Created(2, ItemTwoLocation)).TapAsync((value, _) => { tappedValues.Add(value); return Task.CompletedTask; }, token);
        await Task.FromResult(Result<int>.NoContent()).TapAsync((value, _) => { tappedValues.Add(value); return Task.CompletedTask; }, token);
        await Task.FromResult(Result<int>.Fail(Missing)).TapAsync((value, _) => { tappedValues.Add(value); return Task.CompletedTask; }, token);
        Assert.Equal(new[] { 1, 2 }, tappedValues);

        var tappedErrors = new List<Error>();
        await Task.FromResult(Result<int>.Ok(1)).TapErrorAsync((error, _) => { tappedErrors.Add(error); return Task.CompletedTask; }, token);
        await Task.FromResult(Result<int>.Created(2)).TapErrorAsync((error, _) => { tappedErrors.Add(error); return Task.CompletedTask; }, token);
        await Task.FromResult(Result<int>.NoContent()).TapErrorAsync((error, _) => { tappedErrors.Add(error); return Task.CompletedTask; }, token);
        await Task.FromResult(Result<int>.Fail(Missing)).TapErrorAsync((error, _) => { tappedErrors.Add(error); return Task.CompletedTask; }, token);
        await Task.FromResult(Result<int>.Fail(MissingErrors)).TapErrorAsync((error, _) => { tappedErrors.Add(error); return Task.CompletedTask; }, token);
        Assert.Equal(new[] { Missing, Missing, MissingErrors[1] }, tappedErrors);

        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Task.FromResult(Result<int>.Ok(1)).MatchAsync(
            value => Task.FromResult(value), (value, _) => Task.FromResult(value), () => Task.FromResult(0),
            _ => Task.FromResult(0), _ => Task.FromResult(0), cancelled.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Task.FromResult(Result<int>.Ok(1)).MapAsync((value, _) => Task.FromResult(value), cancelled.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Task.FromResult(Result<int>.Ok(1)).BindAsync((value, _) => Task.FromResult(Result<int>.Ok(value)), cancelled.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Task.FromResult(Result<int>.Ok(1)).TapAsync((_, _) => Task.CompletedTask, cancelled.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Task.FromResult(Result<int>.Ok(1)).TapErrorAsync((_, _) => Task.CompletedTask, cancelled.Token));
    }

    [Fact]
    public async Task Match_async_passes_cancellation_to_callbacks_and_propagates_callback_failures()
    {
        using var cancellation = new CancellationTokenSource();
        var receivedToken = default(CancellationToken);
        var actual = await Task.FromResult(Result<int>.Ok(7)).MatchAsync(
            (value, token) => { receivedToken = token; return Task.FromResult(value); },
            (value, _, token) => { receivedToken = token; return Task.FromResult(value); },
            token => { receivedToken = token; return Task.FromResult(0); },
            (_, token) => { receivedToken = token; return Task.FromResult(0); },
            (_, token) => { receivedToken = token; return Task.FromResult(0); },
            cancellation.Token);

        Assert.Equal(7, actual);
        Assert.Equal(cancellation.Token, receivedToken);

        using var inFlightCancellation = new CancellationTokenSource();
        var inFlightMatch = Task.FromResult(Result<int>.Ok(1)).MatchAsync(
            async (_, token) => { await Task.Delay(Timeout.Infinite, token).ConfigureAwait(false); return 0; },
            (_, _, _) => Task.FromResult(0),
            _ => Task.FromResult(0),
            (_, _) => Task.FromResult(0),
            (_, _) => Task.FromResult(0),
            inFlightCancellation.Token);
        inFlightCancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => inFlightMatch);

        await Assert.ThrowsAsync<InvalidOperationException>(() => Task.FromResult(Result<int>.Ok(1)).MatchAsync(
            (_, _) => Task.FromException<int>(new InvalidOperationException("callback failed")),
            (_, _, _) => Task.FromResult(0),
            _ => Task.FromResult(0),
            (_, _) => Task.FromResult(0),
            (_, _) => Task.FromResult(0), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Match_async_cancels_while_waiting_for_the_input_result()
    {
        var pending = new TaskCompletionSource<Result<int>>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var cancellation = new CancellationTokenSource();
        var match = pending.Task.MatchAsync(
            value => Task.FromResult(value),
            (value, _) => Task.FromResult(value),
            () => Task.FromResult(0),
            _ => Task.FromResult(0),
            _ => Task.FromResult(0),
            cancellation.Token);

        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => match);
    }

    [Fact]
    public async Task Async_transform_and_tap_methods_propagate_callback_failures()
    {
        var failure = new InvalidOperationException("callback failed");
        Func<int, CancellationToken, Task<int>> map = (_, _) => Task.FromException<int>(failure);
        Func<int, CancellationToken, Task<Result<int>>> bind = (_, _) => Task.FromException<Result<int>>(failure);
        Func<int, CancellationToken, Task> tap = (_, _) => Task.FromException(failure);
        Func<Error, CancellationToken, Task> tapError = (_, _) => Task.FromException(failure);

        await Assert.ThrowsAsync<InvalidOperationException>(() => Task.FromResult(Result<int>.Ok(1)).MapAsync(map, TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<InvalidOperationException>(() => Task.FromResult(Result<int>.Ok(1)).BindAsync(bind, TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<InvalidOperationException>(() => Task.FromResult(Result<int>.Ok(1)).TapAsync(tap, TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<InvalidOperationException>(() => Task.FromResult(Result<int>.Fail(Missing)).TapErrorAsync(tapError, TestContext.Current.CancellationToken));
    }
}
