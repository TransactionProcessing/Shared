namespace Shared.Tests;

using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Shared.General;
using Shared.Logger.TennantContext;
using Shared.Middleware;
using Shouldly;
using Xunit;

[Collection("Sequential")]
public class TenantMiddlewareTests
{
    [Fact]
    public void IsAuthenticated_ReturnsFalseForUnauthenticatedPrincipal()
    {
        new ClaimsPrincipal(new ClaimsIdentity()).IsAuthenticated().ShouldBeFalse();
    }

    [Fact]
    public void IsAuthenticated_ReturnsTrueForAuthenticatedPrincipal()
    {
        ClaimsIdentity identity = new([], "test");

        new ClaimsPrincipal(identity).IsAuthenticated().ShouldBeTrue();
    }

    [Fact]
    public void GetIdentifiersFromHeaders_ReturnsHeaderIdentifiers()
    {
        Guid estateId = Guid.NewGuid();
        Guid merchantId = Guid.NewGuid();
        DefaultHttpContext context = new();
        context.Request.Headers["estateId"] = estateId.ToString();
        context.Request.Headers["merchantId"] = merchantId.ToString();

        TenantIdentifiers result = context.GetIdentifiersFromHeaders();

        result.ShouldBe(new TenantIdentifiers(estateId, merchantId));
    }

    [Fact]
    public void GetIdentifiersFromHeaders_ReturnsDefaultForInvalidEstateId()
    {
        DefaultHttpContext context = new();
        context.Request.Headers["estateId"] = "invalid";

        context.GetIdentifiersFromHeaders().ShouldBe(TenantIdentifiers.Default());
    }

    [Fact]
    public void GetIdentifiersFromRoute_ReturnsRouteIdentifiers()
    {
        Guid estateId = Guid.NewGuid();
        Guid merchantId = Guid.NewGuid();
        DefaultHttpContext context = new();
        context.Request.RouteValues["estateId"] = estateId;
        context.Request.RouteValues["merchantId"] = merchantId;

        context.GetIdentifiersFromRoute().ShouldBe(new TenantIdentifiers(estateId, merchantId));
    }

    [Fact]
    public void GetIdentifiersFromToken_ReturnsTokenIdentifiers()
    {
        Guid estateId = Guid.NewGuid();
        Guid merchantId = Guid.NewGuid();
        ClaimsIdentity identity = new(
            new[]
            {
                new Claim("estateId", estateId.ToString()),
                new Claim("merchantId", merchantId.ToString())
            },
            "test");
        DefaultHttpContext context = new() { User = new ClaimsPrincipal(identity) };

        context.GetIdentifiersFromToken().ShouldBe(new TenantIdentifiers(estateId, merchantId));
    }

    [Fact]
    public void GetIdentifiersFromToken_ReturnsDefaultForUnauthenticatedUser()
    {
        DefaultHttpContext context = new();

        context.GetIdentifiersFromToken().ShouldBe(TenantIdentifiers.Default());
    }

    [Fact]
    public async Task TenantMiddleware_StoresValidCorrelationIdAndCallsNext()
    {
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();
        ConfigurationReader.Initialise(configuration);
        TestHelpers.InitialiseLogger();

        Guid correlationId = Guid.NewGuid();
        DefaultHttpContext context = TestHelpers.CreateHttpContext();
        context.Request.Headers[TenantMiddleware.KeyNameCorrelationId] = correlationId.ToString();
        TenantContext tenantContext = new();
        Boolean nextCalled = false;
        TenantMiddleware middleware = new(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context, tenantContext);

        tenantContext.CorrelationId.Value.ShouldBe(correlationId);
        context.Items[TenantMiddleware.KeyNameCorrelationId]
            .ShouldBe(CorrelationId.From(correlationId).ToString());
        nextCalled.ShouldBeTrue();
    }

    [Fact]
    public async Task TenantMiddleware_DoesNotStoreInvalidCorrelationId()
    {
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();
        ConfigurationReader.Initialise(configuration);
        TestHelpers.InitialiseLogger();

        DefaultHttpContext context = TestHelpers.CreateHttpContext();
        context.Request.Headers[TenantMiddleware.KeyNameCorrelationId] = "invalid";
        TenantContext tenantContext = new();
        CorrelationId originalCorrelationId = tenantContext.CorrelationId;
        TenantMiddleware middleware = new(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context, tenantContext);

        tenantContext.CorrelationId.ShouldBe(originalCorrelationId);
        context.Items.ContainsKey(TenantMiddleware.KeyNameCorrelationId).ShouldBeFalse();
    }
}
