namespace TransactionProcessing.Shared.IntegrationTesting.UnitTests;

using Reqnroll;
using Shouldly;

public class ReqnrollTableHelperTests
{
    private const string FieldOneHeader = "Field1";

    [Theory]
    [InlineData(FieldOneHeader, "true", true)]
    [InlineData(FieldOneHeader, "false", false)]
    public void ReqnrollTableHelper_GetBooleanValue_ExpectedValueIsReturned(String header, String value, Boolean expectedValue)
    {
        List<String> headers = new();
        headers.Add(header);
        DataTable table = new(headers.ToArray());
        table.AddRow(value);
        Boolean actual = ReqnrollTableHelper.GetBooleanValue(table.Rows.First(), header);
        actual.ShouldBe(expectedValue);
    }

    [Theory]
    [MemberData(nameof(GetUserChoiceTestData))]
    public void ReqnrollTableHelper_GetDateForDateString_ExpectedValueIsReturned(String value, DateTime expectedDate)
    {
            
        DateTime today = new(2023, 11, 27);
        DateTime actual = ReqnrollTableHelper.GetDateForDateString(value, today);
        actual.ShouldBe(expectedDate);
    }

    public static IEnumerable<object[]> GetUserChoiceTestData()
    {
        yield return new object[] { "TODAY", new DateTime(2023, 11, 27) };
        yield return new object[] { "YESTERDAY", new DateTime(2023, 11, 26) };
        yield return new object[] { "LASTWEEK", new DateTime(2023, 11, 20) };
        yield return new object[] { "LASTMONTH", new DateTime(2023, 10, 27) };
        yield return new object[] { "LASTYEAR", new DateTime(2022, 11, 27) };
        yield return new object[] { "TOMORROW", new DateTime(2023, 11, 28) };
        yield return new object[] { "2023-11-01", new DateTime(2023, 11, 1) };
    }

    [Theory]
    [MemberData(nameof(GetDecimalTestData))]
    public void ReqnrollTableHelper_GetDecimalValue_ExpectedValueIsReturned(String header, String value, Decimal expectedValue)
    {
        List<String> headers = new();
        headers.Add(header);
        DataTable table = new(headers.ToArray());
        table.AddRow(value);
        Decimal actual = ReqnrollTableHelper.GetDecimalValue(table.Rows.First(), header);
        actual.ShouldBe(expectedValue);
    }

    public static IEnumerable<object[]> GetDecimalTestData()
    {
        yield return new object[] { FieldOneHeader, "-1.00", -1.00m };
        yield return new object[] { FieldOneHeader, "0.00", 0.00m };
        yield return new object[] { FieldOneHeader, "1.00", 1.00m };
        yield return new object[] { FieldOneHeader, "-1.23", -1.23m };
        yield return new object[] { FieldOneHeader, "1.23", 1.23m };
    }

    [Theory]
    [InlineData(FieldOneHeader, "-1", -1.00)]
    [InlineData(FieldOneHeader, "0", 0.00)]
    [InlineData(FieldOneHeader, "1", 1.00)]
    public void ReqnrollTableHelper_GetIntValue_ExpectedValueIsReturned(String header, String value, Int32 expectedValue)
    {
        List<String> headers = new();
        headers.Add(header);
        DataTable table = new(headers.ToArray());
        table.AddRow(value);
        Int32 actual = ReqnrollTableHelper.GetIntValue(table.Rows.First(), header);
        actual.ShouldBe(expectedValue);
    }

    [Theory]
    [InlineData(FieldOneHeader, "-1", -1.00)]
    [InlineData(FieldOneHeader, "0", 0.00)]
    [InlineData(FieldOneHeader, "1", 1.00)]
    public void ReqnrollTableHelper_GetShortValue_ExpectedValueIsReturned(String header, String value, Int16 expectedValue)
    {
        List<String> headers = new();
        headers.Add(header);
        DataTable table = new(headers.ToArray());
        table.AddRow(value);
        Int16 actual = ReqnrollTableHelper.GetShortValue(table.Rows.First(), header);
        actual.ShouldBe(expectedValue);
    }

    [Fact]
    public void ReqnrollTableHelper_GetStringRowValue_ExpectedValueIsReturned()
    {
        String header = FieldOneHeader;
        String expectedValue = "TestStringValue";

        List<String> headers = new();
        headers.Add(header);
        DataTable table = new(headers.ToArray());
        table.AddRow(expectedValue);

        String? actual = ReqnrollTableHelper.GetStringRowValue(table.Rows.First(), header);
        actual.ShouldBe(expectedValue);
    }

    [Theory]
    [InlineData(FieldOneHeader, "Ordinal", StringComparison.Ordinal)]
    [InlineData(FieldOneHeader, "OrdinalIgnoreCase", StringComparison.OrdinalIgnoreCase)]
    [InlineData(FieldOneHeader, "InvariantCulture", StringComparison.InvariantCulture)]
    [InlineData(FieldOneHeader, "InvariantCultureIgnoreCase", StringComparison.InvariantCultureIgnoreCase)]
    [InlineData(FieldOneHeader, "CurrentCulture", StringComparison.CurrentCulture)]
    [InlineData(FieldOneHeader, "CurrentCultureIgnoreCase", StringComparison.CurrentCultureIgnoreCase)]
    public void ReqnrollTableHelper_GetEnumValue_ExpectedResultIsReturned(String header, String value, StringComparison expectedValue)
    {
        List<String> headers = new();
        headers.Add(header);
        DataTable table = new(headers.ToArray());
        table.AddRow(value);
        StringComparison actual = ReqnrollTableHelper.GetEnumValue<StringComparison>(table.Rows.First(), header);
        actual.ShouldBe(expectedValue);
    }
}
