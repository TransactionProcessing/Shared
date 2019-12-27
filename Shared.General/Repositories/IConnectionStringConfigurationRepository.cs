namespace Shared.General.Repositories
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public interface IConnectionStringConfigurationRepository
    {
        #region Methods

        /// <summary>
        /// Deletes the given connection string.
        /// </summary>
        /// <param name="externalIdentifier">The external identifier.</param>
        /// <param name="connectionStringType">Type of the connection string.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        Task DeleteConnectionStringConfiguration(String externalIdentifier, ConnectionStringType connectionStringType, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the connection string.
        /// </summary>
        /// <param name="externalIdentifier">The external identifier.</param>
        /// <param name="connectionStringType">Type of the connection string.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        Task<String> GetConnectionString(String externalIdentifier, ConnectionStringType connectionStringType, CancellationToken cancellationToken);

        /// <summary>
        /// Creates the connection string.
        /// </summary>
        /// <param name="externalIdentifier">The external identifier.</param>
        /// <param name="connectionStringType">Type of the connection string.</param>
        /// <param name="connectionString">The connection string.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        Task CreateConnectionString(String externalIdentifier, ConnectionStringType connectionStringType, String connectionString, CancellationToken cancellationToken);

        #endregion
    }
}
