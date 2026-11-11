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
    public void Error_list_snapshots_and_checks_categories()
    {
        var input = new List<Error> { new("A", "first", ErrorType.Validation) };
        var errors = new ErrorList(input);
        input.Add(new("B", "second", ErrorType.Validation));
        Assert.Single(errors);
        Assert.Throws<ArgumentException>(() => new ErrorList([new("A", "a", ErrorType.Validation), new("B", "b", ErrorType.NotFound)]));
    }
}
