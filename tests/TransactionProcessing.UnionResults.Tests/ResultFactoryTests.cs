using Xunit;

namespace TransactionProcessing.UnionResults.Tests;

public sealed class ResultFactoryTests
{
    [Fact]
    public void Ok_creates_a_success_without_a_payload()
    {
        var result = Results.Ok();

        Assert.IsType<Success>(result.Value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(42)]
    public void Generic_ok_wraps_the_supplied_value(int value)
    {
        var result = Results.Ok(value);

        var success = Assert.IsType<Success<int>>(result.Value);
        Assert.Equal(value, success.Value);
    }

    [Fact]
    public void Generic_ok_wraps_a_nullable_payload_even_when_it_is_null()
    {
        var result = Results.Ok<string?>(null);

        var success = Assert.IsType<Success<string?>>(result.Value);
        Assert.Null(success.Value);
    }

    [Fact]
    public void Fail_preserves_the_supplied_error()
    {
        var error = new Error(ErrorType.NotFound, "Item not found");

        var result = Results.Fail(error);

        Assert.Same(error, Assert.IsType<Error>(result.Value));
    }

    [Fact]
    public void Fail_preserves_the_supplied_error_list()
    {
        var errors = new ErrorList([new Error(ErrorType.Validation, "Name is required")]);

        var result = Results.Fail(errors);

        Assert.Same(errors, Assert.IsType<ErrorList>(result.Value));
    }

    [Fact]
    public void Fail_with_a_missing_error_returns_a_critical_diagnostic()
    {
        var expected = new Error(ErrorType.Critical, "A failed result requires an error.");

        Assert.Equal(expected, Assert.Single(Results.Fail((Error)null!).GetErrors()));
        Assert.Equal(expected, Assert.Single(Results.Fail((ErrorList)null!).GetErrors()));
    }

    [Fact]
    public void Message_factories_create_errors_with_the_expected_category_and_message()
    {
        const string Message = "  Supplied message  ";
        (Result Result, ErrorType Category)[] cases =
        [
            (Results.Validation(Message), ErrorType.Validation),
            (Results.Conflict(Message), ErrorType.Conflict),
            (Results.Critical(Message), ErrorType.Critical),
            (Results.Fail(Message), ErrorType.Failure),
            (Results.Forbidden(Message), ErrorType.Forbidden),
            (Results.NotFound(Message), ErrorType.NotFound),
            (Results.Unauthorized(Message), ErrorType.Unauthorized),
            (Results.Unknown(Message), ErrorType.Unknown),
        ];

        foreach (var (result, category) in cases)
        {
            var error = Assert.IsType<Error>(result.Value);
            Assert.Equal(new Error(category, Message), error);
        }
    }
}
