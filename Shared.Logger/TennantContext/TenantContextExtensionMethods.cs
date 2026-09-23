namespace Shared.Logger.TennantContext;

public static class TenantContextExtensionMethods
{
    [Obsolete("Use SetCorrelationId(this TenantContext context,CorrelationId correlationId) overload instead")]
    public static void SetCorrelationId(this TenantContext context,
                                        Guid correlationId)
    {
        context.CorrelationId = CorrelationId.From(correlationId);
    }

    public static void SetCorrelationId(this TenantContext context,
                                        CorrelationId correlationId)
    {
        context.CorrelationId = correlationId;
    }

    public static void Initialise(this TenantContext tenantContext,
                                  TenantIdentifiers identifiers,
                                  Boolean perTenantLogsEnabled)
    {
        tenantContext.EstateId = identifiers.EstateId;
        tenantContext.MerchantId = identifiers.MerchantId;
        tenantContext.PerTenantLogsEnabled = perTenantLogsEnabled;
    }
}