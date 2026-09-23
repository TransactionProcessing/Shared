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