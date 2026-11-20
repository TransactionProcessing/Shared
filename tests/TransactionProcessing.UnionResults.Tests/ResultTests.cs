using Xunit;

namespace TransactionProcessing.UnionResults.Tests;

public sealed class ResultTests
{
    [Fact]
    public void Generic_result_distinguishes_an_error_payload_from_an_error_case()
    {
        var error = new Error(ErrorType.NotFound, "Item not found");

        Result<Error> success = Results.Ok(error);
        Result<Error> failure = error;

        Assert.Same(error, Assert.IsType<Success<Error>>(success.Value).Value);
        Assert.Same(error, Assert.IsType<Error>(failure.Value));
    }

    [Fact]
    public void Generic_result_distinguishes_an_error_list_payload_from_an_error_list_case()
    {
        var errors = new ErrorList([new Error(ErrorType.Validation, "Name is required")]);

        Result<ErrorList> success = Results.Ok(errors);
        Result<ErrorList> failure = errors;

        Assert.Same(errors, Assert.IsType<Success<ErrorList>>(success.Value).Value);
        Assert.Same(errors, Assert.IsType<ErrorList>(failure.Value));
    }

    [Fact]
    public void Default_results_have_no_union_case()
    {
        Assert.Null(default(Result).Value);
        Assert.Null(default(Result<int>).Value);
    }
}
