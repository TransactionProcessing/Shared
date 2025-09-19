using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Tests;

using System.Diagnostics.PerformanceData;
using Shouldly;
using ValueObjects;
using Xunit;

public partial class SharedTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1.00)]
    [InlineData(1.59)]
    [InlineData(100.00)]
    [InlineData(-1.59)]
    public void Money_CanBeCreated_IsCreated(Decimal moneyValue)
    {
        Money money = Money.Create(moneyValue);

        money.ShouldNotBeNull();
        money.Value.ShouldBe(moneyValue);
    }

    [Theory]
    [InlineData(1,100,101)]
    [InlineData(0, 100, 100)]
    [InlineData(-1, 100, 99)]
    [InlineData(1.01, 100.01, 101.02)]
    [InlineData(0.01, 100.01, 100.02)]
    [InlineData(-1.01, 100.01, 99)]
    public void Money_AddOperator_ValueAdded(Decimal initialValue, Decimal valueToAdd, Decimal expectedResult)
    {
        Money money1 = Money.Create(initialValue);
        Money money2 = Money.Create(valueToAdd);

        var result = money1 + money2;
        result.Value.ShouldBe(expectedResult);
    }

    [Theory]
    [InlineData(1, 100, -99)]
    [InlineData(0, 100, -100)]
    [InlineData(-1, 100, -101)]
    [InlineData(1.01, 100.01, -99)]
    [InlineData(0.01, 100.01, -100)]
    [InlineData(-1.01, 100.01, -101.02)]
    public void Money_SubtractOperator_ValueAdded(Decimal initialValue, Decimal valueToSubtract, Decimal expectedResult)
    {
        Money money1 = Money.Create(initialValue);
        Money money2 = Money.Create(valueToSubtract);

        var result = money1 - money2;
        result.Value.ShouldBe(expectedResult);
    }
}