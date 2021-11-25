namespace Shared.EventStore.SubscriptionWorker
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using DomainDrivenDesign.EventSourcing;
    using EventHandling;

    public static class EventDispatchHelper
    {
        #region Methods

        /// <summary>
        /// Dispatches to handlers.
        /// </summary>
        /// <param name="event">The event.</param>
        /// <param name="eventHandlers">The event handlers.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public static async Task DispatchToHandlers(this IDomainEvent @event,
                                                    List<IDomainEventHandler> eventHandlers,
                                                    CancellationToken cancellationToken)
        {
            // Now execute all the tasks
            Task all = Task.WhenAll(eventHandlers.Select(x => x.Handle(@event, cancellationToken)));

            try
            {
                await all;
            }
            catch (Exception)
            {
                if (all.Exception != null)
                {
                    Logger.Logger.LogError(all.Exception);
                    throw all.Exception;
                }
            }
        }

        #endregion
    }
}