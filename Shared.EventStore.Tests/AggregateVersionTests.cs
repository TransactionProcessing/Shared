namespace Shared.EventStore.Tests;

using System;
using Aggregate;
using Shouldly;
using Xunit;

public class AggregateVersionTests{
    [Fact]
    public void AggregateVersion_CreateFrom_VersionCreated(){
        AggregateVersion v= AggregateVersion.CreateFrom(-1);
        v.Value.ShouldBe(-1);
    }
    
    [Fact]
    public void AggregateVersion_CreateNew_VersionCreated()
    {
        AggregateVersion v = AggregateVersion.CreateNew();
        v.Value.ShouldBe(0);
    }

    [Fact]
    public void AggregateVersion_Parse_VersionCreated(){
        AggregateVersion v = AggregateVersion.Parse("1");
        v.Value.ShouldBe(1);
    }

    [Fact]
    public void AggregateVersion_ImplicitConversion_Numeric_VersionCreated1(){
        AggregateVersion v = 1;
        v.Value.ShouldBe(1);
    }

    [Fact]
    public void AggregateVersion_ImplicitConversion_AggregateVersion_VersionCreated1()
    {
        AggregateVersion version1 = AggregateVersion.CreateFrom(1);
        Int64 v = version1;
        v.ShouldBe(1);
    }

    [Theory]
    [InlineData(1,1, true)]
    [InlineData(1, 0, false)]
    [InlineData(0, 1,false)]
    public void AggregateVersion_Equals_ResultAsExpected(Int32 v1, Int32 v2, Boolean expectedResult)
    {
        AggregateVersion version1 = AggregateVersion.CreateFrom(v1);
        AggregateVersion version2 = AggregateVersion.CreateFrom(v2);
        version1.Equals(version2).ShouldBe(expectedResult);
    }

    [Theory]
    [InlineData(1, 1, true)]
    [InlineData(1, 0, false)]
    [InlineData(0, 1, false)]
    public void AggregateVersion_EqualsOperator_ResultAsExpected(Int32 v1, Int32 v2, Boolean expectedResult)
    {
        AggregateVersion version1 = AggregateVersion.CreateFrom(v1);
        AggregateVersion version2 = AggregateVersion.CreateFrom(v2);
        (version1==version2).ShouldBe(expectedResult);
    }

    [Theory]
    [InlineData(1, 1, true)]
    [InlineData(1, 0, false)]
    [InlineData(0, 1, false)]
    public void AggregateVersion_Equals_Object_ResultAsExpected(Int32 v1, Int32 v2, Boolean expectedResult)
    {
        AggregateVersion version1 = AggregateVersion.CreateFrom(v1);
        AggregateVersion version2 = AggregateVersion.CreateFrom(v2);
        Object o = version2;
        version1.Equals(o).ShouldBe(expectedResult);
    }

    [Fact]
    public void AggregateVersion_Equals_ObjectIsNull_ResultAsExpected()
    {
        AggregateVersion version1 = AggregateVersion.CreateFrom(1);
        Object o = null;
        version1.Equals(o).ShouldBeFalse();
    }

    [Theory]
    [InlineData(1, 1, false)]
    [InlineData(1, 0, true)]
    [InlineData(0, 1, true)]
    public void AggregateVersion_NotEqualsOperator_ResultAsExpected(Int32 v1, Int32 v2, Boolean expectedResult)
    {
        AggregateVersion version1 = AggregateVersion.CreateFrom(v1);
        AggregateVersion version2 = AggregateVersion.CreateFrom(v2);
        (version1 != version2).ShouldBe(expectedResult);
    }

    [Theory]
    [InlineData(1, 1, 0)]
    [InlineData(1, 0, 1)]
    [InlineData(0, 1, -1)]
    public void AggregateVersion_CompareTo_ResultAsExpected(Int32 v1, Int32 v2, Int32 expectedResult)
    {
        AggregateVersion version1 = AggregateVersion.CreateFrom(v1);
        AggregateVersion version2 = AggregateVersion.CreateFrom(v2);
        version1.CompareTo(version2).ShouldBe(expectedResult);
    }

    [Fact]
    public void AggregateVersion_GetHashCode_HashCodeReturned()
    {
        AggregateVersion v = AggregateVersion.CreateFrom(100);
        var hashcode= v.GetHashCode();
        hashcode.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void AggregateVersion_ToString_StringReturned()
    {
        AggregateVersion v = AggregateVersion.CreateFrom(100);
        var toString = v.ToString();
        toString.ShouldBe("100");
    }
}