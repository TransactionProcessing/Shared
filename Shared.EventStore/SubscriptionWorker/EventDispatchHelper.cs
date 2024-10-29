using Shared.Exceptions;
using SimpleResults;

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
        public static async Task<Result> DispatchToHandlers(this IDomainEvent @event,
                                                    List<IDomainEventHandler> eventHandlers,
                                                    CancellationToken cancellationToken)
        {
            // Now execute all the tasks
            Task<Result[]> all = Task.WhenAll(eventHandlers.Select(x => x.Handle(@event, cancellationToken)));

            try
            {
                Result[] results = await all;
                if (results.Any(r => r.IsFailed)) {
                    IEnumerable<String> failedResults = results.Where(r => r.IsFailed).Select(r => r.Message);
                    String errors = String.Join(Environment.NewLine, failedResults);
                    // We have a failed result so need to do something with it
                    return Result.Failure($"One or more event handlers have failed. Error Messages [{errors}]");
                }
            }
            catch (Exception ex)
            {
                if (all.Exception != null)
                {
                    Logger.Logger.LogError(all.Exception);
                    return Result.Failure(all.Exception.GetExceptionMessages());
                }
                return Result.Failure(ex.GetExceptionMessages());
            }
            return Result.Success();
        }

        #endregion
    }
}