using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Scalar.AspNetCore;

namespace TransactionProcessing.ApiDefaults;

/// <summary>
/// Registers and maps the shared API versioning and documentation defaults.
/// </summary>
public static class ApiDefaultsExtensions
{
    /// <summary>
    /// Registers URL-segment API versioning, version discovery, and OpenAPI generation.
    /// </summary>
    public static IServiceCollection AddApiDefaults(
        this IServiceCollection services,
        Action<ApiDefaultsOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.Configure(configure ?? (_ => { }));
        services.AddApiVersioning(options =>
        {
            options.ApiVersionReader = new UrlSegmentApiVersionReader();
            options.DefaultApiVersion = ApiVersions.V1;
            options.AssumeDefaultVersionWhenUnspecified = false;
            options.ReportApiVersions = true;
        }).AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        }).AddOpenApi(options =>
        {
            options.Document.AddDocumentTransformer((document, context, _) =>
            {
                ApiDefaultsOptions metadata = context.ApplicationServices
                    .GetRequiredService<IOptions<ApiDefaultsOptions>>()
                    .Value;
                document.Info.Title = metadata.Title;
                document.Info.Description = metadata.Description;
                return Task.CompletedTask;
            });
        });
        services.AddTransient<Microsoft.AspNetCore.Mvc.ApiExplorer.IApiDescriptionProvider, MinimumApiVersionApiDescriptionProvider>();

        return services;
    }

    /// <summary>
    /// Maps OpenAPI documents at <c>/openapi/{version}.json</c> and Scalar at <c>/scalar</c>.
    /// </summary>
    public static WebApplication UseApiDefaults(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.DescribeApiVersions();
        app.MapOpenApi().WithDocumentPerVersion();
        app.MapScalarApiReference("/scalar", (options, context) =>
        {
            ApiDefaultsOptions defaults = context.RequestServices
                .GetRequiredService<IOptions<ApiDefaultsOptions>>()
                .Value;
            IApiVersionDescriptionProvider versions = context.RequestServices
                .GetRequiredService<IApiVersionDescriptionProvider>();

            options.WithTitle(defaults.Title)
                .WithOpenApiRoutePattern("/openapi/{documentName}.json");

            foreach (ApiVersionDescription version in versions.ApiVersionDescriptions)
            {
                options.AddDocument(version.GroupName, $"{defaults.Title} {version.GroupName.ToUpperInvariant()}");
            }
        });

        return app;
    }
}
