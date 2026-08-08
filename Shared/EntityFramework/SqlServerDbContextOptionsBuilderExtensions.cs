using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Shared.EntityFramework;

public sealed class SqlServerRetryOptions
{
    public int? MaxRetryCount { get; set; }

    public TimeSpan? MaxRetryDelay { get; set; }

    public ICollection<int>? AdditionalTransientErrorNumbers { get; set; }
}

public static class SqlServerDbContextOptionsBuilderExtensions
{
    private const int DefaultMaxRetryCount = 6;
    private static readonly TimeSpan DefaultMaxRetryDelay = TimeSpan.FromSeconds(30);

    public static DbContextOptionsBuilder UseSharedSqlServer<TContext>(
        this DbContextOptionsBuilder optionsBuilder,
        string connectionString,
        Action<SqlServerRetryOptions>? configureRetryOptions = null)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(optionsBuilder);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        SqlServerRetryOptions retryOptions = new();
        configureRetryOptions?.Invoke(retryOptions);

        string migrationsAssembly = typeof(TContext).Assembly.GetName().Name
            ?? throw new InvalidOperationException(
                $"Unable to determine the migrations assembly for '{typeof(TContext).FullName}'.");

        optionsBuilder.UseSqlServer(connectionString, sqlServerOptions =>
        {
            sqlServerOptions.MigrationsAssembly(migrationsAssembly);
            ApplyRetryOptions(sqlServerOptions, retryOptions);
        });

        return optionsBuilder;
    }

    private static void ApplyRetryOptions(
        SqlServerDbContextOptionsBuilder sqlServerOptions,
        SqlServerRetryOptions retryOptions)
    {
        bool hasCustomRetryValues = retryOptions.MaxRetryCount.HasValue
            || retryOptions.MaxRetryDelay.HasValue
            || retryOptions.AdditionalTransientErrorNumbers is not null;

        if (!hasCustomRetryValues)
            return;

        int maxRetryCount = retryOptions.MaxRetryCount ?? DefaultMaxRetryCount;
        TimeSpan maxRetryDelay = retryOptions.MaxRetryDelay ?? DefaultMaxRetryDelay;
        int[] additionalTransientErrorNumbers = retryOptions.AdditionalTransientErrorNumbers?.ToArray() ?? Array.Empty<int>();

        sqlServerOptions.EnableRetryOnFailure(maxRetryCount, maxRetryDelay, additionalTransientErrorNumbers);
    }
}
