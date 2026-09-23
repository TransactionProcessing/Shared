namespace Shared.Tests;

using System;
using Shouldly;
using Shared.Logger.TennantContext;
using Xunit;

public class CorrelationIdTests
{
    [Fact]
    public void New_CreatesNonEmptyCorrelationId()
    {
        CorrelationId correlationId = CorrelationId.New();

        correlationId.Value.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void From_PreservesTheProvidedGuid()
    {
        Guid value = Guid.NewGuid();

        CorrelationId correlationId = CorrelationId.From(value);

        correlationId.Value.ShouldBe(value);
    }

    [Fact]
    public void From_RejectsAnEmptyGuid()
    {
        Should.Throw<ArgumentException>(() => CorrelationId.From(Guid.Empty));
    }

    [Fact]
    public void Parse_CreatesCorrelationIdFromAValidGuid()
    {
        Guid value = Guid.NewGuid();

        CorrelationId correlationId = CorrelationId.Parse(value.ToString());

        correlationId.Value.ShouldBe(value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-guid")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public void Parse_RejectsInvalidOrEmptyValues(string value)
    {
        Should.Throw<ArgumentException>(() => CorrelationId.Parse(value));
    }

    [Fact]
    public void ToString_ReturnsTheGuidInNFormat()
    {
        Guid value = Guid.Parse("01234567-89ab-cdef-0123-456789abcdef");
        CorrelationId correlationId = CorrelationId.From(value);

        correlationId.ToString().ShouldBe("0123456789abcdef0123456789abcdef");
    }

    [Fact]
    public void EqualValuesAreEqual()
    {
        Guid value = Guid.NewGuid();

        CorrelationId first = CorrelationId.From(value);
        CorrelationId second = CorrelationId.From(value);

        first.ShouldBe(second);
    }
}
