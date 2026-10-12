using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.IntegrationTesting;

using System.Threading;
using System.Threading.Tasks;

public static class Retry
{
    #region Fields

    /// <summary>
    /// The default retry for
    /// </summary>
    private static readonly TimeSpan DefaultRetryFor = TimeSpan.FromSeconds(60);

    /// <summary>
    /// The default retry interval
    /// </summary>
    private static readonly TimeSpan DefaultRetryInterval = TimeSpan.FromSeconds(5);

    #endregion

    #region Methods

    /// <summary>
    /// Fors the specified action.
    /// </summary>
    /// <param name="action">The action.</param>
    /// <param name="retryFor">The retry for.</param>
    /// <param name="retryInterval">The retry interval.</param>
    /// <returns></returns>
    public static async Task For(Func<Task> action,
                                 TimeSpan? retryFor = null,
                                 TimeSpan? retryInterval = null)
    {
        await For(
            _ => action(),
            CancellationToken.None,
            retryFor,
            retryInterval).ConfigureAwait(false);
    }

    public static async Task For(
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken,
        TimeSpan? retryFor = null,
        TimeSpan? retryInterval = null)
    {
        await For(
            action,
            cancellationToken,
            _ => true,
            retryFor,
            retryInterval).ConfigureAwait(false);
    }

    public static async Task For(
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken,
        Func<Exception, Boolean> shouldRetry,
        TimeSpan? retryFor = null,
        TimeSpan? retryInterval = null)
    {
        cancellationToken.ThrowIfCancellationRequested();
        DateTime startTime = DateTime.Now;
        Exception lastException = null;

        if (retryFor == null)
        {
            retryFor = Retry.DefaultRetryFor;
        }

        while (DateTime.Now.Subtract(startTime).TotalMilliseconds < retryFor.Value.TotalMilliseconds)
        {
            try
            {
                await action(cancellationToken).ConfigureAwait(false);
                lastException = null;
                break;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception e)
            {
                if (shouldRetry(e) == false)
                {
                    throw;
                }

                lastException = e;

                // wait before retrying
                await Task.Delay(retryInterval ?? Retry.DefaultRetryInterval, cancellationToken);
            }
        }

        if (lastException != null)
        {
            throw lastException;
        }
    }

    #endregion
}
