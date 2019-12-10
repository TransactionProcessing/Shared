using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.EventStore.EventStore
{
    using DomainDrivenDesign.EventStore;

    public interface IAggregateRepositoryManager
    {
        #region Methods

        /// <summary>
        /// Gets the aggregate repository.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="identifier">The identifier.</param>
        /// <returns></returns>
        IAggregateRepository<T> GetAggregateRepository<T>(Guid identifier) where T : Aggregate, new();

        #endregion
    }
}
