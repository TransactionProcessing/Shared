using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace TransactionProcessing.UnionResults.AspNetCore.Tests;

public sealed class FilterTests
{
    [Fact]
    public async Task Writes_only_the_success_payload_and_supports_empty_success()
    {
        await using var app = await StartApp(api =>
        {
            api.MapGet("/text", () => Results.Ok("done"));
            api.MapGet("/number", () => Results.Ok(42));
            api.MapGet("/error-payload", () => Results.Ok(new Error(ErrorType.NotFound, "Payload")));
            api.MapGet("/empty", () => Results.Ok());
            api.MapGet("/null", () => Results.Ok<string?>(null));
        });
        using var client = app.GetTestClient();
        var token = TestContext.Current.CancellationToken;

        Assert.Equal("\"done\"", await client.GetStringAsync("/api/text", token));
        Assert.Equal("42", await client.GetStringAsync("/api/number", token));
        using var payload = JsonDocument.Parse(await client.GetStringAsync("/api/error-payload", token));
        Assert.Equal("Payload", payload.RootElement.GetProperty("message").GetString());

        using var empty = await client.GetAsync("/api/empty", token);
        Assert.Equal(HttpStatusCode.OK, empty.StatusCode);
        Assert.Empty(await empty.Content.ReadAsByteArrayAsync(token));

        using var nullPayload = await client.GetAsync("/api/null", token);
        Assert.Equal(HttpStatusCode.OK, nullPayload.StatusCode);
        Assert.Empty(await nullPayload.Content.ReadAsByteArrayAsync(token));
    }

    [Fact]
    public async Task Passes_through_native_http_results_and_unrelated_unions()
    {
        await using var app = await StartApp(api =>
        {
            api.MapGet("/raw", () => "raw response");
            api.MapGet("/other-union", () => new OtherResult(new Error(ErrorType.NotFound, "Other union")));
            api.MapPost("/created", () => TypedResults.Created("/api/items/42", "created"));
            api.MapDelete("/deleted", () => TypedResults.NoContent());
        });
        using var client = app.GetTestClient();
        var token = TestContext.Current.CancellationToken;

        Assert.Equal("raw response", await client.GetStringAsync("/api/raw", token));
        Assert.Contains("Other union", await client.GetStringAsync("/api/other-union", token), StringComparison.Ordinal);

        using var created = await client.PostAsync("/api/created", null, token);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal("/api/items/42", created.Headers.Location?.OriginalString);

        using var deleted = await client.DeleteAsync("/api/deleted", token);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
    }

