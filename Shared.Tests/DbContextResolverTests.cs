using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.SqlServer;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Imposter.Abstractions;
using Shared.EntityFramework;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Shared.Tests;

public class TestDbContext : DbContext {
    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) {
            
    }
}

public sealed class DisposableTestDbContext : DbContext
{
    public DisposableTestDbContext(DbContextOptions<DisposableTestDbContext> options) : base(options) { }

    public bool WasDisposed { get; private set; }

    public override void Dispose()
    {
        this.WasDisposed = true;
        base.Dispose();
    }
}

public sealed class NoOpCommandInterceptor : DbCommandInterceptor { }
public class DbContextResolverTests {
    [Fact]
    public void ResolvedDbContext_RejectsNullScope()
    {
        Should.Throw<ArgumentNullException>(() => new ResolvedDbContext<TestDbContext>(null));
    }

    [Fact]
    public void Resolve_WithValidConnectionString_ResolvesDbContext() {
        // Arrange
        ServiceCollection services = new();
        services.AddDbContext<TestDbContext>(options => options.UseInMemoryDatabase("TestDb"));
        ServiceProvider provider = services.BuildServiceProvider();

        IConfigurationSectionImposter configSectionMock = new();
        configSectionMock[Arg<String>.Is("Default")].Getter().Returns("Server=.;Database=Default;Trusted_Connection=True;");

        IConfigurationImposter configMock = new();
        configMock.GetSection("ConnectionStrings").Returns(configSectionMock.Instance());

        DbContextResolver<TestDbContext> resolver = new(provider, configMock.Instance());

        // Act
        ResolvedDbContext<TestDbContext> result = resolver.Resolve("Default", null);

        // Assert
        result.ShouldNotBeNull();
        result.Context.ShouldNotBeNull();
        result.Dispose();
    }

    [Fact]
    public void Resolve_WithoutSuffix_ResolvesDbContext() {
        ServiceCollection services = new();
        services.AddDbContext<TestDbContext>(options => options.UseInMemoryDatabase("TestDbWithoutSuffix"));
        ServiceProvider provider = services.BuildServiceProvider();

        IConfigurationSectionImposter configSectionMock = new();
        configSectionMock[Arg<String>.Is("Default")].Getter().Returns("Server=.;Database=Default;Trusted_Connection=True;");

        IConfigurationImposter configMock = new();
        configMock.GetSection("ConnectionStrings").Returns(configSectionMock.Instance());

        DbContextResolver<TestDbContext> resolver = new(provider, configMock.Instance());

        ResolvedDbContext<TestDbContext> result = resolver.Resolve("Default");

        result.Context.ShouldNotBeNull();
        result.Dispose();
    }

