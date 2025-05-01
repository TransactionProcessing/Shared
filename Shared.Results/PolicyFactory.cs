using System.Diagnostics.CodeAnalysis;
using Polly;
using Polly.Retry;
using SimpleResults;

namespace Shared.Results
{
    public static class PolicyFactory
    {
        private enum LogType { Retry, Final }

        public static IAsyncPolicy<T> CreatePolicy<T>(int retryCount = 5, TimeSpan? retryDelay = null, string policyTag = "",
                                                      Func<T, Boolean>? shouldRetry = null) where T : ResultBase
        {
            Func<T, Boolean> retryCondition = shouldRetry ?? (ShouldRetry);
            TimeSpan delay = retryDelay ?? TimeSpan.FromSeconds(5);
            return CreateRetryPolicy<T>(retryCount, delay, policyTag, retryCondition);
        }

        public static async Task<T> ExecuteWithPolicyAsync<T>(Func<Task<T>> action, IAsyncPolicy<T> policy, string policyTag = "") where T : ResultBase
        {
            Context context = new Context();
            T result = await policy.ExecuteAsync(_ => action(), context);

            int retryCount = context.TryGetValue("RetryCount", out Object? retryObj) && retryObj is int r ? r : 0;
            LogResult(policyTag, result, retryCount, LogType.Final);

            return result;
        }

        private static AsyncRetryPolicy<T> CreateRetryPolicy<T>(int retryCount, TimeSpan retryDelay, string policyTag, Func<T, bool> shouldRetry) where T : ResultBase
        {
            return Policy<T>
                .HandleResult(shouldRetry)
                .WaitAndRetryAsync(
                    retryCount,
                    _ => retryDelay,
                    (result, _, attempt, context) =>
                    {
                        context["RetryCount"] = attempt;
                        LogResult(policyTag, result.Result, attempt, LogType.Retry);
                    });
        }

        private static bool ShouldRetry(ResultBase result)
        {
            return result switch
            {
                { IsSuccess: true } => false,
                { Errors: var errors } when errors?.Any(MatchesRetryCondition) == true => true,
                { Message: not null } when MatchesRetryCondition(result.Message) => true,
                _ => false
            };
        }

        private static bool MatchesRetryCondition(string input)
        {
            return input.Contains("WrongExpectedVersion", StringComparison.OrdinalIgnoreCase)
                   || input.Contains("DeadlineExceeded", StringComparison.OrdinalIgnoreCase);
        }

        [ExcludeFromCodeCoverage]
        private static string FormatResultMessage(ResultBase result)
        {
            return result switch
            {
                { IsSuccess: true } => "Success",
                { IsSuccess: false, Message: not "" } => result.Message,
                { IsSuccess: false, Errors: var errors } when errors?.Any() == true => string.Join(", ", errors),
                _ => "Unknown Error"
            };
        }

        [ExcludeFromCodeCoverage]
        private static void LogResult(string policyTag, ResultBase result, int retryCount, LogType type)
        {
            string message = FormatResultMessage(result);

            switch (type)
            {
                case LogType.Retry:
                    Logger.Logger.LogWarning($"{policyTag} - Retry {retryCount} due to error: {message}. Waiting before retrying...");
                    break;

                case LogType.Final:
                    string retryMessage = retryCount > 0 ? $" after {retryCount} retries." : "";
                    Logger.Logger.LogWarning($"{policyTag} - {message}{retryMessage}");
                    break;
            }
        }
    }


}
