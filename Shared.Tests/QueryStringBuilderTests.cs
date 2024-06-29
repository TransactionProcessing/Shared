using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using Shared.Web;
using Shouldly;
using Xunit;

namespace Shared.Tests
{
    public class QueryStringBuilderTests
    {
        [Fact]
        public void QueryStringBuilder_NoParameters_EmptyQueryStringReturned()
        {
            QueryStringBuilder builder = new QueryStringBuilder();

            var queryString = builder.BuildQueryString();

            queryString.ShouldBe(String.Empty);
        }

        [Fact]
        public void QueryStringBuilder_SingleParameters_CorrectQueryStringReturned()
        {
            QueryStringBuilder builder = new QueryStringBuilder();

            builder.AddParameter("param1", "testparam1");

            var queryString = builder.BuildQueryString();

            queryString.ShouldBe("param1=testparam1");
        }

        [Fact]
        public void QueryStringBuilder_MultipleParameters_CorrectQueryStringReturned()
        {
            QueryStringBuilder builder = new QueryStringBuilder();

            builder.AddParameter("param1", "testparam1");
            builder.AddParameter("param2", "testparam2");

            var queryString = builder.BuildQueryString();

            queryString.ShouldBe("param1=testparam1&param2=testparam2");
        }

        static T GetDefaultGeneric<T>()
        {
            return default(T);
        }

        static object GetDefault(Type type)
        {
            // Create a generic method with reflection to get the default value
            var method = typeof(QueryStringBuilderTests).GetMethod(nameof(GetDefaultGeneric), System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            var genericMethod = method.MakeGenericMethod(type);
            return genericMethod.Invoke(null, null);
        }

        [Theory]
        [InlineData(typeof(Decimal))]
        [InlineData(typeof(DateTime))]
        [InlineData(typeof(Int32))]
        [InlineData(typeof(Int16))]
        public void QueryStringBuilder_AddParametersWithDefaultValue_NotIncludedInQueryStringReturned(Type t)
        {
            QueryStringBuilder builder = new QueryStringBuilder();

            object defaultValue = GetDefault(t);

            builder.AddParameter("param1", "testparam1");
            builder.AddParameter("param2", defaultValue);

            var queryString = builder.BuildQueryString();

            queryString.Contains("param2").ShouldBeFalse();
        }

        [Theory]
        [InlineData(typeof(Decimal))]
        [InlineData(typeof(DateTime))]
        [InlineData(typeof(Int32))]
        [InlineData(typeof(Int16))]
        public void QueryStringBuilder_AddParametersWithDefaultValue_AlwaysInclude_ParameterIncludedInQueryStringReturned(Type t)
        {
            QueryStringBuilder builder = new QueryStringBuilder();

            object defaultValue = GetDefault(t);

            builder.AddParameter("param1", "testparam1");
            builder.AddParameter("param2", defaultValue, true);

            var queryString = builder.BuildQueryString();

            queryString.Contains("param2").ShouldBeTrue();
        }
    }
}
