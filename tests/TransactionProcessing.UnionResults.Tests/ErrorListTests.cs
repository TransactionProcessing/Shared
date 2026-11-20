using System.Collections;
using Xunit;

namespace TransactionProcessing.UnionResults.Tests;

public sealed class ErrorListTests
{
    private const string NameRequired = "Name is required";
    private const string ErrorsParameter = "errors";

    [Fact]
    public void Error_list_preserves_the_category_and_order()
    {
        var first = new Error(ErrorType.Validation, NameRequired);
        var second = new Error(ErrorType.Validation, "Email is required");

        var errors = new ErrorList([first, second]);

        Assert.Equal(ErrorType.Validation, errors.ErrorType);
        Assert.Equal(2, errors.Count);
        Assert.Same(first, errors[0]);
        Assert.Same(second, errors[1]);
        Assert.Equal([first, second], errors.AsEnumerable());
        IEnumerable nonGeneric = errors;
        var enumerator = nonGeneric.GetEnumerator();
        Assert.True(enumerator.MoveNext());
        Assert.Same(first, enumerator.Current);
        Assert.True(enumerator.MoveNext());
        Assert.Same(second, enumerator.Current);
        Assert.False(enumerator.MoveNext());
    }

    [Fact]
    public void Error_list_snapshots_its_input()
    {
        var original = new Error(ErrorType.Validation, NameRequired);
        var input = new List<Error> { original };
        var errors = new ErrorList(input);

        input[0] = new Error(ErrorType.NotFound, "Item not found");
        input.Add(new Error(ErrorType.Validation, "Email is required"));

        Assert.Same(original, Assert.Single(errors));
    }

    [Fact]
    public void Error_list_rejects_null_input()
    {
        Assert.Throws<ArgumentNullException>(ErrorsParameter, () => new ErrorList(null!));
    }

    [Fact]
    public void Error_list_rejects_empty_input()
    {
        Assert.Throws<ArgumentException>(ErrorsParameter, () => new ErrorList([]));
    }

    [Fact]
    public void Error_list_rejects_a_null_entry()
    {
        var error = new Error(ErrorType.Validation, NameRequired);

        Assert.Throws<ArgumentException>(ErrorsParameter, () => new ErrorList([error, null!]));
    }

    [Fact]
    public void Error_list_rejects_mixed_error_categories()
    {
        var validation = new Error(ErrorType.Validation, NameRequired);
        var notFound = new Error(ErrorType.NotFound, "Item not found");

        Assert.Throws<ArgumentException>(ErrorsParameter, () => new ErrorList([validation, notFound]));
    }
}
