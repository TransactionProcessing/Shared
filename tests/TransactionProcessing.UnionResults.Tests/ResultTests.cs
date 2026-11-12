using TransactionProcessing.UnionResults;
using Xunit;

namespace TransactionProcessing.UnionResults.Tests;

public sealed class ResultTests
{
    [Fact]
    public void Map_preserves_created_case_and_location()
    {
        var result = Result<int>.Created(42, "/items/42").Map(value => value.ToString());
        var created = Assert.IsType<Created<string>>(result.Value);
        Assert.Equal("42", created.Value);
        Assert.Equal("/items/42", created.Location);
    }

    [Fact]
    public void Default_result_fails_fast()
    {
        Assert.Throws<InvalidOperationException>(() => default(Result<int>).GetOutcome());
    }

    [Fact]
    public void Value_access_and_failure_projection_preserve_union_outcomes()
    {
        var success = Result<int>.Created(42, "/items/42");
        Assert.True(success.IsSuccess());
        Assert.Equal(42, success.GetValueOrThrow());

        var failure = Result<int>.Fail(new Error("Item.Missing", "Missing", ErrorType.NotFound));
        Assert.True(failure.IsFailure());
        Assert.Equal("Missing", failure.GetFailureMessage());
        Assert.Equal(ErrorType.NotFound, failure.ToResult().GetOutcome().Error?.ErrorType);
    }

    [Fact]
    public void Generic_payload_types_can_overlap_with_other_union_cases()
    {
        var errorPayload = Error.NotFound("payload");
        var successfulError = Result<Error>.Ok(errorPayload).GetOutcome();
        var failedError = Result<Error>.Fail(errorPayload).GetOutcome();

        Assert.Equal(ResultKind.Ok, successfulError.Kind);
        Assert.Same(errorPayload, successfulError.Value);
        Assert.Equal(ResultKind.Error, failedError.Kind);
        Assert.Same(errorPayload, failedError.Error);

        var errorListPayload = new ErrorList([errorPayload]);
        var successfulErrorList = Result<ErrorList>.Ok(errorListPayload).GetOutcome();
        var failedErrorList = Result<ErrorList>.Fail(errorListPayload).GetOutcome();
        Assert.Equal(ResultKind.Ok, successfulErrorList.Kind);
        Assert.Same(errorListPayload, successfulErrorList.Value);
        Assert.Equal(ResultKind.ErrorList, failedErrorList.Kind);
        Assert.Same(errorListPayload, failedErrorList.Errors);

        var noContentPayload = new NoContent();
        var successfulNoContent = Result<NoContent>.Ok(noContentPayload).GetOutcome();
        var actualNoContent = Result<NoContent>.NoContent().GetOutcome();

        Assert.Equal(ResultKind.Ok, successfulNoContent.Kind);
        Assert.Same(noContentPayload, successfulNoContent.Value);
        Assert.Equal(ResultKind.NoContent, actualNoContent.Kind);
        Assert.Null(actualNoContent.Value);
    }

    [Fact]
    public void Error_list_snapshots_and_checks_categories()
    {
        var input = new List<Error> { new("A", "first", ErrorType.Validation) };
        var errors = new ErrorList(input);
        input.Add(new("B", "second", ErrorType.Validation));
        Assert.Single(errors);
        Assert.Throws<ArgumentException>(() => new ErrorList([new("A", "a", ErrorType.Validation), new("B", "b", ErrorType.NotFound)]));
    }
}
