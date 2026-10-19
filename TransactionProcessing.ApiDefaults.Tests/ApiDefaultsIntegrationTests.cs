using System.Net;
using System.Text.Json;
using Asp.Versioning;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shouldly;
using TransactionProcessing.ApiDefaults;
using Xunit;

namespace TransactionProcessing.ApiDefaults.Tests;

public sealed class ApiDefaultsIntegrationTests
{
    [Fact]
    public void ApiDefaultsOptions_HasExpectedDefaults()
    {
        ApiDefaultsOptions options = new();

        options.Title.ShouldBe("API");
        options.Description.ShouldBeNull();
    }

    [Fact]
    public async Task Endpoints_AreRoutedByExplicitUrlVersion()
    {
        await using var app = await CreateApp();
        using var client = app.GetTestClient();

        (await client.GetStringAsync("/api/v1/transactions/shared", TestContext.Current.CancellationToken)).ShouldBe("shared");
        (await client.GetStringAsync("/api/v2/transactions/shared", TestContext.Current.CancellationToken)).ShouldBe("shared");
        (await client.GetStringAsync("/api/v1/transactions/v1", TestContext.Current.CancellationToken)).ShouldBe("v1");
        (await client.GetStringAsync("/api/v2/transactions/v2", TestContext.Current.CancellationToken)).ShouldBe("v2");
    }

