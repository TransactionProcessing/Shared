namespace Shared.EntityFramework
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.EntityFrameworkCore;
    using Repositories;

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

        /// <summary>
        /// Gets a context for the given identifier
        /// </summary>
        /// <param name="identifier">The identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// DbContext.
        /// </returns>
        public virtual async Task<T> GetContext(Guid identifier,
                                                CancellationToken cancellationToken)
        {
            this.GuardIdentifier(identifier);

            String connectionString =
                await this.ConnectionStringConfigurationRepository.GetConnectionString(identifier.ToString(), ConnectionStringType.ReadModel, cancellationToken);
            return this.CreateContext(connectionString);
        }

        /// <summary>
        /// Guards the identifier.
        /// </summary>
        /// <param name="identifier">The identifier.</param>
        /// <exception cref="ArgumentNullException">identifier</exception>
        private void GuardIdentifier(Guid identifier)
        {
            if (identifier == Guid.Empty)
            {
                throw new ArgumentNullException(nameof(identifier));
            }
        }

        #endregion
    }
}