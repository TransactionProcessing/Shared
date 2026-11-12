using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
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
    private const string Conflict = "conflict";
    private const string EmailField = "email";
    private const string FirstCode = "first";
    private const string InvalidLocation = "https://[";
    private const string SecondCode = "second";
    private const string StatusProperty = "status";

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
            api.MapGet("/created-no-location", () => Result<string>.Created("created"));
            api.MapGet("/empty-outcome", () => new CustomOutcome(new ResultOutcome(ResultKind.Ok)));
            api.MapGet("/raw", () => "raw response");
        });
        using var client = app.GetTestClient();

        var value = await client.GetAsync("/api/value", TestContext.Current.CancellationToken);
        Assert.Equal(System.Net.HttpStatusCode.OK, value.StatusCode);
        Assert.Equal("\"done\"", await value.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));

        var empty = await client.GetAsync("/api/ok-empty", TestContext.Current.CancellationToken);
        Assert.Equal(System.Net.HttpStatusCode.OK, empty.StatusCode);
        Assert.Empty(await empty.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken));

        var outcomeWithoutValue = await client.GetAsync("/api/empty-outcome", TestContext.Current.CancellationToken);
        Assert.Equal(System.Net.HttpStatusCode.OK, outcomeWithoutValue.StatusCode);
        Assert.Empty(await outcomeWithoutValue.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken));

        var noContent = await client.GetAsync("/api/no-content", TestContext.Current.CancellationToken);
        Assert.Equal(System.Net.HttpStatusCode.NoContent, noContent.StatusCode);

        var created = await client.GetAsync("/api/created", TestContext.Current.CancellationToken);
        Assert.Equal(System.Net.HttpStatusCode.Created, created.StatusCode);
        Assert.Equal("/items/42", created.Headers.Location?.OriginalString);
        Assert.Equal("\"created\"", await created.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));

        var createdWithoutLocation = await client.GetAsync("/api/created-no-location", TestContext.Current.CancellationToken);
        Assert.Equal(System.Net.HttpStatusCode.Created, createdWithoutLocation.StatusCode);
        Assert.Null(createdWithoutLocation.Headers.Location);

        var raw = await client.GetAsync("/api/raw", TestContext.Current.CancellationToken);
        Assert.Equal("raw response", await raw.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task OpenApi_describes_union_results_only_on_endpoints_opted_in()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddUnionResults();
        builder.Services.AddOpenApi();
        await using var app = builder.Build();
        var api = app.MapGroup("/api").AddUnionResultFilter();
        api.MapGet("/generic", () => Result<string>.Ok("value"))
            .ProducesUnionResult<string>(422);
        api.MapGet("/non-generic", () => Result.Ok())
            .ProducesUnionResult();
        api.MapGet("/raw", () => "raw response");
        app.MapOpenApi();
        await app.StartAsync(TestContext.Current.CancellationToken);
        using var client = app.GetTestClient();

        using var document = JsonDocument.Parse(await client.GetStringAsync("/openapi/v1.json", TestContext.Current.CancellationToken));
        var paths = document.RootElement.GetProperty("paths");
        var genericResponses = paths.GetProperty("/api/generic").GetProperty("get").GetProperty("responses");
        var nonGenericResponses = paths.GetProperty("/api/non-generic").GetProperty("get").GetProperty("responses");
        var rawResponses = paths.GetProperty("/api/raw").GetProperty("get").GetProperty("responses");

        Assert.Equal(["200", "201", "204", "400", "401", "403", "404", "409", "422", "500"], genericResponses.EnumerateObject().Select(static response => response.Name).Order(StringComparer.Ordinal));
        Assert.Equal(["200", "204", "400", "401", "403", "404", "409", "500"], nonGenericResponses.EnumerateObject().Select(static response => response.Name).Order(StringComparer.Ordinal));
        Assert.Equal(["200"], rawResponses.EnumerateObject().Select(static response => response.Name));
        Assert.True(genericResponses.GetProperty("422").GetProperty("content").TryGetProperty("application/problem+json", out _));
        Assert.True(genericResponses.GetProperty("200").GetProperty("content").TryGetProperty("application/json", out _));
        Assert.True(genericResponses.GetProperty("201").GetProperty("content").TryGetProperty("application/json", out _));
        var successSchema = genericResponses.GetProperty("200").GetProperty("content").GetProperty("application/json").GetProperty("schema");
        Assert.Equal("string", successSchema.GetProperty("type").GetString());
        Assert.False(genericResponses.GetProperty("204").TryGetProperty("content", out _));
        Assert.False(nonGenericResponses.GetProperty("200").TryGetProperty("content", out _));
        Assert.False(nonGenericResponses.GetProperty("204").TryGetProperty("content", out _));
    }

    [Fact]
    public void OpenApi_rejects_invalid_additional_problem_status_codes()
    {
        var builder = WebApplication.CreateBuilder();
        var app = builder.Build();

        Assert.Throws<ArgumentOutOfRangeException>(() => app.MapGet("/invalid", () => Result.Ok()).ProducesUnionResult(399));
    }

    [Fact]
    public async Task Unexpected_exceptions_are_left_to_exception_handler()
    {
        await using var app = await StartApp(api => api.MapGet("/exception", () => Task.FromException<string>(new InvalidOperationException("private exception detail"))));
        using var client = app.GetTestClient();

        using var response = await client.GetAsync("/api/exception", TestContext.Current.CancellationToken);

        Assert.Equal(System.Net.HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.DoesNotContain("private exception detail", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Rejects_invalid_created_locations_and_unknown_union_kinds()
    {
        await using var app = await StartApp(api =>
        {
            api.MapGet("/invalid-location", () => Result<string>.Created("value", InvalidLocation));
            api.MapGet("/null-created", () => new CustomOutcome(new ResultOutcome(ResultKind.Created)));
            api.MapGet("/unknown-kind", () => new CustomOutcome(new ResultOutcome((ResultKind)int.MaxValue)));
        });
        using var client = app.GetTestClient();

        using var invalidLocation = await client.GetAsync("/api/invalid-location", TestContext.Current.CancellationToken);
        Assert.Equal(System.Net.HttpStatusCode.InternalServerError, invalidLocation.StatusCode);

        using var nullCreated = await client.GetAsync("/api/null-created", TestContext.Current.CancellationToken);
        Assert.Equal(System.Net.HttpStatusCode.InternalServerError, nullCreated.StatusCode);

        using var unknownKind = await client.GetAsync("/api/unknown-kind", TestContext.Current.CancellationToken);
        Assert.Equal(System.Net.HttpStatusCode.InternalServerError, unknownKind.StatusCode);
    }

    [Fact]
    public async Task Maps_categories_and_error_lists_to_problem_details()
    {
        await using var app = await StartApp(api =>
        {
            api.MapGet("/conflict", () => Result.Fail(Error.Conflict(Conflict)));
            api.MapGet("/critical", () => Result.Fail(Error.Critical("secret detail")));
            api.MapGet("/unauthorized", () => Result.Fail(Error.Unauthorized("unauthorized")));
            api.MapGet("/forbidden", () => Result.Fail(Error.Forbidden("forbidden")));
            api.MapGet("/not-found", () => Result.Fail(Error.NotFound("missing")));
            api.MapGet("/failure", () => Result.Fail(Error.Failure("failed")));
            api.MapGet("/unknown", () => Result.Fail(new Error("Unknown", "unknown", ErrorType.Unknown)));
            api.MapGet("/validation", () => Result.Fail(new ErrorList([
                Error.Validation("Email is invalid", EmailField),
                Error.Validation("Email is required", EmailField),
                Error.Validation("Name is required", "name")])));
            api.MapGet("/mixed-codes", () => Result.Fail(new ErrorList([
                new Error(FirstCode, "first validation", ErrorType.Validation),
                new Error(SecondCode, "second validation", ErrorType.Validation)])));
        });
        using var client = app.GetTestClient();

        using var conflict = await GetProblem(client, "/api/conflict");
        Assert.Equal(409, conflict.RootElement.GetProperty(StatusProperty).GetInt32());
        Assert.Equal(Conflict, conflict.RootElement.GetProperty("title").GetString());
        Assert.Equal("/api/conflict", conflict.RootElement.GetProperty("instance").GetString());
        Assert.Equal("Conflict", conflict.RootElement.GetProperty("code").GetString());
        Assert.True(conflict.RootElement.TryGetProperty("traceId", out _));

        using var critical = await GetProblem(client, "/api/critical");
        Assert.Equal(500, critical.RootElement.GetProperty(StatusProperty).GetInt32());
        Assert.Equal("An unexpected error occurred.", critical.RootElement.GetProperty("title").GetString());
        Assert.Equal("/api/critical", critical.RootElement.GetProperty("instance").GetString());
        Assert.DoesNotContain("secret detail", critical.RootElement.ToString(), StringComparison.Ordinal);

        using var unauthorized = await GetProblem(client, "/api/unauthorized");
        Assert.Equal(401, unauthorized.RootElement.GetProperty(StatusProperty).GetInt32());
        using var forbidden = await GetProblem(client, "/api/forbidden");
        Assert.Equal(403, forbidden.RootElement.GetProperty(StatusProperty).GetInt32());
        using var notFound = await GetProblem(client, "/api/not-found");
        Assert.Equal(404, notFound.RootElement.GetProperty(StatusProperty).GetInt32());
        using var failure = await GetProblem(client, "/api/failure");
        Assert.Equal(500, failure.RootElement.GetProperty(StatusProperty).GetInt32());
        using var unknown = await GetProblem(client, "/api/unknown");
        Assert.Equal(500, unknown.RootElement.GetProperty(StatusProperty).GetInt32());

        using var validation = await GetProblem(client, "/api/validation");
        Assert.Equal(400, validation.RootElement.GetProperty(StatusProperty).GetInt32());
        Assert.Equal(3, validation.RootElement.GetProperty("codes").GetArrayLength());
        Assert.Equal(2, validation.RootElement.GetProperty("errors").GetProperty(EmailField).GetArrayLength());
        Assert.Equal(1, validation.RootElement.GetProperty("errors").GetProperty("name").GetArrayLength());

        using var mixedCodes = await GetProblem(client, "/api/mixed-codes");
        Assert.Equal(400, mixedCodes.RootElement.GetProperty(StatusProperty).GetInt32());
    }

    [Fact]
    public async Task Mixed_error_code_statuses_fall_back_to_the_error_category()
    {
        var cases = new (string Path, ErrorType Type, int ExpectedStatus)[]
        {
            ("validation", ErrorType.Validation, 400),
            ("unauthorized", ErrorType.Unauthorized, 401),
            ("forbidden", ErrorType.Forbidden, 403),
            ("not-found", ErrorType.NotFound, 404),
            (Conflict, ErrorType.Conflict, 409),
            ("failure", ErrorType.Failure, 503),
            ("critical", ErrorType.Critical, 500),
            ("unknown", ErrorType.Unknown, 500),
        };
        await using var app = await StartApp(api =>
        {
            foreach (var (path, type, _) in cases)
            {
                api.MapGet($"/{path}", () => Result.Fail(new ErrorList([
                    new Error(FirstCode, FirstCode, type), new Error(SecondCode, SecondCode, type)])));
            }
        }, options =>
        {
            options.MapCode(FirstCode, 418);
            options.MapCode(SecondCode, 422);
            options.Map(ErrorType.Failure, 503);
        });
        using var client = app.GetTestClient();

        foreach (var (path, _, expectedStatus) in cases)
        {
            using var problem = await GetProblem(client, $"/api/{path}");
            Assert.Equal(expectedStatus, problem.RootElement.GetProperty(StatusProperty).GetInt32());
        }
    }

    [Fact]
    public async Task Applies_code_and_category_overrides_and_can_omit_problem_extensions()
    {
        await using var app = await StartApp(api =>
        {
            api.MapGet("/override", () => Result.Fail(new Error("Email.Invalid", "invalid email", ErrorType.Validation, EmailField)));
            api.MapGet("/category", () => Result.Fail(Error.Conflict(Conflict)));
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
        Assert.Equal(418, overridden.RootElement.GetProperty(StatusProperty).GetInt32());
        Assert.False(overridden.RootElement.TryGetProperty("code", out _));
        Assert.False(overridden.RootElement.TryGetProperty("traceId", out _));
        Assert.Equal("invalid email", overridden.RootElement.GetProperty("errors").GetProperty(EmailField)[0].GetString());

        using var category = await GetProblem(client, "/api/category");
        Assert.Equal(422, category.RootElement.GetProperty(StatusProperty).GetInt32());
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
        Assert.Throws<InvalidOperationException>(() => registeredOptions.IncludeErrorCodes = false);
        Assert.Throws<InvalidOperationException>(() => registeredOptions.IncludeTraceId = false);
    }

    [Fact]
    public async Task Invalid_mapping_fails_when_the_application_starts()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddUnionResults(options => options.Map(ErrorType.Failure, 399));
        await using var app = builder.Build();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => app.StartAsync(TestContext.Current.CancellationToken));
    }

    private static async Task<WebApplication> StartApp(Action<RouteGroupBuilder> mapRoutes, Action<UnionResultOptions>? configure = null)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddUnionResults(configure);
        var app = builder.Build();
        app.UseExceptionHandler(errorApp => errorApp.Run(context =>
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            return Task.CompletedTask;
        }));
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

    private sealed class CustomOutcome(ResultOutcome outcome) : IResultOutcome
    {
        public ResultOutcome GetOutcome() => outcome;
    }
}
