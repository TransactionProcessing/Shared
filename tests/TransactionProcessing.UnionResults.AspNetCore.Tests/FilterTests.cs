using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
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
}
