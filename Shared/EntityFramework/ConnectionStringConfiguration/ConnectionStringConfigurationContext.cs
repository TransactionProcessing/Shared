using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.EntityFramework.ConnectionStringConfiguration;

using System.Diagnostics.CodeAnalysis;

[Obsolete]
[ExcludeFromCodeCoverage]
public class ConnectionStringConfigurationContext : DbContext
{
    #region Fields

    /// <summary>
    /// The connection string
    /// </summary>
    private readonly String ConnectionString;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionStringConfigurationContext" /> class using the connection string called ReadModelContext in the app.config file.
    /// </summary>
    public ConnectionStringConfigurationContext()
    {
        // Paramaterless constructor required for migrations.
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionStringConfigurationContext" /> class using the connection string passed in.
    /// </summary>
    /// <param name="connectionString">The connection string.</param>
    public ConnectionStringConfigurationContext(String connectionString)
    {
        this.ConnectionString = connectionString;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionStringConfigurationContext"/> class.
    /// </summary>
    /// <param name="dbContextOptions">The database context options.</param>
    public ConnectionStringConfigurationContext(DbContextOptions dbContextOptions) : base(dbContextOptions)
    {
    }

    #endregion

    /// <summary>
    /// Gets or sets the connection string configuration.
    /// </summary>
    /// <value>
    /// The connection string configuration.
    /// </value>
    public DbSet<ConnectionStringConfiguration> ConnectionStringConfiguration { get; set; }

    /// <summary>
    /// Gets or sets the type of the connection string.
    /// </summary>
    /// <value>
    /// The type of the connection string.
    /// </value>
    public DbSet<ConnectionStringType> ConnectionStringType { get; set; }

    /// <summary>
    /// <para>
    /// Override this method to configure the database (and other options) to be used for this context.
    /// This method is called for each instance of the context that is created.
    /// </para>
    /// <para>
    /// In situations where an instance of <see cref="T:Microsoft.Data.Entity.Infrastructure.DbContextOptions" /> may or may not have been passed
    /// to the constructor, you can use <see cref="P:Microsoft.Data.Entity.DbContextOptionsBuilder.IsConfigured" /> to determine if
    /// the options have already been set, and skip some or all of the logic in
    /// <see cref="M:Microsoft.Data.Entity.DbContext.OnConfiguring(Microsoft.Data.Entity.DbContextOptionsBuilder)" />.
    /// </para>
    /// </summary>
    /// <param name="optionsBuilder">A builder used to create or modify options for this context. Databases (and other extensions)
    /// typically define extension methods on this object that allow you to configure the context.</param>
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!string.IsNullOrWhiteSpace(this.ConnectionString))
        {
            optionsBuilder.UseSqlServer(this.ConnectionString);
        }

        base.OnConfiguring(optionsBuilder);
    }

    /// <summary>
    /// Override this method to further configure the model that was discovered by convention from the entity types
    /// exposed in <see cref="T:Microsoft.EntityFrameworkCore.DbSet`1" /> properties on your derived context. The resulting model may be cached
    /// and re-used for subsequent instances of your derived context.
    /// </summary>
    /// <param name="modelBuilder">The builder being used to construct the model for this context. Databases (and other extensions) typically
    /// define extension methods on this object that allow you to configure aspects of the model that are specific
    /// to a given database.</param>
    /// <remarks>
    /// If a model is explicitly set on the options for this context (via <see cref="M:Microsoft.EntityFrameworkCore.DbContextOptionsBuilder.UseModel(Microsoft.EntityFrameworkCore.Metadata.IModel)" />)
    /// then this method will not be run.
    /// </remarks>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ConnectionStringConfiguration>().HasIndex(c => new
        {
            c.ExternalIdentifier,
            c.ConnectionStringIdentifier
        }).IsUnique();
    }
}