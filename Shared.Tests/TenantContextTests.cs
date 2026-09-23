namespace Shared.Tests;

using System;
using Shared.Logger.TennantContext;
using Shouldly;
using Xunit;

public class TenantContextTests
{
    [Fact]
    public void TenantIdentifiers_Default_ReturnsEmptyIdentifiers()
    {
        TenantIdentifiers identifiers = TenantIdentifiers.Default();

        identifiers.EstateId.ShouldBe(Guid.Empty);
        identifiers.MerchantId.ShouldBe(Guid.Empty);
    }

    [Fact]
    public void TenantContext_StartsWithNewCorrelationIdAndDefaultTenantSettings()
    {
        TenantContext context = new();

        context.CorrelationId.ShouldNotBeNull();
        context.CorrelationId.Value.ShouldNotBe(Guid.Empty);
        context.EstateId.ShouldBe(Guid.Empty);
        context.MerchantId.ShouldBe(Guid.Empty);
        context.PerTenantLogsEnabled.ShouldBeFalse();
    }

    [Fact]
    public void CurrentTenant_CanStoreAndRetrieveTheCurrentContext()
    {
        TenantContext previousContext = TenantContext.CurrentTenant;
        TenantContext context = new();

        try
        {
            TenantContext.CurrentTenant = context;

            TenantContext.CurrentTenant.ShouldBe(context);
        }
        finally
        {
            TenantContext.CurrentTenant = previousContext;
        }
    }

    [Fact]
    public void CurrentTenant_WhenUnset_ReturnsNull()
    {
        TenantContext previousContext = TenantContext.CurrentTenant;

        try
        {
            TenantContext.CurrentTenant = null;

            TenantContext.CurrentTenant.ShouldBeNull();
        }
        finally
        {
            TenantContext.CurrentTenant = previousContext;
        }
    }

    [Fact]
    public void SetCorrelationId_WithValueObject_SetsTheCorrelationId()
    {
        TenantContext context = new();
        CorrelationId correlationId = CorrelationId.From(Guid.NewGuid());

        context.SetCorrelationId(correlationId);

        context.CorrelationId.ShouldBe(correlationId);
    }

#pragma warning disable CS0618
    [Fact]
    public void SetCorrelationId_WithGuid_SetsTheCorrelationId()
    {
        TenantContext context = new();
        Guid value = Guid.NewGuid();

        context.SetCorrelationId(value);

        context.CorrelationId.Value.ShouldBe(value);
    }
#pragma warning restore CS0618

    [Fact]
    public void Initialise_SetsTenantIdentifiersAndLoggingSetting()
    {
        TenantContext context = new();
        TenantIdentifiers identifiers = new(Guid.NewGuid(), Guid.NewGuid());

        context.Initialise(identifiers, true);

        context.EstateId.ShouldBe(identifiers.EstateId);
        context.MerchantId.ShouldBe(identifiers.MerchantId);
        context.PerTenantLogsEnabled.ShouldBeTrue();
    }
}
