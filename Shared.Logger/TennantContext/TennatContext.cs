namespace Shared.Logger.TennantContext;

public class TenantContext
{
    #region Fields
    public static readonly String KeyNameCorrelationId = "correlationId";

    private static readonly AsyncLocal<TenantContext> Current = new AsyncLocal<TenantContext>();

    #endregion

    #region Properties

    public CorrelationId CorrelationId { get; internal set; } = CorrelationId.New();

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