using Xunit;

namespace TransactionProcessing.UnionResults.Tests;

public sealed class ResultFunctionalExtensionsTests
{
    private static readonly Error Missing = new(ErrorType.NotFound, "Item not found");
    private static readonly Error Invalid = new(ErrorType.Validation, "Value must be positive");
    private static readonly ErrorList ValidationErrors = new([Invalid, new Error(ErrorType.Validation, "Name is required")]);

    [Fact]
    public void Map_transforms_a_success_and_preserves_both_failure_cases()
    {
        Result<int> single = Missing;
        Result<int> multiple = ValidationErrors;

        var mapped = Results.Ok(21).Map(value => $"{value * 2}");

        Assert.Equal("42", Assert.IsType<Success<string>>(mapped.Value).Value);
        Assert.Same(Missing, single.Map(Unexpected<int, string>).Value);
        Assert.Same(ValidationErrors, multiple.Map(Unexpected<int, string>).Value);
    }

    [Fact]
    public void Bind_passes_values_to_callbacks_and_returns_the_callbacks_result()
    {
        var success = Results.Ok();
        var generic = Results.Ok(42);

        Assert.Same(Missing, success.Bind(() => Results.Fail(Missing)).Value);
        Assert.Equal(42, Assert.IsType<Success<int>>(success.Bind(() => generic).Value).Value);
        Assert.Equal("42", Assert.IsType<Success<string>>(generic.Bind(value => Results.Ok(value.ToString())).Value).Value);
        Assert.Same(ValidationErrors, generic.Bind(value => value == 42 ? Results.Fail(ValidationErrors) : Results.Ok()).Value);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Bind_short_circuits_both_failure_cases(bool useErrorList)
    {
        Result result = useErrorList ? Results.Fail(ValidationErrors) : Results.Fail(Missing);
        Result<int> generic = useErrorList ? (Result<int>)ValidationErrors : Missing;

        Assert.Same(result.Value, result.Bind(Unexpected<Result>).Value);
        Assert.Same(result.Value, result.Bind(Unexpected<Result<string>>).Value);
        Assert.Same(generic.Value, generic.Bind(Unexpected<int, Result>).Value);
        Assert.Same(generic.Value, generic.Bind(Unexpected<int, Result<string>>).Value);
    }

    [Fact]
    public void Match_dispatches_success_and_normalizes_both_failure_cases()
    {
        Result<int> single = Missing;
        Result<int> multiple = ValidationErrors;

        Assert.Equal(42, Results.Ok(42).Match(value => value, Unexpected<IReadOnlyList<Error>, int>));
        Assert.Equal(42, Results.Ok().Match(() => 42, Unexpected<IReadOnlyList<Error>, int>));
        Assert.Same(Missing, Assert.Single(single.Match(Unexpected<int, IReadOnlyList<Error>>, errors => errors)));
        Assert.Same(Missing, Assert.Single(Results.Fail(Missing).Match(Unexpected<IReadOnlyList<Error>>, errors => errors)));
        Assert.Same(ValidationErrors, multiple.Match(Unexpected<int, IReadOnlyList<Error>>, errors => errors));
        Assert.Same(ValidationErrors, Results.Fail(ValidationErrors).Match(Unexpected<IReadOnlyList<Error>>, errors => errors));
    }

    [Fact]
    public void Tap_runs_success_callbacks_and_preserves_the_original_case()
    {
        var result = Results.Ok();
        var generic = Results.Ok(42);
        var calls = 0;
        var observed = 0;

        Assert.Same(result.Value, result.Tap(() => calls++).Value);
        Assert.Same(generic.Value, generic.Tap(value => observed = value).Value);
        Assert.Same(Missing, Results.Fail(Missing).Tap(() => Assert.Fail("Unexpected callback")).Value);
        Assert.Same(ValidationErrors, ((Result<int>)ValidationErrors).Tap(_ => Assert.Fail("Unexpected callback")).Value);
        Assert.Equal(1, calls);
        Assert.Equal(42, observed);
    }

    [Theory]
    [InlineData(42, true)]
    [InlineData(0, false)]
    public void Ensure_preserves_success_or_returns_the_supplied_error(int value, bool passes)
    {
        var result = Results.Ok();
        var generic = Results.Ok(value);

        Assert.Same(passes ? result.Value : Invalid, result.Ensure(() => passes, Invalid).Value);
        Assert.Same(passes ? generic.Value : Invalid, generic.Ensure(number => number > 0, Invalid).Value);
    }

    [Fact]
    public void Ensure_preserves_existing_failures_without_running_the_predicate()
    {
        Assert.Same(Missing, Results.Fail(Missing).Ensure(Unexpected<bool>, Invalid).Value);
        Assert.Same(ValidationErrors, ((Result<int>)ValidationErrors).Ensure(Unexpected<int, bool>, Invalid).Value);
    }

    [Fact]
    public void MapError_transforms_errors_in_order_and_preserves_the_union_case()
    {
        var result = Results.Ok();
        var generic = Results.Ok(42);

        Assert.Same(result.Value, result.MapError(Unexpected<Error, Error>).Value);
        Assert.Same(generic.Value, generic.MapError(Unexpected<Error, Error>).Value);
        Assert.Equal(ToCritical(Missing), Assert.IsType<Error>(Results.Fail(Missing).MapError(ToCritical).Value));
        Assert.Equal(ToCritical(Missing), Assert.IsType<Error>(((Result<int>)Missing).MapError(ToCritical).Value));

        var multiple = Assert.IsType<ErrorList>(Results.Fail(ValidationErrors).MapError(ToCritical).Value);
        var genericMultiple = Assert.IsType<ErrorList>(((Result<int>)ValidationErrors).MapError(ToCritical).Value);
        Assert.Equal(ValidationErrors.Select(ToCritical), multiple.AsEnumerable());
        Assert.Equal(ValidationErrors.Select(ToCritical), genericMultiple.AsEnumerable());
    }

    [Fact]
    public void Operators_reject_missing_callbacks()
    {
        Assert.Throws<ArgumentNullException>("map", () => Results.Ok(42).Map<int, int>(null!));
        Assert.Throws<ArgumentNullException>("onFailure", () => Results.Ok().Match(() => 42, null!));
    }

    [Fact]
    public void Operators_propagate_uninitialized_results_as_critical_errors()
    {
        var expected = new Error(ErrorType.Critical, "An uninitialized result has no outcome.");

        Assert.Equal(expected, default(Result<int>).Map(value => value).Value);
        Assert.Equal(expected, default(Result).Bind(Results.Ok).Value);
        Assert.Equal(expected, default(Result).Match(Unexpected<Error>, errors => Assert.Single(errors)));
    }

    [Fact]
    public void Map_bind_and_tap_convert_callback_exceptions_to_critical_errors()
    {
        var failure = new InvalidOperationException("Callback failed");
        var expected = new Error(ErrorType.Critical, failure.Message);

        Assert.Equal(expected, Results.Ok(42).Map<int, int>(_ => throw failure).Value);
        Assert.Equal(expected, Results.Ok(42).Bind<int, int>(_ => throw failure).Value);
        Assert.Equal(expected, Results.Ok(42).Tap(_ => throw failure).Value);
        Assert.Equal(expected, Results.Ok().Bind(() => throw failure).Value);
        Assert.Equal(expected, Results.Ok().Tap(() => throw failure).Value);
    }

    [Fact]
    public async Task Task_results_support_chaining_synchronous_callbacks()
    {
        var token = TestContext.Current.CancellationToken;
        var observed = 0;
        var result = await Task.FromResult(Results.Ok())
            .Bind(Results.Ok, token)
            .Tap(() => observed++, token)
            .Ensure(() => observed == 1, Invalid, token)
            .MapError(Unexpected<Error, Error>, token)
            .Bind(() => Results.Ok(21), token)
            .Map(value => value * 2, token)
            .Bind(value => Results.Ok(value), token)
            .Tap(value => observed = value, token)
            .Ensure(value => value == 42, Invalid, token)
            .MapError(Unexpected<Error, Error>, token)
            .Match(value => value, Unexpected<IReadOnlyList<Error>, int>, token);

        Assert.Equal(42, result);
        Assert.Equal(42, observed);

        var converted = await Task.FromResult(Results.Ok(42))
            .Bind(value => value == 42 ? Results.Ok() : Results.Fail(Invalid), token)
            .Match(() => true, Unexpected<IReadOnlyList<Error>, bool>, token);
        Assert.True(converted);
    }

    [Fact]
    public async Task Task_results_support_chaining_asynchronous_callbacks()
    {
        var token = TestContext.Current.CancellationToken;
        var result = Results.Ok();
        var observed = 0;

        var matched = await Task.FromResult(result)
            .BindAsync(() => Task.FromResult(result), token)
            .TapAsync(() => { observed++; return Task.CompletedTask; }, token)
            .EnsureAsync(() => Task.FromResult(observed == 1), Invalid, token)
            .MapErrorAsync(Unexpected<Error, Task<Error>>, token)
            .BindAsync(() => Task.FromResult(Results.Ok(21)), token)
            .MapAsync(value => Task.FromResult(value * 2), token)
            .BindAsync(value => Task.FromResult(Results.Ok(value)), token)
            .TapAsync(value => { observed = value; return Task.CompletedTask; }, token)
            .EnsureAsync(value => Task.FromResult(value == 42), Invalid, token)
            .MapErrorAsync(Unexpected<Error, Task<Error>>, token)
            .MatchAsync(Task.FromResult, Unexpected<IReadOnlyList<Error>, Task<int>>, token);

        Assert.Equal(42, matched);
        Assert.Equal(42, observed);

        var converted = await Task.FromResult(Results.Ok(42))
            .BindAsync(value => Task.FromResult(value == 42 ? Results.Ok() : Results.Fail(Invalid)), token)
            .MatchAsync(() => Task.FromResult(true), Unexpected<IReadOnlyList<Error>, Task<bool>>, token);
        Assert.True(converted);
    }

    [Fact]
    public async Task Async_operators_on_completed_results_pass_the_success_to_each_callback()
    {
        var token = TestContext.Current.CancellationToken;
        var result = Results.Ok();
        var bound = await result.BindAsync(() => Task.FromResult(result), token);
        var observed = 0;
        var tapped = await bound.TapAsync(() =>
        {
            observed++;
            return Task.CompletedTask;
        }, token);
        var checkedResult = await tapped.EnsureAsync(() => Task.FromResult(observed == 1), Invalid);
        var generic = await checkedResult.BindAsync(() => Task.FromResult(Results.Ok(21)), token);
        var mapped = await generic.MapAsync(value => Task.FromResult(value * 2), token);
        var genericBound = await mapped.BindAsync(value => Task.FromResult(Results.Ok(value)), token);
        var genericTapped = await genericBound.TapAsync(value =>
        {
            observed = value;
            return Task.CompletedTask;
        }, token);
        var checkedGeneric = await genericTapped.EnsureAsync(value => Task.FromResult(value == 42), Invalid);
        var resultWithoutPayload = await checkedGeneric.BindAsync(_ => Task.FromResult(result), token);

        Assert.Same(result.Value, resultWithoutPayload.Value);
        Assert.Equal(42, observed);
        Assert.Equal(42, await checkedGeneric.MatchAsync(Task.FromResult, Unexpected<IReadOnlyList<Error>, Task<int>>));
        Assert.True(await resultWithoutPayload.MatchAsync(() => Task.FromResult(true), Unexpected<IReadOnlyList<Error>, Task<bool>>));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Async_operators_short_circuit_success_callbacks_on_failure(bool useErrorList)
    {
        var token = TestContext.Current.CancellationToken;
        Result result = useErrorList ? Results.Fail(ValidationErrors) : Results.Fail(Missing);
        Result<int> generic = useErrorList ? (Result<int>)ValidationErrors : Missing;

        Assert.Same(generic.Value, (await generic.MapAsync(Unexpected<int, Task<string>>, token)).Value);
        Assert.Same(generic.Value, (await generic.BindAsync(Unexpected<int, Task<Result<string>>>, token)).Value);
        Assert.Same(generic.Value, (await generic.BindAsync(Unexpected<int, Task<Result>>, token)).Value);
        Assert.Same(result.Value, (await result.BindAsync(Unexpected<Task<Result>>, token)).Value);
        Assert.Same(result.Value, (await result.BindAsync(Unexpected<Task<Result<string>>>, token)).Value);
        Assert.Same(generic.Value, (await generic.TapAsync(Unexpected<int, Task>, token)).Value);
        Assert.Same(result.Value, (await result.TapAsync(Unexpected<Task>, token)).Value);
        Assert.Same(generic.Value, (await generic.EnsureAsync(Unexpected<int, Task<bool>>, Invalid)).Value);
        Assert.Same(result.Value, (await result.EnsureAsync(Unexpected<Task<bool>>, Invalid)).Value);

        var errors = await generic.MatchAsync(Unexpected<int, Task<IReadOnlyList<Error>>>, Task.FromResult);
        Assert.Equal(result.GetErrors().AsEnumerable(), errors);
        errors = await result.MatchAsync(Unexpected<Task<IReadOnlyList<Error>>>, Task.FromResult);
        Assert.Equal(generic.GetErrors().AsEnumerable(), errors);
    }

    [Fact]
    public async Task Async_validation_and_error_mapping_preserve_expected_cases()
    {
        Assert.Same(Invalid, (await Results.Ok(0).EnsureAsync(value => Task.FromResult(value > 0), Invalid)).Value);
        Assert.Same(Invalid, (await Results.Ok().EnsureAsync(() => Task.FromResult(false), Invalid)).Value);

        var single = await Results.Fail(Missing).MapErrorAsync(error => Task.FromResult(ToCritical(error)));
        Assert.Equal(ToCritical(Missing), Assert.IsType<Error>(single.Value));
        var genericSingle = await ((Result<int>)Missing).MapErrorAsync(error => Task.FromResult(ToCritical(error)));
        Assert.Equal(ToCritical(Missing), Assert.IsType<Error>(genericSingle.Value));

        var multiple = await Results.Fail(ValidationErrors).MapErrorAsync(error => Task.FromResult(ToCritical(error)));
        Assert.Equal(ValidationErrors.Select(ToCritical), Assert.IsType<ErrorList>(multiple.Value).AsEnumerable());
        var genericMultiple = await ((Result<int>)ValidationErrors).MapErrorAsync(error => Task.FromResult(ToCritical(error)));
        Assert.Equal(ValidationErrors.Select(ToCritical), Assert.IsType<ErrorList>(genericMultiple.Value).AsEnumerable());
    }

    [Fact]
    public async Task Async_map_awaits_and_converts_callback_exceptions_to_critical_errors()
    {
        var failure = new InvalidOperationException("Callback failed");
        var completion = new TaskCompletionSource<int>();
        var mapped = Results.Ok(42).MapAsync(_ => completion.Task, TestContext.Current.CancellationToken);

        Assert.False(mapped.IsCompleted);
        completion.SetException(failure);

        Assert.Equal(new Error(ErrorType.Critical, failure.Message), (await mapped).Value);
    }

    [Fact]
    public async Task Async_bind_and_tap_convert_callback_exceptions_to_critical_errors()
    {
        var token = TestContext.Current.CancellationToken;
        var failure = new InvalidOperationException("Callback failed");
        var expected = new Error(ErrorType.Critical, failure.Message);

        Assert.Equal(expected, (await Results.Ok(42).BindAsync(_ => Task.FromException<Result<int>>(failure), token)).Value);
        Assert.Equal(expected, (await Results.Ok().BindAsync(() => Task.FromException<Result>(failure), token)).Value);
        Assert.Equal(expected, (await Results.Ok(42).TapAsync(_ => Task.FromException(failure), token)).Value);
        Assert.Equal(expected, (await Results.Ok().TapAsync(() => Task.FromException(failure), token)).Value);
    }

    [Fact]
    public async Task Async_error_mapping_awaits_started_callbacks_even_after_a_synchronous_throw()
    {
        var pending = new TaskCompletionSource<Error>(TaskCreationOptions.RunContinuationsAsynchronously);
        var exception = new InvalidOperationException("Callback failed");
        var mapped = Results.Fail(ValidationErrors).MapErrorAsync(error =>
            error == Invalid ? pending.Task : throw exception);
        var completedEarly = mapped.IsCompleted;

        pending.SetResult(Invalid);

        Assert.Same(exception, await Assert.ThrowsAsync<InvalidOperationException>(() => mapped));
        Assert.False(completedEarly);
    }

    [Fact]
    public async Task Error_mapping_rejects_a_mix_of_error_categories()
    {
        var result = Results.Fail(ValidationErrors);
        Error MapOne(Error error) => error == Invalid ? ToCritical(error) : error;

        Assert.Throws<ArgumentException>("errors", () => result.MapError(MapOne));
        await Assert.ThrowsAsync<ArgumentException>("errors", () => result.MapErrorAsync(error => Task.FromResult(MapOne(error))));
    }

    [Fact]
    public async Task Task_chain_can_cancel_while_waiting_for_the_input()
    {
        var pending = new TaskCompletionSource<Result<int>>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var cancellation = new CancellationTokenSource();
        var mapped = pending.Task.MapAsync(Unexpected<int, Task<int>>, cancellation.Token);

        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            mapped.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
        Assert.False(pending.Task.IsCompleted);
    }

    [Fact]
    public async Task Task_chain_passes_the_token_to_a_cancellable_callback()
    {
        using var cancellation = new CancellationTokenSource();
        var pending = new TaskCompletionSource<int>();
        var mapped = Task.FromResult(Results.Ok(42)).MapAsync((value, token) =>
        {
            Assert.Equal(cancellation.Token, token);
            return pending.Task.WaitAsync(token);
        }, cancellation.Token);

        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            mapped.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
    }

    private static Error ToCritical(Error error) => new(ErrorType.Critical, $"Mapped: {error.Message}");

    private static TOut Unexpected<TOut>() => throw new InvalidOperationException("Unexpected callback");

    private static TOut Unexpected<TIn, TOut>(TIn _) => Unexpected<TOut>();
}
