using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Tests
{
    using System.Threading;
    using EntityFramework;
    using Microsoft.EntityFrameworkCore;
    using Moq;
    using Repositories;
    using Shouldly;
    using Xunit;

    public partial class SharedTests
    {
        [Fact]
        public async Task DbContextFactory_GetContext_ContextIsReturned()
        {

            Guid identifer = Guid.Parse("D55E7543-7C14-400B-B124-2566E1B3FB13");
            String connectionStringIdentifier = "TestDatabase";

            Func<String, DbContext> createContext = (connString) => {
                var dbContextOptionsBuilder = new DbContextOptionsBuilder();
                dbContextOptionsBuilder.UseSqlServer();
                return new DbContext(dbContextOptionsBuilder.Options);
            };
            Mock<IConnectionStringConfigurationRepository> connectionStringConfigurationRepository = new Mock<IConnectionStringConfigurationRepository>();
            DbContextFactory<DbContext> factory = new DbContextFactory<DbContext>(connectionStringConfigurationRepository.Object,
                                                                                  createContext);

            var ctx = await factory.GetContext(identifer, connectionStringIdentifier, CancellationToken.None);
            ctx.ShouldNotBeNull();
        }

        [Fact]
        public async Task DbContextFactory_GetContext_EmptyIdentifer_ErrorThrown()
        {

            Guid identifer = Guid.Empty;
            String connectionStringIdentifier = "TestDatabase";
            Func<String, DbContext> createContext = connString => new DbContext(new DbContextOptionsBuilder().UseSqlServer().Options);
            Mock<IConnectionStringConfigurationRepository> connectionStringConfigurationRepository = new Mock<IConnectionStringConfigurationRepository>();
            DbContextFactory<DbContext> factory = new DbContextFactory<DbContext>(connectionStringConfigurationRepository.Object,
                                                                                  createContext);

            Should.Throw<ArgumentNullException>(async () => {
                await factory.GetContext(identifer, connectionStringIdentifier, CancellationToken.None);
            });
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task DbContextFactory_GetContext_InvalidConnectionStringIdentifier_ErrorThrown(String connectionStringIdentifier)
        {

            Guid identifer = Guid.Parse("D55E7543-7C14-400B-B124-2566E1B3FB13");
            Func<String, DbContext> createContext = (connString) => {
                var dbContextOptionsBuilder = new DbContextOptionsBuilder();
                dbContextOptionsBuilder.UseSqlServer();
                return new DbContext(dbContextOptionsBuilder.Options);
            };
            Mock<IConnectionStringConfigurationRepository> connectionStringConfigurationRepository = new Mock<IConnectionStringConfigurationRepository>();
            DbContextFactory<DbContext> factory = new DbContextFactory<DbContext>(connectionStringConfigurationRepository.Object,
                                                                                  createContext);

            Should.Throw<ArgumentNullException>(async () => await factory.GetContext(identifer, connectionStringIdentifier, CancellationToken.None));
        }
    }
}
