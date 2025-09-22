using System;
using System.Collections.Generic;
using Shared.General;
using Shouldly;
using Xunit;

namespace Shared.Tests;

using System.CodeDom;
using System.Text;

public partial class SharedTests
{
    public enum GuardTestEnum
    {
        First = 0,
        Second
    }

    [Fact]
    public void Guard_ThrowIfNull_NotNull_NoErrorThrown()
    {
        Object testObject = new();

        Should.NotThrow( () => Guard.ThrowIfNull(testObject, nameof(testObject)));
    }

    [Fact]
    public void Guard_ThrowIfNull_ObjectIsNull_ErrorThrown()
    {
        Object testObject = null;

        Should.Throw<ArgumentNullException>( () => Guard.ThrowIfNull(testObject, nameof(testObject)));
    }

    [Fact]
    public void Guard_ThrowIfNullWithExceptionType_NotNull_NoErrorThrown()
    {
        Object testObject = new();

        Should.NotThrow(() => Guard.ThrowIfNull(testObject, typeof(Exception), nameof(testObject)));
    }

    [Fact]
    public void Guard_ThrowIfNullExceptionType_ObjectIsNull_ErrorThrown()
    {
        Object testObject = null;

        Should.Throw<ArgumentNullException>(() => Guard.ThrowIfNull(testObject, typeof(ArgumentNullException), nameof(testObject)));
    }

