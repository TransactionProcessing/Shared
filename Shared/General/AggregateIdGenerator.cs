namespace Shared.General;

using System;
using System.Diagnostics.CodeAnalysis;
using Extensions;

[ExcludeFromCodeCoverage]
public static class AggregateIdGenerator
{
    public static Guid CalculateSettlementAggregateId(DateTime settlementDate,
                                                      Guid estateId)
    {
        Guid aggregateId = GuidCalculator.Combine(estateId, settlementDate.ToGuid());
        return aggregateId;
    }
}