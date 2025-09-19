using Shared.Extensions;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Shared.Tests;

public partial class SharedTests
{
    [Theory]
    [InlineData("{\r\n\"Property1\": \"Value1\",\r\n\"Property2\": \"Value2\"\r\n}", true)]
    [InlineData("{\r\n\"Property1\": \"Value1\"\r\n\"Property2\": \"Value2\"\r\n}", false)]
    [InlineData("{\r\n\"Property1\": \r\n}", false)]
    [InlineData("{\r\n\"Property1\": \"Value1\",\r\n\"Property3\": \"Value2\"\r\n}", false)]
    public void StringExtensions_TryParseJson_ResultExpected(String json, Boolean expectedResult)
    {
        Boolean isValidObject = json.TryParseJson(out TestModel model);

        isValidObject.ShouldBe(expectedResult);
    }

    public class TestModel
    {
        public String Property1 { get; set; }
        public String Property2 { get; set; }
    }
}