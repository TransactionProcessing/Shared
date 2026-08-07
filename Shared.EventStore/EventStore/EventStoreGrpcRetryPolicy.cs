namespace Shared.EventStore;

using System;
using System.IO;
using System.Net.Http;
using System.Net.Sockets;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

internal static class EventStoreGrpcRetryPolicy
{
    internal static async Task ExecuteAsync(Func<Task> operation,
                                            String operationName,
                                            String scope,
                                            Action<LogLevel, String> logger = null)
    {
        await ExecuteAsync(async cancellationToken =>
        {
            await operation();
            return true;
        }, operationName, scope, logger, CancellationToken.None);
    }

    internal static async Task<T> ExecuteAsync<T>(Func<Task<T>> operation,
                                                  String operationName,
                                                  String scope,
                                                  Action<LogLevel, String> logger = null)
    {
        return await ExecuteAsync(async _ => await operation(), operationName, scope, logger, CancellationToken.None);
    }

    private static async Task<T> ExecuteAsync<T>(Func<CancellationToken, ValueTask<T>> operation,
                                                 String operationName,
                                                 String scope,
                                                 Action<LogLevel, String> logger,
                                                 CancellationToken cancellationToken)
    {
        Int32 maxAttempts = EventStoreGrpcRetrySettings.MaxAttempts;
        if (maxAttempts <= 1)
        {
            return await operation(cancellationToken);
        }

        ResiliencePipeline<T> pipeline = new ResiliencePipelineBuilder<T>()
            .AddRetry(new RetryStrategyOptions<T>
            {
                ShouldHandle = args => new ValueTask<bool>(ShouldRetry(args.Outcome.Exception)),
                MaxRetryAttempts = maxAttempts - 1,
                BackoffType = DelayBackoffType.Exponential,
                Delay = EventStoreGrpcRetrySettings.BaseDelay,
                MaxDelay = EventStoreGrpcRetrySettings.MaxDelay,
                UseJitter = EventStoreGrpcRetrySettings.UseJitter,
                OnRetry = args =>
                {
                    Exception? exception = args.Outcome.Exception;
                    logger?.Invoke(LogLevel.Warning, BuildRetryMessage(operationName, scope, args.AttemptNumber + 1, args.RetryDelay, exception));
                    return default;
                }
            })
            .Build();

        try
        {
            return await pipeline.ExecuteAsync(operation, cancellationToken);
        }
        catch (Exception exception)
        {
            logger?.Invoke(LogLevel.Error, BuildFinalFailureMessage(operationName, scope, exception));
            throw;
        }
    }

    private static String BuildRetryMessage(String operationName,
                                            String scope,
                                            Int32 attempt,
                                            TimeSpan retryDelay,
                                            Exception? exception)
    {
        String exceptionDetails = exception == null
            ? "an unknown exception"
            : $"{exception.GetType().Name}: {exception.Message}";

        return $"{operationName} retry {attempt} for {scope} after {exceptionDetails}. Waiting {retryDelay.TotalMilliseconds:0}ms before retrying.";
    }

    private static String BuildFinalFailureMessage(String operationName,
                                                   String scope,
                                                   Exception exception)
    {
        return $"{operationName} failed for {scope} after retry attempts due to {exception.GetType().Name}: {exception.Message}";
    }

    private static Boolean ShouldRetry(Exception? exception)
    {
        return exception != null && ShouldRetryCore(exception);
    }

    private static Boolean ShouldRetryCore(Exception exception)
    {
        return exception switch
        {
            RpcException rpcException when rpcException.StatusCode == StatusCode.Unavailable => true,
            HttpRequestException => true,
            IOException => true,
            SocketException => true,
            AggregateException aggregateException => aggregateException.InnerExceptions.Any(ShouldRetryCore),
            _ when exception.InnerException != null => ShouldRetryCore(exception.InnerException),
            _ => false
        };
    }
}
