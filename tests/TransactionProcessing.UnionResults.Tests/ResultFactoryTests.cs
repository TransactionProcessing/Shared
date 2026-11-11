using TransactionProcessing.UnionResults;
using Xunit;

namespace TransactionProcessing.UnionResults.Tests;

public sealed class ResultFactoryTests
{
    [Fact]
    public void Error_factories_assign_their_error_categories()
    {
        var errors = new[]
        {
            (Error.Failure("failure"), ErrorType.Failure),
            (Error.Validation("validation", "name"), ErrorType.Validation),
            (Error.NotFound("missing"), ErrorType.NotFound),
            (Error.Unauthorized("unauthorized"), ErrorType.Unauthorized),
            (Error.Forbidden("forbidden"), ErrorType.Forbidden),
            (Error.Conflict("conflict"), ErrorType.Conflict),
            (Error.Critical("critical"), ErrorType.Critical),
        };

        foreach (var (error, errorType) in errors)
            Assert.Equal(errorType, error.ErrorType);

        Assert.Equal("name", errors[1].Item1.Field);
        Assert.Null(errors[0].Item1.Field);
    }

    [Fact]
    public void Error_constructor_validates_code_and_message()
    {
        Assert.Throws<ArgumentException>(() => new Error(" ", "message", ErrorType.Failure));
        Assert.Throws<ArgumentNullException>(() => new Error("code", null!, ErrorType.Failure));
        var error = new Error("custom.code", "custom message", ErrorType.Unknown, "field");
        Assert.Equal("custom.code", error.Code);
        Assert.Equal("custom message", error.Message);
        Assert.Equal(ErrorType.Unknown, error.ErrorType);
        Assert.Equal("field", error.Field);
    }

    [Fact]
    public void Error_list_validates_entries_and_exposes_read_only_list_contract()
    {
        Assert.Throws<ArgumentNullException>(() => new ErrorList(null!));
        Assert.Throws<ArgumentException>(() => new ErrorList([]));
        Assert.Throws<ArgumentException>(() => new ErrorList([null!]));
        Assert.Throws<ArgumentException>(() => new ErrorList([Error.Validation("bad"), Error.NotFound("missing")]));

        var first = Error.Validation("first");
        var second = Error.Validation("second");
        var errors = new ErrorList([first, second]);
        Assert.Equal(ErrorType.Validation, errors.ErrorType);
        Assert.Equal(2, errors.Count);
        Assert.Same(first, errors[0]);
        Assert.Equal(new[] { first, second }, errors.ToArray());
        Assert.Equal(2, ((System.Collections.IEnumerable)errors).Cast<object>().Count());
    }

    [Fact]
    public void Result_factories_create_success_and_no_content_cases()
    {
        Assert.Equal(ResultKind.Ok, Result.Ok().GetOutcome().Kind);
        Assert.Equal(ResultKind.Ok, Result.Success().GetOutcome().Kind);
        Assert.Equal(42, Result.Success(42).Data);
        Assert.Equal(ResultKind.NoContent, Result.NoContent().GetOutcome().Kind);
        Assert.Equal(ResultKind.Ok, Result.CreatedResource().GetOutcome().Kind);
        Assert.Equal(ResultKind.NoContent, Result.DeletedResource().GetOutcome().Kind);
        Assert.Equal(ResultKind.NoContent, Result.UpdatedResource().GetOutcome().Kind);
        Assert.Equal("value", Result.ObtainedResource("value").Data);
        Assert.Equal(ResultKind.Created, Result<string>.Created("value", "/items/1").GetOutcome().Kind);
        Assert.Equal(ResultKind.NoContent, Result<string>.NoContent().GetOutcome().Kind);
        Assert.Throws<ArgumentNullException>(() => Result<string>.Ok(null!));
        Assert.Throws<ArgumentNullException>(() => Result<string>.Created(null!));
    }

    [Fact]
    public void Category_factories_create_single_errors_and_error_lists()
    {
        var singleErrors = new[]
        {
            Result.Failure("failure"),
            Result.Validation("validation"),
            Result.Invalid("invalid"),
            Result.NotFound("missing"),
            Result.Unauthorized("unauthorized"),
            Result.Forbidden("forbidden"),
            Result.Conflict("conflict"),
            Result.CriticalError("critical"),
        };
        Assert.Equal(
            [ErrorType.Failure, ErrorType.Validation, ErrorType.Validation, ErrorType.NotFound, ErrorType.Unauthorized, ErrorType.Forbidden, ErrorType.Conflict, ErrorType.Critical],
            singleErrors.Select(static error => error.ErrorType));

        var errorLists = new[]
        {
            Result.Failure("failure", ["detail"]),
            Result.Invalid("invalid", ["detail"]),
            Result.Validation("validation", ["detail"]),
            Result.NotFound("missing", ["detail"]),
            Result.Unauthorized("unauthorized", ["detail"]),
            Result.Forbidden("forbidden", ["detail"]),
            Result.Conflict("conflict", ["detail"]),
            Result.CriticalError("critical", ["detail"]),
        };
        Assert.Equal(
            [ErrorType.Failure, ErrorType.Validation, ErrorType.Validation, ErrorType.NotFound, ErrorType.Unauthorized, ErrorType.Forbidden, ErrorType.Conflict, ErrorType.Critical],
            errorLists.Select(static errors => errors.ErrorType));
        Assert.All(errorLists, static errors => Assert.Equal(2, errors.Count));
    }

    [Fact]
    public void Error_list_factories_filter_blank_values_and_use_a_fallback_message()
    {
        var filtered = Result.Invalid(" summary ", ["field error", " ", ""]);
        Assert.Equal(2, filtered.Count);
        Assert.Equal(" summary ", filtered[0].Message);
        Assert.Equal("field error", filtered[1].Message);

        var fallback = Result.Failure(Array.Empty<string>());
        Assert.Single(fallback);
        Assert.Equal("An unspecified error occurred.", fallback[0].Message);
        Assert.Throws<ArgumentNullException>(() => Result.Failure("failure", null!));
    }

    [Fact]
    public void Generic_failure_factory_propagates_single_and_multiple_errors()
    {
        var single = Result<int>.FromFailure(Result.Fail(Error.NotFound("missing")));
        Assert.Equal(ErrorType.NotFound, single.Errors[0].ErrorType);
        var multiple = Result<int>.FromFailure(Result.Fail(new ErrorList([Error.Validation("one"), Error.Validation("two")])));
        Assert.Equal(2, multiple.Errors.Count);
        Assert.Throws<InvalidOperationException>(() => Result<int>.FromFailure(Result.Ok()));
        Assert.Throws<ArgumentNullException>(() => Result<int>.FromFailure(null!));
    }

    [Fact]
    public void Default_results_reject_value_access_and_report_no_errors_for_success()
    {
        var successful = Result.Success("value");
        Assert.Empty(successful.Errors);
        Assert.Empty(Result.Ok().Errors);
        Assert.False(successful.IsFailed);
        Assert.True(Result.Fail(Error.Failure("failure")).IsFailed);
        Assert.Throws<InvalidOperationException>(() => default(Result).IsFailed);
        Assert.Throws<InvalidOperationException>(() => default(Result).Errors);
        Assert.Throws<InvalidOperationException>(() => default(Result).Message);
        Assert.Throws<InvalidOperationException>(() => default(Result<int>).Data);
        Assert.Throws<InvalidOperationException>(() => default(Result<int>).Errors);
        Assert.Throws<InvalidOperationException>(() => default(Result<int>).Message);
    }
}
