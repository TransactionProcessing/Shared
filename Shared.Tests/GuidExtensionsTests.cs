namespace Shared.Tests;

using System;
using Extensions;
using Shouldly;
using Xunit;

public partial class SharedTests
{
    #region Methods

    [Theory]
    [InlineData("79fe8000-d55e-08d9-0000-000000000000", "2022-01-12", "yyyy-MM-dd")]
    [InlineData("93d4ad00-d5c8-08d9-0000-000000000000", "2022-01-12 12:39:30", "yyyy-MM-dd HH:mm:ss")]
    public void Guid_ToDateTime_IsConverted(String guid,
                                            String expectedDateTime,
                                            String format)
    {
        Guid inputGuid = Guid.Parse(guid);

        DateTime dateTime = inputGuid.ToDateTime();

        dateTime.ShouldBe(DateTime.ParseExact(expectedDateTime, format, null));
    }

    #endregion
}