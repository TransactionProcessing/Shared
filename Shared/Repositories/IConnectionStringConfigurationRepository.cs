using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.Repositories;

using System.Threading;
using System.Threading.Tasks;

public interface IConnectionStringConfigurationRepository
{
    #region Methods
        
    Task DeleteConnectionStringConfiguration(String externalIdentifier, String connectionStringIdentifier, CancellationToken cancellationToken);
        
    Task<String> GetConnectionString(String externalIdentifier, String connectionStringIdentifier, CancellationToken cancellationToken);

    Task CreateConnectionString(String externalIdentifier, String connectionStringIdentifier, String connectionString, CancellationToken cancellationToken);

    #endregion
}