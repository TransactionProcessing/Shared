using Asp.Versioning;

namespace TransactionProcessing.ApiDefaults;

/// <summary>
/// Common API versions for endpoint declarations. OpenAPI discovery is independent of this type.
/// </summary>
public static class ApiVersions
{
    /// <summary>API version 1.0.</summary>
    public static readonly ApiVersion V1 = new(1, 0);

    /// <summary>API version 2.0.</summary>
    public static readonly ApiVersion V2 = new(2, 0);

    /// <summary>API version 3.0.</summary>
    public static readonly ApiVersion V3 = new(3, 0);
    public static readonly ApiVersion V4 = new(4, 0);
}
