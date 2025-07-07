using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace Shared.EntityFramework
{
    public interface IDbContextResolver<TContext> where TContext : DbContext
    {
        ResolvedDbContext<TContext> Resolve(String connectionStringKey);
        ResolvedDbContext<TContext> Resolve(String connectionStringKey, String databaseNameSuffix);
    }

    public class DbContextResolver<TContext> : IDbContextResolver<TContext> where TContext : DbContext
    {
        private readonly IServiceProvider _rootProvider;
        private readonly IConfiguration _config;

        public DbContextResolver(IServiceProvider rootProvider, IConfiguration config)
        {
            _rootProvider = rootProvider;
            _config = config;
        }

        public ResolvedDbContext<TContext> Resolve(String connectionStringKey) {
            return this.Resolve(connectionStringKey, String.Empty);
        }

        public ResolvedDbContext<TContext> Resolve(String connectionStringKey, String databaseNameSuffix)
        {
            IServiceScope scope = _rootProvider.CreateScope();
            String connectionString = _config.GetConnectionString(connectionStringKey);
            if (String.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException($"Connection string for '{connectionStringKey}' not found.");
            
            // Update the connection string with the identifier if needed
            if (!String.IsNullOrWhiteSpace(databaseNameSuffix)) {
                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(connectionString);
                builder.InitialCatalog = $"{builder.InitialCatalog}-{databaseNameSuffix}";
                connectionString = builder.ConnectionString;


                // Create an isolated service collection and provider
                ServiceCollection services = new();
                services.AddDbContext<TContext>(options => { options.UseSqlServer(connectionString); });

                ServiceProvider provider = services.BuildServiceProvider();
                scope = provider.CreateScope();

                return new ResolvedDbContext<TContext>(scope);
            }
            // Standard resolution using DI container
            return new ResolvedDbContext<TContext>(scope);
        }
    }
}


