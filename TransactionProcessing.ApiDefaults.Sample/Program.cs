using Asp.Versioning;
using TransactionProcessing.ApiDefaults;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddApiDefaults(options =>
{
    options.Title = "Transaction API";
    options.Description = "Sample API demonstrating the shared API defaults.";
});

WebApplication app = builder.Build();
app.UseApiDefaults();

var api = app.NewVersionedApi("Transactions");
var transactions = api
    .MapGroup("/api/v{version:apiVersion}/transactions")
    .HasApiVersion(ApiVersions.V1)
    .HasApiVersion(ApiVersions.V2)
    .HasApiVersion(ApiVersions.V3)
    .HasApiVersion(ApiVersions.V4);

transactions.MapGetWithVersionFallback("/", new Dictionary<ApiVersion, Func<HttpContext, IResult>>
{
    [ApiVersions.V1] = _ => Results.Ok(new { Message = "V1 transaction response" }),
    [ApiVersions.V2] = _ => Results.Ok(new { Message = "V2 transaction response" })
});
transactions.MapGet("/shared", () => Results.Ok(new { Message = "Available in all supported versions" }));

app.Run();
