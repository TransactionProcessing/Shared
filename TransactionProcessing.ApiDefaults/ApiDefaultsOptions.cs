namespace TransactionProcessing.ApiDefaults;

/// <summary>
/// Documentation metadata applied to every generated API version document.
/// </summary>
public sealed class ApiDefaultsOptions
{
    /// <summary>Gets or sets the API title.</summary>
    public string Title { get; set; } = "API";

    /// <summary>Gets or sets the API description.</summary>
    public string? Description { get; set; }
}
