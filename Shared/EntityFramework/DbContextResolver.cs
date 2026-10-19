#nullable enable
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Shared.EntityFramework;

public interface IDbContextResolver<TContext> where TContext : DbContext
{
    ResolvedDbContext<TContext> Resolve(String connectionStringKey);
    ResolvedDbContext<TContext> Resolve(String connectionStringKey, String databaseNameSuffix);
}

public class DbContextResolver<TContext> : IDbContextResolver<TContext> where TContext : DbContext
{
    private readonly IServiceProvider _rootProvider;
    private readonly IConfiguration _config;
    private readonly DbContextResolverOptions _options;

    public DbContextResolver(IServiceProvider rootProvider, IConfiguration config)
        : this(rootProvider, config, new DbContextResolverOptions())
    {
    }

    public DbContextResolver(IServiceProvider rootProvider,
                             IConfiguration config,
                             DbContextResolverOptions options)
    {
        _rootProvider = rootProvider ?? throw new ArgumentNullException(nameof(rootProvider));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public ResolvedDbContext<TContext> Resolve(String connectionStringKey)
    {
        return this.Resolve(connectionStringKey, String.Empty);
    }

    public ResolvedDbContext<TContext> Resolve(String connectionStringKey, String databaseNameSuffix)
    {
        String? connectionString = _config.GetConnectionString(connectionStringKey);
        if (String.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException($"Connection string for '{connectionStringKey}' not found.");

        SqlConnectionStringBuilder connectionStringBuilder = new(connectionString);
        if (String.IsNullOrWhiteSpace(databaseNameSuffix))
        {
            IServiceScope regularScope = _rootProvider.CreateScope();
            try
            {
                return new ResolvedDbContext<TContext>(regularScope);
            }
            catch
            {
                regularScope.Dispose();
                throw;
            }
        }

        connectionStringBuilder.InitialCatalog = $"{connectionStringBuilder.InitialCatalog}-{databaseNameSuffix}";
        connectionString = connectionStringBuilder.ConnectionString;

        IServiceScope scope = _rootProvider.CreateScope();
        try
        {
            DbContextOptionsBuilder<TContext> optionsBuilder = new();
            SqlServerRetryOptions retryOptions = _options.RetryOptionsFactory?.Invoke(connectionStringKey)
                ?? new SqlServerRetryOptions
                {
                    MaxRetryCount = SqlServerDbContextOptionsBuilderExtensions.DefaultMaxRetryCount,
                    MaxRetryDelay = SqlServerDbContextOptionsBuilderExtensions.DefaultMaxRetryDelay
                };

            optionsBuilder.UseSharedSqlServer<TContext>(connectionString, retry =>
            {
                retry.MaxRetryCount = retryOptions.MaxRetryCount;
                retry.MaxRetryDelay = retryOptions.MaxRetryDelay;
                retry.AdditionalTransientErrorNumbers = retryOptions.AdditionalTransientErrorNumbers;
            });

            if (_options.Interceptors.Count > 0)
                optionsBuilder.AddInterceptors(_options.Interceptors);

            TContext context = ActivatorUtilities.CreateInstance<TContext>(scope.ServiceProvider, optionsBuilder.Options);
            return new ResolvedDbContext<TContext>(scope, context);
        }
        catch
        {
            scope.Dispose();
            throw;
        }
    }
}
