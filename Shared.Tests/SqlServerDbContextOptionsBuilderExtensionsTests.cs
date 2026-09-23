using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.SqlServer;
using Shared.EntityFramework;
using Shouldly;
using System;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Shared.Tests;

public class SqlServerDbContextOptionsBuilderExtensionsTests
{
    [Fact]
    public void UseSharedSqlServer_UsesDefaultRetryValuesAndMigrationsAssembly()
    {
        DbContextOptionsBuilder<TestDbContext> optionsBuilder = new();

        optionsBuilder.UseSharedSqlServer<TestDbContext>("Server=.;Database=DefaultDb;Trusted_Connection=True;");

        using TestDbContext context = new(optionsBuilder.Options);

        var executionStrategy = context.Database.CreateExecutionStrategy();
        executionStrategy.RetriesOnFailure.ShouldBeFalse();

        object relationalExtension = optionsBuilder.Options.Extensions.Single(extension =>
            extension.GetType().FullName == "Microsoft.EntityFrameworkCore.SqlServer.Infrastructure.Internal.SqlServerOptionsExtension");

        PropertyInfo migrationsAssemblyProperty = relationalExtension.GetType().GetProperty(
            "MigrationsAssembly",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Expected a migrations assembly property.");

        string? migrationsAssembly = migrationsAssemblyProperty.GetValue(relationalExtension) as string;
        migrationsAssembly.ShouldBe(typeof(TestDbContext).Assembly.GetName().Name);
    }

    [Fact]
    public void UseSharedSqlServer_AppliesCustomRetryValues()
    {
        DbContextOptionsBuilder<TestDbContext> optionsBuilder = new();

        optionsBuilder.UseSharedSqlServer<TestDbContext>(
            "Server=.;Database=DefaultDb;Trusted_Connection=True;",
            retryOptions =>
            {
                retryOptions.MaxRetryCount = 3;
                retryOptions.MaxRetryDelay = TimeSpan.FromSeconds(5);
                retryOptions.AdditionalTransientErrorNumbers = new[] { 4060, 10928 };
            });

        using TestDbContext context = new(optionsBuilder.Options);

        SqlServerRetryingExecutionStrategy executionStrategy =
            context.Database.CreateExecutionStrategy() as SqlServerRetryingExecutionStrategy
            ?? throw new InvalidOperationException("Expected SQL Server retrying execution strategy.");

        executionStrategy.MaxRetryCount.ShouldBe(3);
        executionStrategy.MaxRetryDelay.ShouldBe(TimeSpan.FromSeconds(5));
        executionStrategy.AdditionalErrorNumbers.ShouldBe(new[] { 4060, 10928 });
    }

    [Fact]
    public void SqlServerRetryOptions_ExposeRetryConfigurationShape()
    {
        SqlServerRetryOptions options = new()
        {
            MaxRetryCount = 9,
            MaxRetryDelay = TimeSpan.FromSeconds(12),
            AdditionalTransientErrorNumbers = new[] { 1, 2, 3 }
        };

        options.MaxRetryCount.ShouldBe(9);
        options.MaxRetryDelay.ShouldBe(TimeSpan.FromSeconds(12));
        options.AdditionalTransientErrorNumbers.ShouldBe(new[] { 1, 2, 3 });
    }

    [Fact]
    public void UseSharedSqlServer_WithOnlyRetryCount_UsesDefaultDelayAndErrorNumbers()
    {
        DbContextOptionsBuilder<TestDbContext> optionsBuilder = new();

        optionsBuilder.UseSharedSqlServer<TestDbContext>(
            "Server=.;Database=DefaultDb;Trusted_Connection=True;",
            retryOptions => retryOptions.MaxRetryCount = 2);

        SqlServerRetryingExecutionStrategy strategy =
            new TestDbContext(optionsBuilder.Options).Database.CreateExecutionStrategy()
                as SqlServerRetryingExecutionStrategy;

        strategy.MaxRetryCount.ShouldBe(2);
        strategy.MaxRetryDelay.ShouldBe(TimeSpan.FromSeconds(30));
        strategy.AdditionalErrorNumbers.ShouldBeEmpty();
    }

    [Fact]
    public void UseSharedSqlServer_WithOnlyAdditionalErrors_UsesDefaultRetryValues()
    {
        DbContextOptionsBuilder<TestDbContext> optionsBuilder = new();

        optionsBuilder.UseSharedSqlServer<TestDbContext>(
            "Server=.;Database=DefaultDb;Trusted_Connection=True;",
            retryOptions => retryOptions.AdditionalTransientErrorNumbers = new[] { 4060 });

        SqlServerRetryingExecutionStrategy strategy =
            new TestDbContext(optionsBuilder.Options).Database.CreateExecutionStrategy()
                as SqlServerRetryingExecutionStrategy;

        strategy.MaxRetryCount.ShouldBe(6);
        strategy.MaxRetryDelay.ShouldBe(TimeSpan.FromSeconds(30));
        strategy.AdditionalErrorNumbers.ShouldBe(new[] { 4060 });
    }

    [Fact]
    public void UseSharedSqlServer_WithOnlyRetryDelay_UsesDefaultCountAndErrorNumbers()
    {
        DbContextOptionsBuilder<TestDbContext> optionsBuilder = new();

        optionsBuilder.UseSharedSqlServer<TestDbContext>(
            "Server=.;Database=DefaultDb;Trusted_Connection=True;",
            retryOptions => retryOptions.MaxRetryDelay = TimeSpan.FromSeconds(7));

        SqlServerRetryingExecutionStrategy strategy =
            new TestDbContext(optionsBuilder.Options).Database.CreateExecutionStrategy()
                as SqlServerRetryingExecutionStrategy;

        strategy.MaxRetryCount.ShouldBe(6);
        strategy.MaxRetryDelay.ShouldBe(TimeSpan.FromSeconds(7));
        strategy.AdditionalErrorNumbers.ShouldBeEmpty();
    }

    [Fact]
    public void UseSharedSqlServer_RejectsNullOrBlankArguments()
    {
        Should.Throw<ArgumentNullException>(() =>
            SqlServerDbContextOptionsBuilderExtensions.UseSharedSqlServer<TestDbContext>(
                null,
                "connection"));
        Should.Throw<ArgumentException>(() =>
            new DbContextOptionsBuilder().UseSharedSqlServer<TestDbContext>(" "));
    }
}
