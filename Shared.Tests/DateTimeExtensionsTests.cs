namespace Shared.Tests
{
    using System;
    using Extensions;
    using Shouldly;
    using Xunit;

    public partial class SharedTests
    {
        #region Methods

        [Theory]
        [InlineData("2022-01-12", "yyyy-MM-dd", "79fe8000-d55e-08d9-0000-000000000000")]
        [InlineData("2022-01-12 12:39:30", "yyyy-MM-dd HH:mm:ss", "93d4ad00-d5c8-08d9-0000-000000000000")]
        public void DateTime_ToGuid_IsConverted(String dateTime,
                                                String format,
                                                String expectedGuid)
        {
            DateTime inputDateTime = DateTime.ParseExact(dateTime, format, null);

            Guid guid = inputDateTime.ToGuid();

            guid.ShouldBe(Guid.Parse(expectedGuid));
        }

        #endregion
    }
}