using Microsoft.EntityFrameworkCore;
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

public class DbContextResolverTests {
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
}