    [Fact]
    public void Guard_ThrowIfNullOrEmpty_StringNotNullOrEmpty_NoErrorThrown()
    {
        String testString = "test string";

        Should.NotThrow( () => Guard.ThrowIfNullOrEmpty(testString, nameof(testString)));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Guard_ThrowIfNullOrEmpty_StringIsNullOrEmpty_ErrorThrown(String testString)
    {
        Should.Throw<ArgumentNullException>( () => Guard.ThrowIfNullOrEmpty(testString, nameof(testString)));
    }

    [Fact]
    public void Guard_ThrowIfNullOrEmpty_StringArrayNotNullOrEmpty_NoErrorThrown()
    {
        String[] testStringArray = new[] {"testString"};

        Should.NotThrow( () => Guard.ThrowIfNullOrEmpty(testStringArray, nameof(testStringArray)));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Guard_ThrowIfNullOrEmpty_StringArrayIsNullOrEmpty_ErrorThrown(String testString)
    {
        String[] testStringArray = testString == String.Empty ? new String[]{} : null;

        Should.Throw<ArgumentNullException>( () => Guard.ThrowIfNullOrEmpty(testStringArray, nameof(testStringArray)));
    }

    [Fact]
    public void Guard_ThrowIfNullOrEmptyWithExceptionType_StringNotNullOrEmpty_NoErrorThrown()
    {
        String testString = "test string";

        Should.NotThrow(() => Guard.ThrowIfNullOrEmpty(testString,typeof(Exception), nameof(testString)));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Guard_ThrowIfNullOrEmptyWithExceptionType_StringIsNullOrEmpty_ErrorThrown(String testString)
    {
        Should.Throw<ArgumentNullException>(() => Guard.ThrowIfNullOrEmpty(testString, typeof(ArgumentNullException), nameof(testString)));
    }

    [Fact]
    public void Guard_ThrowIfNullOrEmptyWithExceptionType_StringArrayNotNullOrEmpty_NoErrorThrown()
    {
        String[] testStringArray = new[] { "testString" };

        Should.NotThrow(() => Guard.ThrowIfNullOrEmpty(testStringArray, typeof(Exception), nameof(testStringArray)));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Guard_ThrowIfNullOrEmptyWithExceptionType_StringArrayIsNullOrEmpty_ErrorThrown(String testString)
    {
        String[] testStringArray = testString == String.Empty ? new String[] { } : null;

        Should.Throw<ArgumentNullException>(() => Guard.ThrowIfNullOrEmpty(testStringArray, typeof(ArgumentNullException), nameof(testStringArray)));
    }

    [Fact]
    public void Guard_ThrowIfInvalidGuid_NoErrorThrown()
    {
        Guid testGuid = Guid.Parse("F78C8A4A-C950-4A61-9AB2-0FE4438D7165");

        Should.NotThrow( () => Guard.ThrowIfInvalidGuid(testGuid, nameof(testGuid)));
    }

    [Fact]
    public void Guard_ThrowIfInvalidGuid_EmptyGuid_NoErrorThrown()
    {
        Guid testGuid = Guid.Empty;

        Should.Throw<ArgumentNullException>( () => Guard.ThrowIfInvalidGuid(testGuid, nameof(testGuid)));
    }

    [Fact]
    public void Guard_ThrowIfInvalidGuidWithExceptionType_NoErrorThrown()
    {
        Guid testGuid = Guid.Parse("F78C8A4A-C950-4A61-9AB2-0FE4438D7165");

        Should.NotThrow(() => Guard.ThrowIfInvalidGuid(testGuid,typeof(Exception), nameof(testGuid)));
    }

    [Fact]
    public void Guard_ThrowIfInvalidGuidWithExceptionType_EmptyGuid_NoErrorThrown()
    {
        Guid testGuid = Guid.Empty;

        Should.Throw<ArgumentNullException>(() => Guard.ThrowIfInvalidGuid(testGuid, typeof(ArgumentNullException), nameof(testGuid)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void Guard_ThrowIfNegative_Int32_NoErrorThrown(Int32 testValue)
    {
        Should.NotThrow( () => Guard.ThrowIfNegative(testValue, nameof(testValue)));
    }

    [Fact]
    public void Guard_ThrowIfNegative_NegativeInt32_ErrorThrown()
    {
        Int32 testValue = -1;
        Should.Throw<ArgumentOutOfRangeException>( () => Guard.ThrowIfNegative(testValue, nameof(testValue)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void Guard_ThrowIfNegative_Int32WithExceptionType_NoErrorThrown(Int32 testValue)
    {
        Should.NotThrow(() => Guard.ThrowIfNegative(testValue, typeof(Exception), nameof(testValue)));
    }

    [Fact]
    public void Guard_ThrowIfNegative_NegativeInt32WithExceptionType_ErrorThrown()
    {
        Int32 testValue = -1;
        Should.Throw<ArgumentOutOfRangeException>(() => Guard.ThrowIfNegative(testValue, typeof(ArgumentOutOfRangeException), nameof(testValue)));
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(1.1)]
    public void Guard_ThrowIfNegative_Decimal_NoErrorThrown(Decimal testValue)
    {
        Should.NotThrow( () => Guard.ThrowIfNegative(testValue, nameof(testValue)));
    }

    [Fact]
    public void Guard_ThrowIfNegative_NegativeDecimal_ErrorThrown()
    {
        Decimal testValue = -1;
        Should.Throw<ArgumentOutOfRangeException>( () => Guard.ThrowIfNegative(testValue, nameof(testValue)));
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(1.1)]
    public void Guard_ThrowIfNegative_Double_NoErrorThrown(Double testValue)
    {
        Should.NotThrow( () => Guard.ThrowIfNegative(testValue, nameof(testValue)));
    }

    [Fact]
    public void Guard_ThrowIfNegative_NegativeDouble_ErrorThrown()
    {
        Double testValue = -1;
        Should.Throw<ArgumentOutOfRangeException>( () => Guard.ThrowIfNegative(testValue, nameof(testValue)));
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(1.1)]
    public void Guard_ThrowIfNegativeWithExceptionType_Decimal_NoErrorThrown(Decimal testValue)
    {
        Should.NotThrow(() => Guard.ThrowIfNegative(testValue, typeof(Exception), nameof(testValue)));
    }

    [Fact]
    public void Guard_ThrowIfNegativeWithExceptionType_NegativeDecimal_ErrorThrown()
    {
        Decimal testValue = -1;
        Should.Throw<ArgumentOutOfRangeException>(() => Guard.ThrowIfNegative(testValue, typeof(ArgumentOutOfRangeException), nameof(testValue)));
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(1.1)]
    public void Guard_ThrowIfNegativeWithExceptionType_Double_NoErrorThrown(Double testValue)
    {
        Should.NotThrow(() => Guard.ThrowIfNegative(testValue, typeof(Exception), nameof(testValue)));
    }

    [Fact]
    public void Guard_ThrowIfNegativeWithExceptionType_NegativeDouble_ErrorThrown()
    {
        Double testValue = -1;
        Should.Throw<ArgumentOutOfRangeException>(() => Guard.ThrowIfNegative(testValue, typeof(ArgumentOutOfRangeException), nameof(testValue)));
    }


    [Theory]
    [InlineData(-1)]
    [InlineData(1)]
    public void Guard_ThrowIfZero_Int32_NoErrorThrown(Int32 testValue)
    {
        Should.NotThrow( () => Guard.ThrowIfZero(testValue, nameof(testValue)));
    }

    [Fact]
    public void Guard_ThrowIfZero_ZeroInt32_ErrorThrown()
    {
        Int32 testValue = 0;
        Should.Throw<ArgumentOutOfRangeException>( () => Guard.ThrowIfZero(testValue, nameof(testValue)));
    }

    [Theory]
    [InlineData(-1.1)]
    [InlineData(1.1)]
    public void Guard_ThrowIfZero_Decimal_NoErrorThrown(Decimal testValue)
    {
        Should.NotThrow( () => Guard.ThrowIfZero(testValue, nameof(testValue)));
    }

    [Fact]
    public void Guard_ThrowIfZero_ZeroDecimal_ErrorThrown()
    {
        Decimal testValue = 0;
        Should.Throw<ArgumentOutOfRangeException>( () => Guard.ThrowIfZero(testValue, nameof(testValue)));
    }

    [Theory]
    [InlineData(-1.1)]
    [InlineData(1.1)]
    public void Guard_ThrowIfZero_Double_NoErrorThrown(Double testValue)
    {
        Should.NotThrow( () => Guard.ThrowIfZero(testValue, nameof(testValue)));
    }

    [Fact]
    public void Guard_ThrowIfZero_ZeroDouble_ErrorThrown()
    {
        Double testValue = 0;
        Should.Throw<ArgumentOutOfRangeException>( () => Guard.ThrowIfZero(testValue, nameof(testValue)));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1)]
    public void Guard_ThrowIfZeroWithExceptionType_Int32_NoErrorThrown(Int32 testValue)
    {
        Should.NotThrow(() => Guard.ThrowIfZero(testValue,typeof(Exception), nameof(testValue)));
    }

    [Fact]
    public void Guard_ThrowIfZeroWithExceptionType_ZeroInt32_ErrorThrown()
    {
        Int32 testValue = 0;
        Should.Throw<ArgumentOutOfRangeException>(() => Guard.ThrowIfZero(testValue, typeof(ArgumentOutOfRangeException), nameof(testValue)));
    }

    [Theory]
    [InlineData(-1.1)]
    [InlineData(1.1)]
    public void Guard_ThrowIfZeroWithExceptionType_Decimal_NoErrorThrown(Decimal testValue)
    {
        Should.NotThrow(() => Guard.ThrowIfZero(testValue, typeof(Exception), nameof(testValue)));
    }

    [Fact]
    public void Guard_ThrowIfZeroWithExceptionType_ZeroDecimal_ErrorThrown()
    {
        Decimal testValue = 0;
        Should.Throw<ArgumentOutOfRangeException>(() => Guard.ThrowIfZero(testValue, typeof(ArgumentOutOfRangeException), nameof(testValue)));
    }

    [Theory]
    [InlineData(-1.1)]
    [InlineData(1.1)]
    public void Guard_ThrowIfZeroWithExceptionType_Double_NoErrorThrown(Double testValue)
    {
        Should.NotThrow(() => Guard.ThrowIfZero(testValue, typeof(Exception), nameof(testValue)));
    }

    [Fact]
    public void Guard_ThrowIfZeroWithExceptionType_ZeroDouble_ErrorThrown()
    {
        Double testValue = 0;
        Should.Throw<ArgumentOutOfRangeException>(() => Guard.ThrowIfZero(testValue, typeof(ArgumentOutOfRangeException), nameof(testValue)));
    }


    [Fact]
    public void Guard_ThrowIfInvalidEnum_NoErrorThrown()
    {
        GuardTestEnum testValue = GuardTestEnum.First;

        Should.NotThrow( () => Guard.ThrowIfInvalidEnum(typeof(GuardTestEnum), testValue, nameof(testValue)));
    }

    [Fact]
    public void Guard_ThrowIfInvalidEnum_InvalidValue_ErrorThrown()
    {
        GuardTestEnum testValue = (GuardTestEnum) 99;

        Should.Throw<ArgumentOutOfRangeException>( () => Guard.ThrowIfInvalidEnum(typeof(GuardTestEnum), testValue, nameof(testValue)));
    }

    [Fact]
    public void Guard_ThrowIfInvalidEnumWithExceptionType_NoErrorThrown()
    {
        GuardTestEnum testValue = GuardTestEnum.First;

        Should.NotThrow(() => Guard.ThrowIfInvalidEnum(typeof(GuardTestEnum), testValue,typeof(Exception), nameof(testValue)));
    }

    [Fact]
    public void Guard_ThrowIfInvalidEnumWithExceptionType_InvalidValue_ErrorThrown()
    {
        GuardTestEnum testValue = (GuardTestEnum)99;

        Should.Throw<ArgumentOutOfRangeException>(() => Guard.ThrowIfInvalidEnum(typeof(GuardTestEnum), testValue, typeof(ArgumentOutOfRangeException), nameof(testValue)));
    }

    [Fact]
    public void Guard_ThrowIfInvalidDate_NoErrorThrown()
    {
        DateTime testValue = new(2018,10,21,1,2,3);

        Should.NotThrow( () => Guard.ThrowIfInvalidDate(testValue, nameof(testValue)));
    }

    [Fact]
    public void Guard_ThrowIfInvalidDate_InvalidDateValue_ErrorThrown()
    {
        DateTime testValue = DateTime.MinValue;

        Should.Throw<ArgumentNullException>( () => Guard.ThrowIfInvalidDate(testValue, nameof(testValue)));
    }

    [Fact]
    public void Guard_ThrowIfInvalidDateWithExceptionType_NoErrorThrown()
    {
        DateTime testValue = new(2018, 10, 21, 1, 2, 3);

        Should.NotThrow(() => Guard.ThrowIfInvalidDate(testValue, typeof(Exception), nameof(testValue)));
    }

    [Fact]
    public void Guard_ThrowIfInvalidDateWithExceptionType_InvalidDateValue_ErrorThrown()
    {
        DateTime testValue = DateTime.MinValue;

        Should.Throw<ArgumentNullException>(() => Guard.ThrowIfInvalidDate(testValue, typeof(ArgumentNullException), nameof(testValue)));
    }

    [Fact]
    public void Guard_ThrowIfContainsNullOrEmpty_NoErrorThrown()
    {
        String[] testArray = new[] {"testString"};

        Should.NotThrow( () => Guard.ThrowIfContainsNullOrEmpty(testArray, nameof(testArray)));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Guard_ThrowIfContainsNullOrEmpty_ContainsNullOrEmpty_ErrorThrown(String testValue)
    {
        List<String> testArray = new List<String>();
        testArray.Add("String1");
        testArray.Add(testValue);
            
        Should.Throw<ArgumentNullException>( () => Guard.ThrowIfContainsNullOrEmpty(testArray.ToArray(), nameof(testValue)));
    }

    [Fact]
    public void Guard_ThrowIfContainsNullOrEmptyWithExceptionType_NoErrorThrown()
    {
        String[] testArray = new[] { "testString" };

        Should.NotThrow(() => Guard.ThrowIfContainsNullOrEmpty(testArray,typeof(Exception), nameof(testArray)));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Guard_ThrowIfContainsNullOrEmptyWithExceptionType_ContainsNullOrEmpty_ErrorThrown(String testValue)
    {
        List<String> testArray = new List<String>();
        testArray.Add("String1");
        testArray.Add(testValue);

        Should.Throw<ArgumentNullException>(() => Guard.ThrowIfContainsNullOrEmpty(testArray.ToArray(), typeof(ArgumentNullException), nameof(testValue)));
    }

    [Fact]
    public void Guard_ThrowIfNullOrEmpty_ByteArrayNotNullOrEmpty_NoErrorThrown(){
        String testString = "test";
        Byte[] byteArray = Encoding.UTF8.GetBytes(testString);
            
        Should.NotThrow(() => Guard.ThrowIfNullOrEmpty(byteArray, nameof(byteArray)));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Guard_ThrowIfNullOrEmpty_ByteArrayIsNullOrEmpty_ErrorThrown(String testString)
    {
        Byte[] testByteArray;

        testByteArray = testString == String.Empty ? new Byte[] { } : null;


        Should.Throw<ArgumentNullException>(() => Guard.ThrowIfNullOrEmpty(testByteArray, nameof(testByteArray)));
    }
}