    [Fact]
    public async Task VersionSpecificEndpoints_AreUnavailableForOtherVersions()
    {
        await using var app = await CreateApp();
        using var client = app.GetTestClient();

        (await client.GetAsync("/api/v1/transactions/v2", TestContext.Current.CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await client.GetAsync("/api/v2/transactions/v1", TestContext.Current.CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task VersionFallback_UsesLatestPriorHandlerUntilOverridden()
    {
        await using (var app = await CreateApp(includeV3FallbackOverride: false))
        using (var client = app.GetTestClient())
        {
            (await client.GetStringAsync("/api/v3/transactions", TestContext.Current.CancellationToken)).ShouldBe("v2");
            (await client.GetStringAsync("/api/v3/transactions/async", TestContext.Current.CancellationToken)).ShouldBe("async-v2");
        (await client.GetAsync("/api/v3/transactions/future-only", TestContext.Current.CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        }

        await using (var app = await CreateApp(includeV3FallbackOverride: true))
        using (var client = app.GetTestClient())
        {
            (await client.GetStringAsync("/api/v3/transactions", TestContext.Current.CancellationToken)).ShouldBe("v3");
        }
    }

    [Fact]
    public async Task UnsupportedAndMissingVersions_AreNotRouted()
    {
        await using var app = await CreateApp();
        using var client = app.GetTestClient();

        (await client.GetAsync("/api/v4/transactions/shared", TestContext.Current.CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await client.GetAsync("/api/transactions/shared", TestContext.Current.CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Responses_ReportSupportedAndDeprecatedVersions()
    {
        await using var app = await CreateApp();
        using var client = app.GetTestClient();

        using var response = await client.GetAsync("/api/v1/transactions/shared", TestContext.Current.CancellationToken);

        response.Headers.GetValues("api-supported-versions").Single().Split(',').Select(version => version.Trim()).ShouldContain("1.0");
        response.Headers.GetValues("api-supported-versions").Single().Split(',').Select(version => version.Trim()).ShouldContain("3.0");
        response.Headers.GetValues("api-deprecated-versions").Single().Split(',').Select(version => version.Trim()).ShouldContain("2.0");
    }

    [Fact]
    public async Task OpenApiDocuments_ContainOnlyApplicableEndpointsAndMetadata()
    {
        await using var app = await CreateApp();
        using var client = app.GetTestClient();

        using JsonDocument v1 = JsonDocument.Parse(await client.GetStringAsync("/openapi/v1.json", TestContext.Current.CancellationToken));
        using JsonDocument v2 = JsonDocument.Parse(await client.GetStringAsync("/openapi/v2.json", TestContext.Current.CancellationToken));
        using JsonDocument v3 = JsonDocument.Parse(await client.GetStringAsync("/openapi/v3.json", TestContext.Current.CancellationToken));

        v1.RootElement.GetProperty("info").GetProperty("title").GetString().ShouldBe("Test Transactions API");
        v1.RootElement.GetProperty("info").GetProperty("description").GetString().ShouldBe("API defaults integration test.");
        v1.RootElement.GetProperty("paths").EnumerateObject().Select(x => x.Name).ShouldContain("/api/v1/transactions/shared");
        v1.RootElement.GetProperty("paths").EnumerateObject().Select(x => x.Name).ShouldContain("/api/v1/transactions");
        v1.RootElement.GetProperty("paths").EnumerateObject().Select(x => x.Name).ShouldContain("/api/v1/transactions/v1");
        v1.RootElement.GetProperty("paths").EnumerateObject().Select(x => x.Name).ShouldNotContain("/api/v1/transactions/v2");
        v1.RootElement.GetProperty("paths").EnumerateObject().Select(x => x.Name).ShouldNotContain("/api/v1/transactions/async");
        v2.RootElement.GetProperty("paths").EnumerateObject().Select(x => x.Name).ShouldContain("/api/v2/transactions/shared");
        v2.RootElement.GetProperty("paths").EnumerateObject().Select(x => x.Name).ShouldContain("/api/v2/transactions/v2");
        v2.RootElement.GetProperty("paths").EnumerateObject().Select(x => x.Name).ShouldContain("/api/v2/transactions/async");
        v2.RootElement.GetProperty("paths").EnumerateObject().Select(x => x.Name).ShouldNotContain("/api/v2/transactions/v1");
        v3.RootElement.GetProperty("paths").EnumerateObject().Select(x => x.Name).ShouldContain("/api/v3/transactions/shared");
        v3.RootElement.GetProperty("paths").EnumerateObject().Select(x => x.Name).ShouldContain("/api/v3/transactions");
        v3.RootElement.GetProperty("paths").EnumerateObject().Select(x => x.Name).ShouldContain("/api/v3/transactions/v3");
        v3.RootElement.GetProperty("paths").EnumerateObject().Select(x => x.Name).ShouldContain("/api/v3/transactions/async");
        v1.RootElement.GetProperty("paths").EnumerateObject().Select(x => x.Name).ShouldNotContain("/api/v1/transactions/future-only");
        v2.RootElement.GetProperty("paths").EnumerateObject().Select(x => x.Name).ShouldNotContain("/api/v2/transactions/future-only");
        v3.RootElement.GetProperty("paths").EnumerateObject().Select(x => x.Name).ShouldNotContain("/api/v3/transactions/future-only");
    }

    [Fact]
    public async Task Scalar_ListsAllDiscoveredApiVersions()
    {
        await using var app = await CreateApp();
        using var client = app.GetTestClient();

        using var rootResponse = await client.GetAsync("/scalar", TestContext.Current.CancellationToken);
        using var response = await client.GetAsync("/scalar/", TestContext.Current.CancellationToken);
        string html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        rootResponse.StatusCode.ShouldBe(HttpStatusCode.Found);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        html.ShouldContain("\"url\":\"openapi/v1.json\"");
        html.ShouldContain("\"url\":\"openapi/v2.json\"");
        html.ShouldContain("\"url\":\"openapi/v3.json\"");
    }

    private static async Task<WebApplication> CreateApp(bool includeV3FallbackOverride = false)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Development
        });
        builder.WebHost.UseTestServer();
        builder.Services.AddApiDefaults(options =>
        {
            options.Title = "Test Transactions API";
            options.Description = "API defaults integration test.";
        });

        WebApplication app = builder.Build();
        app.UseApiDefaults();

        var api = app.NewVersionedApi("Transactions");
        var transactions = api
            .MapGroup("/api/v{version:apiVersion}/transactions")
            .HasApiVersion(ApiVersions.V1)
            .HasDeprecatedApiVersion(ApiVersions.V2)
            .HasApiVersion(ApiVersions.V3);

        var versionedResponses = new Dictionary<ApiVersion, Func<HttpContext, IResult>>
        {
            [ApiVersions.V1] = _ => Results.Text("v1"),
            [ApiVersions.V2] = _ => Results.Text("v2")
        };
        if (includeV3FallbackOverride)
        {
            versionedResponses[ApiVersions.V3] = _ => Results.Text("v3");
        }

        transactions.MapGetWithVersionFallback("/", versionedResponses);
        transactions.MapGetWithVersionFallbackAsync("/async", new Dictionary<ApiVersion, Func<HttpContext, CancellationToken, Task<IResult>>>
        {
            [ApiVersions.V2] = async (_, cancellationToken) =>
            {
                await Task.Yield();
                cancellationToken.ThrowIfCancellationRequested();
                return Results.Text("async-v2");
            }
        });
        transactions.MapGetWithVersionFallback("/future-only", new Dictionary<ApiVersion, Func<HttpContext, IResult>>
        {
            [new ApiVersion(4, 0)] = _ => Results.Text("future"),
        });

        transactions.MapGet("/shared", () => "shared");
        transactions.MapGet("/v1", () => "v1").MapToApiVersion(ApiVersions.V1);
        transactions.MapGet("/v2", () => "v2").MapToApiVersion(ApiVersions.V2);
        transactions.MapGet("/v3", () => "v3").MapToApiVersion(ApiVersions.V3);

        await app.StartAsync(TestContext.Current.CancellationToken);
        return app;
    }
}
