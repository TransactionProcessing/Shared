namespace Shared.EntityFramework
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.EntityFrameworkCore;

    public interface IDbContextFactory<T> where T : DbContext
    {
        #region Methods

        /// <summary>
        /// Gets a context for the given identifier
        /// </summary>
        /// <param name="identifier">The identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// DbContext.
        /// </returns>
        Task<T> GetContext(Guid identifier,
                           CancellationToken cancellationToken);

        #endregion
    }
}