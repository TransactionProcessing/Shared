using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Shared.EntityFramework;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Shared.Tests {
    public class TestDbContext : DbContext {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) {
            
        }
    }

    public class DbContextResolverTests {
        [Fact]
        public void Resolve_WithValidConnectionString_ResolvesDbContext() {
            // Arrange
            var services = new ServiceCollection();
            services.AddDbContext<TestDbContext>(options => options.UseInMemoryDatabase("TestDb"));
            var provider = services.BuildServiceProvider();

            var configSectionMock = new Mock<IConfigurationSection>();
            configSectionMock.Setup(x => x["Default"]).Returns("Server=.;Database=Default;Trusted_Connection=True;");

            var configMock = new Mock<IConfiguration>();
            configMock.Setup(x => x.GetSection("ConnectionStrings")).Returns(configSectionMock.Object);

            var resolver = new DbContextResolver<TestDbContext>(provider, configMock.Object);

            // Act
            var result = resolver.Resolve("Default", null);

            // Assert
            result.ShouldNotBeNull();
            result.Context.ShouldNotBeNull();
            result.Dispose();
        }

        [Fact]
        public void Resolve_WithMissingConnectionString_Throws() {
            // Arrange
            var services = new ServiceCollection();
            var provider = services.BuildServiceProvider();

            var configSectionMock = new Mock<IConfigurationSection>();
            configSectionMock.Setup(x => x["Missing"]).Returns(String.Empty);

            var configMock = new Mock<IConfiguration>();
            configMock.Setup(x => x.GetSection("ConnectionStrings")).Returns(configSectionMock.Object);

            var resolver = new DbContextResolver<TestDbContext>(provider, configMock.Object);

            // Act & Assert
            Should.Throw<InvalidOperationException>(() => resolver.Resolve("Missing", null));
        }

        [Fact]
        public void Resolve_WithConnectionIdentifier_UpdatesInitialCatalog() {
            // Arrange
            var baseConnectionString = "Server=.;Database=BaseDb;Trusted_Connection=True;";
            var services = new ServiceCollection();
            var provider = services.BuildServiceProvider();

            var configSectionMock = new Mock<IConfigurationSection>();
            configSectionMock.Setup(x => x["Default"]).Returns("Server=.;Database=DefaultDb;Trusted_Connection=True;");

            var configMock = new Mock<IConfiguration>();
            configMock.Setup(x => x.GetSection("ConnectionStrings")).Returns(configSectionMock.Object);

            var resolver = new DbContextResolver<TestDbContext>(provider, configMock.Object);

            // Act
            var result = resolver.Resolve("Default", "Tenant1");

            // Assert
            result.ShouldNotBeNull();
            result.Context.ShouldNotBeNull();
            result.Context.Database.GetDbConnection().Database.ShouldBe("DefaultDb-Tenant1");
            result.Dispose();
        }
    }
}