using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Text.Json;
using TransactionProcessing.UnionResults;
using TransactionProcessing.UnionResults.AspNetCore;
using Xunit;

namespace TransactionProcessing.UnionResults.AspNetCore.Tests;

public sealed class FilterTests
{
    [Fact]
    public async Task Maps_union_success_and_failure_to_http_responses()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddUnionResults();
        await using var app = builder.Build();
        var api = app.MapGroup("/api").AddUnionResultFilter();
        api.MapGet("/ok", () => Result<string>.Ok("done"));
        api.MapGet("/error", () => Result<string>.Fail(new Error("Missing", "Not found", ErrorType.NotFound)));
        await app.StartAsync(TestContext.Current.CancellationToken);

        using var client = app.GetTestClient();
        var success = await client.GetAsync("/api/ok", TestContext.Current.CancellationToken);
        var failure = await client.GetAsync("/api/error", TestContext.Current.CancellationToken);
        Assert.Equal(System.Net.HttpStatusCode.OK, success.StatusCode);
        Assert.Equal(System.Net.HttpStatusCode.NotFound, failure.StatusCode);
        Assert.Equal("application/problem+json", failure.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Maps_ok_created_no_content_and_non_union_results()
    {
        await using var app = await StartApp(api =>
        {
            api.MapGet("/value", () => Result<string>.Ok("done"));
            api.MapGet("/ok-empty", () => Result.Ok());
            api.MapGet("/no-content", () => Result.NoContent());
            api.MapGet("/created", () => Result<string>.Created("created", "/items/42"));
            api.MapGet("/raw", () => "raw response");
        });
        using var client = app.GetTestClient();

        var value = await client.GetAsync("/api/value", TestContext.Current.CancellationToken);
        Assert.Equal(System.Net.HttpStatusCode.OK, value.StatusCode);
        Assert.Equal("\"done\"", await value.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));

        var empty = await client.GetAsync("/api/ok-empty", TestContext.Current.CancellationToken);
        Assert.Equal(System.Net.HttpStatusCode.OK, empty.StatusCode);
        Assert.Empty(await empty.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken));

        var noContent = await client.GetAsync("/api/no-content", TestContext.Current.CancellationToken);
        Assert.Equal(System.Net.HttpStatusCode.NoContent, noContent.StatusCode);

        var created = await client.GetAsync("/api/created", TestContext.Current.CancellationToken);
        Assert.Equal(System.Net.HttpStatusCode.Created, created.StatusCode);
        Assert.Equal("/items/42", created.Headers.Location?.OriginalString);
        Assert.Equal("\"created\"", await created.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));

        var raw = await client.GetAsync("/api/raw", TestContext.Current.CancellationToken);
        Assert.Equal("raw response", await raw.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Maps_categories_and_error_lists_to_problem_details()
    {
        await using var app = await StartApp(api =>
        {
            api.MapGet("/conflict", () => Result.Fail(Error.Conflict("conflict")));
            api.MapGet("/critical", () => Result.Fail(Error.Critical("secret detail")));
            api.MapGet("/validation", () => Result.Fail(new ErrorList([
                Error.Validation("Email is invalid", "email"),
                Error.Validation("Email is required", "email"),
                Error.Validation("Name is required", "name")])));
            api.MapGet("/mixed-codes", () => Result.Fail(new ErrorList([
                new Error("first", "first validation", ErrorType.Validation),
                new Error("second", "second validation", ErrorType.Validation)])));
        });
        using var client = app.GetTestClient();

        using var conflict = await GetProblem(client, "/api/conflict");
        Assert.Equal(409, conflict.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("conflict", conflict.RootElement.GetProperty("title").GetString());
        Assert.Equal("Conflict", conflict.RootElement.GetProperty("code").GetString());
        Assert.True(conflict.RootElement.TryGetProperty("traceId", out _));

        using var critical = await GetProblem(client, "/api/critical");
        Assert.Equal(500, critical.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("An unexpected error occurred.", critical.RootElement.GetProperty("title").GetString());

        using var validation = await GetProblem(client, "/api/validation");
        Assert.Equal(400, validation.RootElement.GetProperty("status").GetInt32());
        Assert.Equal(3, validation.RootElement.GetProperty("codes").GetArrayLength());
        Assert.Equal(2, validation.RootElement.GetProperty("errors").GetProperty("email").GetArrayLength());
        Assert.Equal(1, validation.RootElement.GetProperty("errors").GetProperty("name").GetArrayLength());

        using var mixedCodes = await GetProblem(client, "/api/mixed-codes");
        Assert.Equal(400, mixedCodes.RootElement.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task Applies_code_and_category_overrides_and_can_omit_problem_extensions()
    {
        await using var app = await StartApp(api =>
        {
            api.MapGet("/override", () => Result.Fail(new Error("Email.Invalid", "invalid email", ErrorType.Validation, "email")));
            api.MapGet("/category", () => Result.Fail(Error.Conflict("conflict")));
        }, options =>
        {
            options.Map(ErrorType.Conflict, 422);
            options.Map(ErrorType.Validation, 409);
            options.MapCode("Email.Invalid", 418);
            options.IncludeErrorCodes = false;
            options.IncludeTraceId = false;
        });
        using var client = app.GetTestClient();

        using var overridden = await GetProblem(client, "/api/override");
        Assert.Equal(418, overridden.RootElement.GetProperty("status").GetInt32());
        Assert.False(overridden.RootElement.TryGetProperty("code", out _));
        Assert.False(overridden.RootElement.TryGetProperty("traceId", out _));
        Assert.Equal("invalid email", overridden.RootElement.GetProperty("errors").GetProperty("email")[0].GetString());

        using var category = await GetProblem(client, "/api/category");
        Assert.Equal(422, category.RootElement.GetProperty("status").GetInt32());
    }

    [Fact]
    public void Options_validate_mappings_and_freeze_after_registration()
    {
        var options = new UnionResultOptions();
        Assert.Same(options, options.Map(ErrorType.NotFound, 410));
        Assert.Same(options, options.MapCode("Gone", 410));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.Map(ErrorType.Failure, 399));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.MapCode("Bad", 600));
        Assert.Throws<ArgumentException>(() => options.MapCode(" ", 404));

        var services = new ServiceCollection();
        services.AddUnionResults();
        using var provider = services.BuildServiceProvider();
        var registeredOptions = provider.GetRequiredService<IOptions<UnionResultOptions>>().Value;
        Assert.Throws<InvalidOperationException>(() => registeredOptions.Map(ErrorType.NotFound, 410));
        Assert.Throws<InvalidOperationException>(() => registeredOptions.MapCode("Gone", 410));
    }

    private static async Task<WebApplication> StartApp(Action<RouteGroupBuilder> mapRoutes, Action<UnionResultOptions>? configure = null)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddUnionResults(configure);
        var app = builder.Build();
        mapRoutes(app.MapGroup("/api").AddUnionResultFilter());
        await app.StartAsync(TestContext.Current.CancellationToken);
        return app;
    }

    private static async Task<JsonDocument> GetProblem(HttpClient client, string path)
    {
        using var response = await client.GetAsync(path, TestContext.Current.CancellationToken);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }
}
