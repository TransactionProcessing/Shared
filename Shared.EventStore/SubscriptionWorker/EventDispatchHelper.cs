using System.Diagnostics.CodeAnalysis;
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

    [ExcludeFromCodeCoverage]
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
                    var failedResults = results.Where(r => r.IsFailed && !String.IsNullOrEmpty(r.Message)).Select(r => r.Message).ToList();
                    var failedResults2 = results.Where(r => r.IsFailed).Select(r => r.Errors).ToList();
                    var masterList = new List<String>();
                    masterList.AddRange(failedResults);
                    foreach (IEnumerable<String> enumerable in failedResults2) {
                        masterList.AddRange(enumerable);
                    }
                    
                    String errors = String.Join(Environment.NewLine, masterList);
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