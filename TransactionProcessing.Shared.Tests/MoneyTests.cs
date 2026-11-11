using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TransactionProcessing.Shared.Tests;

using System.Diagnostics.PerformanceData;
using Shouldly;
using ValueObjects;
using Xunit;

public partial class SharedTests
{
    public static TheoryData<Decimal> MoneyCreationCases => new()
    {
        { 0m },
        { 1.00m },
        { 1.59m },
        { 100.00m },
        { -1.59m }
    };

    public static TheoryData<Decimal, Decimal, Decimal> MoneyAdditionCases => new()
    {
        { 1m, 100m, 101m },
        { 0m, 100m, 100m },
        { -1m, 100m, 99m },
        { 1.01m, 100.01m, 101.02m },
        { 0.01m, 100.01m, 100.02m },
        { -1.01m, 100.01m, 99m }
    };

    public static TheoryData<Decimal, Decimal, Decimal> MoneySubtractionCases => new()
    {
        { 1m, 100m, -99m },
        { 0m, 100m, -100m },
        { -1m, 100m, -101m },
        { 1.01m, 100.01m, -99m },
        { 0.01m, 100.01m, -100m },
        { -1.01m, 100.01m, -101.02m }
    };

    [Theory]
    [MemberData(nameof(MoneyCreationCases))]
    public void Money_CanBeCreated_IsCreated(Decimal moneyValue)
    {
        Money money = Money.Create(moneyValue);

        money.ShouldNotBeNull();
        money.Value.ShouldBe(moneyValue);
    }

    [Theory]
    [MemberData(nameof(MoneyAdditionCases))]
    public void Money_AddOperator_ValueAdded(Decimal initialValue, Decimal valueToAdd, Decimal expectedResult)
    {
        Money money1 = Money.Create(initialValue);
        Money money2 = Money.Create(valueToAdd);

        var result = money1 + money2;
        result.Value.ShouldBe(expectedResult);
    }

    [Theory]
    [MemberData(nameof(MoneySubtractionCases))]
    public void Money_SubtractOperator_ValueAdded(Decimal initialValue, Decimal valueToSubtract, Decimal expectedResult)
    {
        Money money1 = Money.Create(initialValue);
        Money money2 = Money.Create(valueToSubtract);

        var result = money1 - money2;
        result.Value.ShouldBe(expectedResult);
    }
}
