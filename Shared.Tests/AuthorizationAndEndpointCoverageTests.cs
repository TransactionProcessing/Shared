namespace Shared.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Shared.Authorisation;
using Shared.Extensions;
using Shouldly;
using Xunit;

public class AuthorizationAndEndpointCoverageTests
{
    [Fact]
    public async Task AddAuthorisedTokenPolicy_RequiresAnAuthenticatedUser()
    {
        ServiceCollection services = new();
        services.AddAuthorisedTokenPolicy();
        services.AddLogging();
        await using ServiceProvider provider = services.BuildServiceProvider();
        IAuthorizationService authorization = provider.GetRequiredService<IAuthorizationService>();

        (await authorization.AuthorizeAsync(
            new ClaimsPrincipal(new ClaimsIdentity()),
            AuthorizationExtensions.PolicyNames.AuthorisedTokenPolicy)).Succeeded.ShouldBeFalse();
        (await authorization.AuthorizeAsync(
            new ClaimsPrincipal(new ClaimsIdentity([], "test")),
            AuthorizationExtensions.PolicyNames.AuthorisedTokenPolicy)).Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void AddAuthorizationHandlers_RegistersBothHandlers()
    {
        ServiceCollection services = new();

        services.AddClientCredentialsHandler();
        services.AddPasswordTokenHandler();

        services.BuildServiceProvider()
            .GetServices<IAuthorizationHandler>()
            .Count()
            .ShouldBe(2);
    }

    [Fact]
    public async Task ClientCredentialsPolicy_SucceedsWithoutUserIdentifier()
    {
        ServiceCollection services = new();
        services.AddClientCredentialsOnlyPolicy();
        services.AddClientCredentialsHandler();
        services.AddLogging();
        await using ServiceProvider provider = services.BuildServiceProvider();
        IAuthorizationService authorization = provider.GetRequiredService<IAuthorizationService>();

        ClaimsPrincipal clientPrincipal = new(new ClaimsIdentity([], "client"));
        ClaimsPrincipal userPrincipal = new(new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, "user") }, "user"));

        (await authorization.AuthorizeAsync(
            clientPrincipal,
            AuthorizationExtensions.PolicyNames.ClientCredentialsOnlyPolicy)).Succeeded.ShouldBeTrue();
        (await authorization.AuthorizeAsync(
            userPrincipal,
            AuthorizationExtensions.PolicyNames.ClientCredentialsOnlyPolicy)).Succeeded.ShouldBeFalse();
    }

    [Fact]
    public async Task PasswordTokenPolicy_SucceedsWithUserIdentifier()
    {
        ServiceCollection services = new();
        services.AddPasswordTokenPolicy();
        services.AddPasswordTokenHandler();
        services.AddLogging();
        await using ServiceProvider provider = services.BuildServiceProvider();
        IAuthorizationService authorization = provider.GetRequiredService<IAuthorizationService>();

        ClaimsPrincipal userPrincipal = new(new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, "user") }, "user"));
        ClaimsPrincipal clientPrincipal = new(new ClaimsIdentity([], "client"));

        (await authorization.AuthorizeAsync(
            userPrincipal,
            AuthorizationExtensions.PolicyNames.PasswordTokenOnlyPolicy)).Succeeded.ShouldBeTrue();
        (await authorization.AuthorizeAsync(
            clientPrincipal,
            AuthorizationExtensions.PolicyNames.PasswordTokenOnlyPolicy)).Succeeded.ShouldBeFalse();
    }

    [Fact]
    public void WithStandardProduces_WithSuccessAndErrorTypes_ReturnsSameBuilder()
    {
        WebApplication app = WebApplication.CreateBuilder().Build();
        RouteHandlerBuilder route = app.MapGet("/typed", () => Results.Ok());

        RouteHandlerBuilder result = route.WithStandardProduces<SuccessResponse, ErrorResponse>(
            StatusCodes.Status201Created);

        result.ShouldBeSameAs(route);
    }

    [Fact]
    public void WithStandardProduces_WithErrorType_ReturnsSameBuilder()
    {
        WebApplication app = WebApplication.CreateBuilder().Build();
        RouteHandlerBuilder route = app.MapGet("/error", () => Results.Ok());

        RouteHandlerBuilder result = route.WithStandardProduces<ErrorResponse>(
            StatusCodes.Status202Accepted);

        result.ShouldBeSameAs(route);
    }

    private sealed class SuccessResponse;

    private sealed class ErrorResponse;
}
