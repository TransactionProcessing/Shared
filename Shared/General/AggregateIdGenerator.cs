namespace Shared.General
{
    using System;
    using Extensions;

    public static class AggregateIdGenerator
    {
        public static Guid CalculateSettlementAggregateId(DateTime settlementDate,
                                                          Guid estateId)
        {
            Guid aggregateId = GuidCalculator.Combine(estateId, settlementDate.ToGuid());
            return aggregateId;
        }
    }
}