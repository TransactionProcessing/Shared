namespace Shared.EntityFramework
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.EntityFrameworkCore;

    [Obsolete("Replaced by DbContextResolver")]
    public interface IDbContextFactory<T> where T : DbContext
    {
        #region Methods
        
        Task<T> GetContext(Guid identifier,
                           String connectionStringIdentifier,
                           CancellationToken cancellationToken);

        #endregion
    }
}