    [Fact]
    public void Resolve_WithMissingConnectionString_Throws() {
        // Arrange
        ServiceCollection services = new();
        ServiceProvider provider = services.BuildServiceProvider();

        IConfigurationSectionImposter configSectionMock = new();
        configSectionMock[Arg<String>.Is("Missing")].Getter().Returns(String.Empty);

        IConfigurationImposter configMock = new();
        configMock.GetSection("ConnectionStrings").Returns(configSectionMock.Instance());

        DbContextResolver<TestDbContext> resolver = new(provider, configMock.Instance());

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => resolver.Resolve("Missing", null));
    }

    [Fact]
    public void Resolve_WithConnectionIdentifier_UpdatesInitialCatalog() {
        // Arrange
        ServiceCollection services = new();
        ServiceProvider provider = services.BuildServiceProvider();

        IConfigurationSectionImposter configSectionMock = new();
        configSectionMock[Arg<String>.Is("Default")].Getter().Returns("Server=.;Database=DefaultDb;Trusted_Connection=True;");

        IConfigurationImposter configMock = new();
        configMock.GetSection("ConnectionStrings").Returns(configSectionMock.Instance());

        DbContextResolver<TestDbContext> resolver = new(provider, configMock.Instance());

        // Act
        ResolvedDbContext<TestDbContext> result = resolver.Resolve("Default", "Tenant1");

        // Assert
        result.ShouldNotBeNull();
        result.Context.ShouldNotBeNull();
        result.Context.Database.GetDbConnection().Database.ShouldBe("DefaultDb-Tenant1");
        result.Dispose();
    }

    [Fact]
    public void Resolve_WithSuffix_UsesConfiguredRetryOptionsAndInterceptors()
    {
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:ReadModel"] = "Server=localhost;Initial Catalog=ReadModel;Integrated Security=True;TrustServerCertificate=True"
            })
            .Build();
        NoOpCommandInterceptor interceptor = new();
        DbContextResolverOptions resolverOptions = new()
        {
            RetryOptionsFactory = key =>
            {
                key.ShouldBe("ReadModel");
                return new SqlServerRetryOptions
                {
                    MaxRetryCount = 2,
                    MaxRetryDelay = TimeSpan.FromSeconds(5),
                    AdditionalTransientErrorNumbers = [4060]
                };
            },
            Interceptors = [interceptor]
        };
        using ServiceProvider rootProvider = new ServiceCollection().BuildServiceProvider();
        DbContextResolver<TestDbContext> resolver = new(rootProvider, configuration, resolverOptions);

        using ResolvedDbContext<TestDbContext> resolved = resolver.Resolve("ReadModel", "estate-123");

        resolved.Context.Database.GetConnectionString().ShouldContain("Initial Catalog=ReadModel-estate-123");
        SqlServerRetryingExecutionStrategy strategy = (SqlServerRetryingExecutionStrategy)resolved.Context.Database.CreateExecutionStrategy();
        strategy.MaxRetryCount.ShouldBe(2);
        strategy.MaxRetryDelay.ShouldBe(TimeSpan.FromSeconds(5));
        strategy.AdditionalErrorNumbers.ShouldBe(new[] { 4060 });
        resolved.Context.GetService<IInterceptors>().Aggregate<IDbCommandInterceptor>().ShouldBe(interceptor);
    }

    [Fact]
    public void Resolve_WithSuffix_UsesDefaultRetryPolicyWhenNoRetryOptionsAreConfigured()
    {
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:ReadModel"] = "Server=localhost;Initial Catalog=ReadModel;Integrated Security=True;TrustServerCertificate=True"
            })
            .Build();
        using ServiceProvider rootProvider = new ServiceCollection().BuildServiceProvider();
        DbContextResolver<TestDbContext> resolver = new(rootProvider, configuration);

        using ResolvedDbContext<TestDbContext> resolved = resolver.Resolve("ReadModel", "estate-123");

        SqlServerRetryingExecutionStrategy strategy = (SqlServerRetryingExecutionStrategy)
            resolved.Context.Database.CreateExecutionStrategy();
        strategy.MaxRetryCount.ShouldBe(6);
        strategy.MaxRetryDelay.ShouldBe(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void Resolve_WithSuffix_DisposesTheDynamicallyCreatedContext()
    {
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:ReadModel"] = "Server=localhost;Initial Catalog=ReadModel;Integrated Security=True;TrustServerCertificate=True"
            })
            .Build();
        using ServiceProvider rootProvider = new ServiceCollection().BuildServiceProvider();
        DbContextResolver<DisposableTestDbContext> resolver = new(rootProvider, configuration);

        ResolvedDbContext<DisposableTestDbContext> resolved = resolver.Resolve("ReadModel", "estate-123");
        DisposableTestDbContext context = resolved.Context;
        resolved.Dispose();

        context.WasDisposed.ShouldBeTrue();
    }

    [Fact]
    public void Resolve_WithMalformedConnectionString_ThrowsBeforeResolvingContext()
    {
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:ReadModel"] = "NotAValidConnectionString"
            })
            .Build();
        using ServiceProvider rootProvider = new ServiceCollection().BuildServiceProvider();
        DbContextResolver<TestDbContext> resolver = new(rootProvider, configuration);

        Should.Throw<ArgumentException>(() => resolver.Resolve("ReadModel", "estate-123"));
    }

    [Fact]
    public void Resolve_WithMalformedUnsuffixedConnectionString_ThrowsBeforeResolvingContext()
    {
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:ReadModel"] = "NotAValidConnectionString"
            })
            .Build();
        using ServiceProvider rootProvider = new ServiceCollection().BuildServiceProvider();
        DbContextResolver<TestDbContext> resolver = new(rootProvider, configuration);

        Should.Throw<ArgumentException>(() => resolver.Resolve("ReadModel"));
    }
}
