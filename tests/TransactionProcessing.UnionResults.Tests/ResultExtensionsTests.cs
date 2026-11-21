using Xunit;

namespace TransactionProcessing.UnionResults.Tests;

public sealed class ResultExtensionsTests
{
    [Fact]
    public void Successes_have_no_errors_or_error_message()
    {
        var result = Results.Ok();
        var generic = Results.Ok(new Error(ErrorType.NotFound, "Payload"));

        Assert.True(result.IsSuccess());
        Assert.True(generic.IsSuccess());
        Assert.Empty(result.GetErrors());
        Assert.Empty(generic.GetErrors());
        Assert.Null(result.GetErrorMessage());
        Assert.Null(generic.GetErrorMessage());
    }

    [Fact]
    public void Single_errors_are_exposed_as_one_item()
    {
        var error = new Error(ErrorType.NotFound, "Item not found");
        var result = Results.Fail(error);
        Result<int> generic = error;

        Assert.False(result.IsSuccess());
        Assert.False(generic.IsSuccess());
        Assert.Same(error, Assert.Single(result.GetErrors()));
        Assert.Same(error, Assert.Single(generic.GetErrors()));
        Assert.Equal(error.Message, result.GetErrorMessage());
        Assert.Equal(error.Message, generic.GetErrorMessage());
    }

    [Fact]
    public void Error_lists_are_preserved_and_messages_are_joined_in_order()
    {
        var errors = new ErrorList(
        [
            new Error(ErrorType.Validation, "Name is required"),
            new Error(ErrorType.Validation, "Email is required"),
        ]);
        var result = Results.Fail(errors);
        Result<int> generic = errors;

        Assert.False(result.IsSuccess());
        Assert.False(generic.IsSuccess());
        Assert.Same(errors, result.GetErrors());
        Assert.Same(errors, generic.GetErrors());
        Assert.Equal("Name is required; Email is required", result.GetErrorMessage());
        Assert.Equal("Name is required; Email is required", generic.GetErrorMessage());
    }

    [Fact]
    public void Default_results_are_unsuccessful_and_have_a_critical_diagnostic()
    {
        Result result = default;
        Result<int> generic = default;

        Assert.False(result.IsSuccess());
        Assert.False(generic.IsSuccess());
        var expected = new Error(ErrorType.Critical, "An uninitialized result has no outcome.");
        Assert.Equal(expected, Assert.Single(result.GetErrors()));
        Assert.Equal(expected, Assert.Single(generic.GetErrors()));
        Assert.Equal(expected.Message, result.GetErrorMessage());
        Assert.Equal(expected.Message, generic.GetErrorMessage());
    }
}
