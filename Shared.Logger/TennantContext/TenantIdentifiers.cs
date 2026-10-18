namespace Shared.Logger.TennantContext;

public record TenantIdentifiers(Guid EstateId,
                                Guid MerchantId)
{

    public static TenantIdentifiers Default()
    {
        return new TenantIdentifiers(Guid.Empty, Guid.Empty);
    }

}