namespace Shared.IntegrationTesting.UnitTests
{
    using Shouldly;
    using TechTalk.SpecFlow;

    public class SpecflowTableHelperTests
    {
        [Theory]
        [InlineData("Field1", "true", true)]
        [InlineData("Field1", "false", false)]
        public void SpecflowTableHelper_GetBooleanValue_ExpectedValueIsReturned(String header, String value, Boolean expectedValue)
        {
            List<String> headers = new List<String>();
            headers.Add(header);
            Table table = new Table(headers.ToArray());
            table.AddRow(value);
            Boolean actual = SpecflowTableHelper.GetBooleanValue(table.Rows.First(), header);
            actual.ShouldBe(expectedValue);
        }

        [Theory]
        [MemberData(nameof(GetUserChoiceTestData))]
        public void SpecflowTableHelper_GetDateForDateString_ExpectedValueIsReturned(String value, DateTime expectedDate)
        {
            
            DateTime today = new DateTime(2023, 11, 27);
            DateTime actual = SpecflowTableHelper.GetDateForDateString(value, today);
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
        [InlineData("Field1", "-1.00", -1.00)]
        [InlineData("Field1", "0.00", 0.00)]
        [InlineData("Field1", "1.00", 1.00)]
        [InlineData("Field1", "-1.23", -1.23)]
        [InlineData("Field1", "1.23", 1.23)]
        public void SpecflowTableHelper_GetDecimalValue_ExpectedValueIsReturned(String header, String value, Decimal expectedValue)
        {
            List<String> headers = new List<String>();
            headers.Add(header);
            Table table = new Table(headers.ToArray());
            table.AddRow(value);
            Decimal actual = SpecflowTableHelper.GetDecimalValue(table.Rows.First(), header);
            actual.ShouldBe(expectedValue);
        }

        [Theory]
        [InlineData("Field1", "-1", -1.00)]
        [InlineData("Field1", "0", 0.00)]
        [InlineData("Field1", "1", 1.00)]        
        public void SpecflowTableHelper_GetIntValue_ExpectedValueIsReturned(String header, String value, Int32 expectedValue)
        {
            List<String> headers = new List<String>();
            headers.Add(header);
            Table table = new Table(headers.ToArray());
            table.AddRow(value);
            Int32 actual = SpecflowTableHelper.GetIntValue(table.Rows.First(), header);
            actual.ShouldBe(expectedValue);
        }

        [Theory]
        [InlineData("Field1", "-1", -1.00)]
        [InlineData("Field1", "0", 0.00)]
        [InlineData("Field1", "1", 1.00)]
        public void SpecflowTableHelper_GetShortValue_ExpectedValueIsReturned(String header, String value, Int16 expectedValue)
        {
            List<String> headers = new List<String>();
            headers.Add(header);
            Table table = new Table(headers.ToArray());
            table.AddRow(value);
            Int16 actual = SpecflowTableHelper.GetShortValue(table.Rows.First(), header);
            actual.ShouldBe(expectedValue);
        }

        [Fact]
        public void SpecflowTableHelper_GetStringRowValue_ExpectedValueIsReturned()
        {
            String header = "Field1";
            String expectedValue = "TestStringValue";

            List<String> headers = new List<String>();
            headers.Add(header);
            Table table = new Table(headers.ToArray());
            table.AddRow(expectedValue);

            String? actual = SpecflowTableHelper.GetStringRowValue(table.Rows.First(), header);
            actual.ShouldBe(expectedValue);
        }

        [Theory]
        [InlineData("Field1", "Ordinal", StringComparison.Ordinal)]
        [InlineData("Field1", "OrdinalIgnoreCase", StringComparison.OrdinalIgnoreCase)]
        [InlineData("Field1", "InvariantCulture", StringComparison.InvariantCulture)]
        [InlineData("Field1", "InvariantCultureIgnoreCase", StringComparison.InvariantCultureIgnoreCase)]
        [InlineData("Field1", "CurrentCulture", StringComparison.CurrentCulture)]
        [InlineData("Field1", "CurrentCultureIgnoreCase", StringComparison.CurrentCultureIgnoreCase)]
        public void SpecflowTableHelper_GetEnumValue_ExpectedResultIsReturned(String header, String value, StringComparison expectedValue)
        {
            List<String> headers = new List<String>();
            headers.Add(header);
            Table table = new Table(headers.ToArray());
            table.AddRow(value);
            StringComparison actual = SpecflowTableHelper.GetEnumValue<StringComparison>(table.Rows.First(), header);
            actual.ShouldBe(expectedValue);
        }
    }
}