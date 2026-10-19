# TransactionProcessing.ApiDefaults

Shared URL-based API versioning, per-version OpenAPI documents, and Scalar UI for .NET 10 Minimal APIs.

## Setup

```csharp
using Asp.Versioning;
using TransactionProcessing.ApiDefaults;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddApiDefaults(options =>
{
    options.Title = "Transaction API";
    options.Description = "Transaction processing services";
});

var app = builder.Build();
app.UseApiDefaults();

var api = app.NewVersionedApi("Transactions");
var transactions = api
    .MapGroup("/api/v{version:apiVersion}/transactions")
    .HasApiVersion(ApiVersions.V1);
transactions.MapGet("/", GetTransactions).MapToApiVersion(ApiVersions.V1);

app.Run();
```

`ApiVersions.V1`, `.V2`, and `.V3` are convenience values. Additional API versions can be declared with `new ApiVersion(major, minor)`; OpenAPI documents and Scalar definitions are discovered from the mapped endpoints.

## Version fallback

For a route whose behavior only changes occasionally, map one handler per version where the behavior changes. The route uses the newest registered handler at or before the requested version:

```csharp
transactions.MapGetWithVersionFallback("/", new Dictionary<ApiVersion, Func<HttpContext, IResult>>
{
    [ApiVersions.V1] = _ => Results.Ok("V1 response"),
    [ApiVersions.V2] = _ => Results.Ok("V2 response")
});
```

If the route group also declares V3, V3 requests use the V2 handler automatically. Add a V3 entry only when the behavior changes; that entry then serves V3 and later versions until another handler is registered. `MapGetWithVersionFallbackAsync` provides the same behavior for asynchronous handlers. Versions earlier than the first registered handler return 404.

## Routes

- API endpoints use explicit URL versions, such as `/api/v1/transactions`.
- OpenAPI documents are served at `/openapi/v1.json`, `/openapi/v2.json`, and the route for each additional discovered version.
- Scalar is available at `/scalar` (redirects to `/scalar/` for relative document URLs).

`ApiDefaultsOptions.Title` defaults to `API`; `Description` is optional. This package is independent of the existing `Shared` packages, so projects that do not reference it do not acquire its API-related dependencies.
