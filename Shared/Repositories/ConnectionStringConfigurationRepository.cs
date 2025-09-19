namespace Shared.Repositories
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading;
    using System.Threading.Tasks;
    using EntityFramework.ConnectionStringConfiguration;
    using Exceptions;
    using Microsoft.EntityFrameworkCore;

    [ExcludeFromCodeCoverage]
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

        public async Task DeleteConnectionStringConfiguration(String externalIdentifier, String connectionStringIdentifier, CancellationToken cancellationToken)
        {
            this.GuardAgainstNoExternalIdentifier(externalIdentifier);
            this.GuardAgainstNoConnectionStringIdentifier(connectionStringIdentifier);

            // Find the record in the config repository
            using (ConnectionStringConfigurationContext context = this.ContextResolver())
            {
                ConnectionStringConfiguration configuration =
                    await context.ConnectionStringConfiguration.SingleOrDefaultAsync(c => c.ExternalIdentifier == externalIdentifier &&
                                                                                          c.ConnectionStringIdentifier == connectionStringIdentifier, cancellationToken);

                if (configuration == null)
                {
                    throw new
                        NotFoundException($"No connection string configuration found for External Identifier [{externalIdentifier}]");
                }

                context.Remove(configuration);

                await context.SaveChangesAsync(cancellationToken);
            }
        }

        public async Task<String> GetConnectionString(String externalIdentifier, String connectionStringIdentifier, CancellationToken cancellationToken)
        {
            this.GuardAgainstNoExternalIdentifier(externalIdentifier);
            this.GuardAgainstNoConnectionStringIdentifier(connectionStringIdentifier);

            // Find the record in the config repository
            using (ConnectionStringConfigurationContext context = this.ContextResolver())
            {
                ConnectionStringConfiguration configuration =
                    await context.ConnectionStringConfiguration.SingleOrDefaultAsync(c => c.ExternalIdentifier == externalIdentifier && 
                                                                                          c.ConnectionStringIdentifier == connectionStringIdentifier,
                                                                                     cancellationToken);

                if (configuration == null)
                {
                    throw new
                        NotFoundException($"No connection string configuration found for External Identifier [{externalIdentifier}] and Connection String Identifier [{connectionStringIdentifier}]");
                }

                return configuration.ConnectionString;
            }
        }

        public async Task CreateConnectionString(String externalIdentifier, String connectionStringIdentifier, String connectionString, CancellationToken cancellationToken)
        {
            this.GuardAgainstNoExternalIdentifier(externalIdentifier);
            this.GuardAgainstNoConnectionStringIdentifier(connectionStringIdentifier);

            // Find the record in the config repository
            using (ConnectionStringConfigurationContext context = this.ContextResolver())
            {
                ConnectionStringConfiguration configuration = new()
                                                              {
                                                                  ExternalIdentifier = externalIdentifier,
                                                                  ConnectionString = connectionString,
                                                                  ConnectionStringIdentifier = connectionStringIdentifier
                                                              };

                await context.AddAsync(configuration, cancellationToken);

                await context.SaveChangesAsync(cancellationToken);
            }
        }

        private void GuardAgainstNoExternalIdentifier(String externalIdentifier)
        {
            //Check if the external Id is present
            if (String.IsNullOrWhiteSpace(externalIdentifier))
            {
                throw new ArgumentException("Value cannot be empty.", nameof(externalIdentifier));
            }
        }

        private void GuardAgainstNoConnectionStringIdentifier(String connectionStringIdentifier)
        {
            //Check if the external Id is present
            if (String.IsNullOrWhiteSpace(connectionStringIdentifier))
            {
                throw new ArgumentException("Value cannot be empty.", nameof(connectionStringIdentifier));
            }
        }

        #endregion
    }
}