    [Theory]
    [InlineData(ErrorType.Validation, 400)]
    [InlineData(ErrorType.Unauthorized, 401)]
    [InlineData(ErrorType.Forbidden, 403)]
    [InlineData(ErrorType.NotFound, 404)]
    [InlineData(ErrorType.Conflict, 409)]
    [InlineData(ErrorType.Failure, 500)]
    [InlineData(ErrorType.Critical, 500)]
    [InlineData(ErrorType.Unknown, 500)]
    public async Task Maps_single_errors_to_problem_details(ErrorType category, int statusCode)
    {
        const string message = "Error from the operation";
        await using var app = await StartApp(api =>
        {
            api.MapGet("/failure", () => Results.Fail(new Error(category, message)));
            api.MapGet("/generic-failure", () => (Result<string>)new Error(category, message));
        });
        using var client = app.GetTestClient();

        foreach (var path in new[] { "/api/failure", "/api/generic-failure" })
        {
            using var problem = await GetProblem(client, path, statusCode);
            Assert.Equal(path, problem.RootElement.GetProperty("instance").GetString());
            Assert.True(problem.RootElement.TryGetProperty("traceId", out _));
            Assert.Equal(statusCode < 500 ? message : "An unexpected error occurred.",
                problem.RootElement.GetProperty("title").GetString());
            if (statusCode >= 500)
                Assert.DoesNotContain(message, problem.RootElement.GetRawText(), StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task Preserves_validation_messages_in_order_under_the_empty_field_name()
    {
        var errors = new ErrorList(
        [
            new Error(ErrorType.Validation, "Email is required"),
            new Error(ErrorType.Validation, "Name is required")
        ]);

        await using var app = await StartApp(api =>
        {
            api.MapGet("/validation", () => Results.Fail(errors));
            api.MapGet("/generic-validation", () => (Result<string>)errors);
        }, options =>
        {
            options.Map(ErrorType.Validation, 422);
            options.IncludeTraceId = false;
        });
        using var client = app.GetTestClient();

        foreach (var path in new[] { "/api/validation", "/api/generic-validation" })
        {
            using var problem = await GetProblem(client, path, 422);
            Assert.Equal("Email is required", problem.RootElement.GetProperty("title").GetString());
            Assert.False(problem.RootElement.TryGetProperty("traceId", out _));
            var messages = problem.RootElement.GetProperty("errors").GetProperty(string.Empty);
            Assert.Equal(["Email is required", "Name is required"], messages.EnumerateArray().Select(item => item.GetString()));
        }
    }

    [Fact]
    public async Task Redacts_error_lists_mapped_to_server_errors()
    {
        await using var app = await StartApp(api => api.MapGet("/validation", () => Results.Fail(
            new ErrorList([new Error(ErrorType.Validation, "private detail")]))),
            options => options.Map(ErrorType.Validation, 503));
        using var client = app.GetTestClient();

        using var problem = await GetProblem(client, "/api/validation", 503);
        Assert.DoesNotContain("private detail", problem.RootElement.GetRawText(), StringComparison.Ordinal);
        Assert.False(problem.RootElement.TryGetProperty("errors", out _));
    }

    [Fact]
    public async Task Uninitialized_unions_return_a_redacted_internal_server_error()
    {
        await using var app = await StartApp(api =>
        {
            api.MapGet("/default", () => default(Result));
            api.MapGet("/generic-default", () => default(Result<string>));
        });
        using var client = app.GetTestClient();

        foreach (var path in new[] { "/api/default", "/api/generic-default" })
        {
            using var problem = await GetProblem(client, path, 500);
            Assert.Equal("An unexpected error occurred.", problem.RootElement.GetProperty("title").GetString());
        }
    }

    [Fact]
    public async Task Unexpected_exceptions_are_left_to_the_exception_handler()
    {
        await using var app = await StartApp(api => api.MapGet("/exception",
            () => Task.FromException<Result<string>>(new InvalidOperationException("private exception detail"))));
        using var client = app.GetTestClient();

        using var response = await client.GetAsync("/api/exception", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.DoesNotContain("private exception detail",
            await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), StringComparison.Ordinal);
    }

    [Fact]
    public async Task OpenApi_describes_the_union_payload_and_default_and_configured_errors()
    {
        await using var app = await StartApp(api =>
        {
            api.MapGet("/generic", () => Results.Ok("value")).ProducesUnionResult<string>(422);
            api.MapGet("/non-generic", () => Results.Ok()).ProducesUnionResult();
            api.MapGet("/raw", () => "raw response");
        }, includeOpenApi: true);
        using var client = app.GetTestClient();

        using var document = JsonDocument.Parse(await client.GetStringAsync("/openapi/v1.json", TestContext.Current.CancellationToken));
        var paths = document.RootElement.GetProperty("paths");
        var genericResponses = paths.GetProperty("/api/generic").GetProperty("get").GetProperty("responses");
        var nonGenericResponses = paths.GetProperty("/api/non-generic").GetProperty("get").GetProperty("responses");
        var rawResponses = paths.GetProperty("/api/raw").GetProperty("get").GetProperty("responses");

        Assert.Equal(["200", "400", "401", "403", "404", "409", "422", "500"],
            genericResponses.EnumerateObject().Select(response => response.Name).Order(StringComparer.Ordinal));
        Assert.Equal(["200", "400", "401", "403", "404", "409", "500"],
            nonGenericResponses.EnumerateObject().Select(response => response.Name).Order(StringComparer.Ordinal));
        Assert.Equal(["200"], rawResponses.EnumerateObject().Select(response => response.Name));
        Assert.True(genericResponses.GetProperty("422").GetProperty("content").TryGetProperty("application/problem+json", out _));
        var successSchema = genericResponses.GetProperty("200").GetProperty("content").GetProperty("application/json").GetProperty("schema");
        Assert.Equal("string", successSchema.GetProperty("type").GetString());
        Assert.False(nonGenericResponses.GetProperty("200").TryGetProperty("content", out _));
    }

    [Fact]
    public void Options_validate_mappings_and_freeze_after_registration()
    {
        var options = new UnionResultOptions();
        Assert.Same(options, options.Map(ErrorType.NotFound, 410));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.Map(ErrorType.Failure, 399));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.Map(ErrorType.Failure, 600));

        var services = new ServiceCollection();
        services.AddUnionResults();
        using var provider = services.BuildServiceProvider();
        var registeredOptions = provider.GetRequiredService<IOptions<UnionResultOptions>>().Value;
        Assert.Throws<InvalidOperationException>(() => registeredOptions.Map(ErrorType.NotFound, 410));
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

    [Fact]
    public async Task OpenApi_rejects_invalid_additional_problem_status_codes()
    {
        var builder = WebApplication.CreateBuilder();
        await using var app = builder.Build();

        Assert.Throws<ArgumentOutOfRangeException>(() => app.MapGet("/invalid", () => Results.Ok()).ProducesUnionResult(399));
    }

    private static async Task<WebApplication> StartApp(Action<RouteGroupBuilder> mapRoutes,
        Action<UnionResultOptions>? configure = null, bool includeOpenApi = false)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddUnionResults(configure);
        if (includeOpenApi)
            builder.Services.AddOpenApi();

        var app = builder.Build();
        app.UseExceptionHandler(errorApp => errorApp.Run(context =>
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            return Task.CompletedTask;
        }));
        mapRoutes(app.MapGroup("/api").AddUnionResultFilter());
        if (includeOpenApi)
            app.MapOpenApi();

        await app.StartAsync(TestContext.Current.CancellationToken);
        return app;
    }

    private static async Task<JsonDocument> GetProblem(HttpClient client, string path, int expectedStatus)
    {
        using var response = await client.GetAsync(path, TestContext.Current.CancellationToken);
        Assert.Equal(expectedStatus, (int)response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.Equal(expectedStatus, document.RootElement.GetProperty("status").GetInt32());
        return document;
    }

    private union OtherResult(string, Error);
}
