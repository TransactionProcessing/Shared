namespace Shared.Tests;

using System;
using Shouldly;
using ValueObjects;
using Xunit;

public partial class SharedTests
{
    [Theory]
    [InlineData(1.00)]
    [InlineData(1.59)]
    [InlineData(100.00)]
    public void PositiveMoney_CanBeCreated_IsCreated(Decimal moneyValue)
    {
        PositiveMoney money = PositiveMoney.Create(Money.Create(moneyValue));

        money.ShouldNotBeNull();
        money.Value.ShouldBe(moneyValue);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void PositiveMoney_NonPositiveAmountRejected_ErrorThrown(Decimal moneyValue)
    {
        Should.Throw<ArgumentOutOfRangeException>(() => PositiveMoney.Create(Money.Create(moneyValue)));
    }
}