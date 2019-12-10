namespace Shared.Repositories
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using EntityFramework.ConnectionStringConfiguration;
    using Exceptions;
    using Microsoft.EntityFrameworkCore;

    public class ConnectionStringConfigurationRepository : IConnectionStringConfigurationRepository
    {
        #region Fields

        /// <summary>
        /// The context resolver
        /// </summary>
        private readonly Func<ConnectionStringConfigurationContext> ContextResolver;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionStringRepository" /> class.
        /// </summary>
        /// <param name="contextResolver">The context resolver.</param>
        public ConnectionStringConfigurationRepository(Func<ConnectionStringConfigurationContext> contextResolver)
        {
            this.ContextResolver = contextResolver;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Deletes the given connection string.
        /// </summary>
        /// <param name="externalIdentifier">The external identifier.</param>
        /// <param name="connectionStringType">Type of the connection string.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="NotFoundException">No configuration found for Connection String Identifier [{connectionStringIdentifier}] and DataModelType [{dataModelType}]</exception>
        public async Task DeleteConnectionStringConfiguration(String externalIdentifier, ConnectionStringType connectionStringType, CancellationToken cancellationToken)
        {
            this.GuardAgainstNoExternalIdentifier(externalIdentifier);

            // Find the record in the config repository
            using (ConnectionStringConfigurationContext context = this.ContextResolver())
            {
                ConnectionStringConfiguration configuration =
                    await context.ConnectionStringConfiguration.SingleOrDefaultAsync(c => c.ExternalIdentifier == externalIdentifier &&
                                                                                          c.ConnectionStringTypeId == (Int32)connectionStringType, cancellationToken);

                if (configuration == null)
                {
                    throw new
                        NotFoundException($"No connection string configuration found for External Identifier [{externalIdentifier}]");
                }

                context.Remove(configuration);

                await context.SaveChangesAsync(cancellationToken);
            }
        }

        /// <summary>
        /// Gets the connection string.
        /// </summary>
        /// <param name="externalIdentifier">The external identifier.</param>
        /// <param name="connectionStringType">Type of the connection string.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        /// <exception cref="NotFoundException">No configuration found for Connection String Identifier [{connectionStringIdentifier}] and DataModelType [{dataModelType}]</exception>
        public async Task<String> GetConnectionString(String externalIdentifier, ConnectionStringType connectionStringType, CancellationToken cancellationToken)
        {
            this.GuardAgainstNoExternalIdentifier(externalIdentifier);

            // Find the record in the config repository
            using (ConnectionStringConfigurationContext context = this.ContextResolver())
            {
                ConnectionStringConfiguration configuration =
                    await context.ConnectionStringConfiguration.SingleOrDefaultAsync(c => c.ExternalIdentifier == externalIdentifier && 
                                                                                          c.ConnectionStringTypeId == (Int32)connectionStringType,
                                                                                     cancellationToken);

                if (configuration == null)
                {
                    throw new
                        NotFoundException($"No connection string configuration found for External Identifier [{externalIdentifier}] and Connection String Type [{connectionStringType}]");
                }

                return configuration.ConnectionString;
            }
        }

        /// <summary>
        /// Creates the connection string.
        /// </summary>
        /// <param name="externalIdentifier">The external identifier.</param>
        /// <param name="connectionStringType">Type of the connection string.</param>
        /// <param name="connectionString">The connection string.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public async Task CreateConnectionString(String externalIdentifier, ConnectionStringType connectionStringType, String connectionString, CancellationToken cancellationToken)
        {
            this.GuardAgainstNoExternalIdentifier(externalIdentifier);

            // Find the record in the config repository
            using (ConnectionStringConfigurationContext context = this.ContextResolver())
            {
                ConnectionStringConfiguration configuration = new ConnectionStringConfiguration
                                                              {
                                                                  ExternalIdentifier = externalIdentifier,
                                                                  ConnectionString = connectionString,
                                                                  ConnectionStringTypeId = (Int32)connectionStringType
                                                              };

                await context.AddAsync(configuration, cancellationToken);

                await context.SaveChangesAsync(cancellationToken);
            }
        }

        /// <summary>
        /// Guards the against no connection string identifier.
        /// </summary>
        /// <param name="externalIdentifier">The external identifier.</param>
        /// <exception cref="ArgumentException">Value cannot be empty. - connectionStringIdentifier</exception>
        /// <exception cref="System.ArgumentException">Value cannot be empty. - connectionStringIdentifier</exception>
        private void GuardAgainstNoExternalIdentifier(String externalIdentifier)
        {
            //Check if the external Id is present
            if (String.IsNullOrWhiteSpace(externalIdentifier))
            {
                throw new ArgumentException("Value cannot be empty.", nameof(externalIdentifier));
            }
        }

        #endregion
    }
}