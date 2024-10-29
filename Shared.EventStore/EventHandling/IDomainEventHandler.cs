using SimpleResults;

namespace Shared.EventStore.EventHandling
{
    using System.Threading;
    using System.Threading.Tasks;
    using DomainDrivenDesign.EventSourcing;

    public interface IDomainEventHandler
    {
        #region Methods

        /// <summary>
        /// Handles the specified domain event.
        /// </summary>
        /// <param name="domainEvent">The domain event.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        Task<Result> Handle(IDomainEvent domainEvent,
                    CancellationToken cancellationToken);

        #endregion
    }
}