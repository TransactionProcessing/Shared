namespace Shared.EntityFramework;

using System;
using System.Threading;
using System.Threading.Tasks;
using General;
using Microsoft.EntityFrameworkCore;
using Repositories;

[Obsolete("Replaced by DbContextResolver")]
public class DbContextFactory<T> : IDbContextFactory<T> where T : DbContext
{
    #region Fields

    private readonly IConnectionStringConfigurationRepository ConnectionStringConfigurationRepository;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="DbContextFactory{T}" /> class.
    /// </summary>
    /// <param name="connectionStringConfigurationRepository">The connection string configuration repository.</param>
    /// <param name="createContext">The create context.</param>
    public DbContextFactory(IConnectionStringConfigurationRepository connectionStringConfigurationRepository,
                            Func<String, T> createContext)
    {
        this.ConnectionStringConfigurationRepository = connectionStringConfigurationRepository;
        this.CreateContext = createContext;
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the context resolver.
    /// </summary>
    /// <value>The context resolver.</value>
    private Func<String, T> CreateContext { get; }

    #endregion

    #region Methods

    public virtual async Task<T> GetContext(Guid identifier,
                                            String connectionStringIdentifier,            
                                            CancellationToken cancellationToken)
    {
        Guard.ThrowIfInvalidGuid(identifier,nameof(identifier));
        Guard.ThrowIfNullOrEmpty(connectionStringIdentifier,nameof(connectionStringIdentifier));

        String connectionString =
            await this.ConnectionStringConfigurationRepository.GetConnectionString(identifier.ToString(), connectionStringIdentifier, cancellationToken);
        return this.CreateContext(connectionString);
    }
        
    #endregion
}