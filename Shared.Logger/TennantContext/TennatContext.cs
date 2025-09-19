namespace Shared.Logger.TennantContext;

public record TenantIdentifiers(Guid EstateId,
                                Guid MerchantId)
{
    #region Methods

    public static TenantIdentifiers Default()
    {
        return new TenantIdentifiers(Guid.Empty, Guid.Empty);
    }

    #endregion
}

public class TenantContext
{
    #region Fields

    private static readonly AsyncLocal<TenantContext> Current = new AsyncLocal<TenantContext>();

    #endregion

    #region Properties

    public Guid CorrelationId { get; internal set; } = Guid.NewGuid();

    public static TenantContext CurrentTenant
    {
        get => TenantContext.Current?.Value;
        set => TenantContext.Current.Value = value;
    }

    public Guid EstateId { get; internal set; }

    public Boolean PerTenantLogsEnabled { get; internal set; }

    public Guid MerchantId { get; internal set; }

    #endregion
}

public static class TenantContextExtensionMethods
{
    #region Methods

    public static void SetCorrelationId(this TenantContext context,
                                        Guid correlationId)
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

    #endregion
}