using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shared.Logger;
using Shared.Results;
using Shouldly;

namespace Shared.Tests;

    using global::Shared.General;
    using Moq;
    using Polly;
    using SimpleResults;
    using System;
    using System.Threading.Tasks;
    using Xunit;

    namespace Shared.Results.Tests
    {
        public class PolicyFactoryTests
        {
            [Fact]
            public async Task CreatePolicy_NoRetriesNeeded()
            {
                // Arrange
                int retryCount = 3;
                int executionCount = 0;
                IAsyncPolicy<Result> policy = PolicyFactory.CreatePolicy<Result>(retryCount: retryCount, retryDelay: TimeSpan.Zero, policyTag: "TestPolicy");

                Queue<Result> resultSequence = new Queue<Result>(new[]
                {
                    Result.Success()
                });

                Task<Result> Action()
                {
                    executionCount++;
                    return Task.FromResult(resultSequence.Dequeue());
                }

                // Act
                Result result = await PolicyFactory.ExecuteWithPolicyAsync(Action, policy, "TestPolicy");

                // Assert
                result.IsSuccess.ShouldBeTrue();
                executionCount.ShouldBe(1);
            }

            [Fact]
            public async Task CreatePolicy_NoRetriesNeeded_NotSuccess()
            {
                // Arrange
                int retryCount = 3;
                int executionCount = 0;

                IAsyncPolicy<Result> policy = PolicyFactory.CreatePolicy<Result>(retryCount: retryCount, retryDelay: TimeSpan.Zero, policyTag: "TestPolicy");

                Queue<Result> resultSequence = new Queue<Result>(new[]
                {
                    Result.Failure()
                });

                Task<Result> Action()
                {
                    executionCount++;
                    return Task.FromResult(resultSequence.Dequeue());
                }

                // Act
                Result result = await PolicyFactory.ExecuteWithPolicyAsync(Action, policy, "TestPolicy");

                // Assert
                result.IsSuccess.ShouldBeFalse();
                executionCount.ShouldBe(1);
            }

            [Fact]
            public async Task CreatePolicy_RetriesConfiguredNumberOfTimes_ErrorInMessage()
            {
                // Arrange
                int retryCount = 3;
                int executionCount = 0;

                var policy = PolicyFactory.CreatePolicy<Result>(retryCount: retryCount, retryDelay: TimeSpan.Zero, policyTag: "TestPolicy");

                var resultSequence = new Queue<Result>(new[]
                {
                    Result.Failure("DeadlineExceeded"),
                    Result.Failure("DeadlineExceeded"),
                    Result.Failure("DeadlineExceeded"),
                    Result.Success()
                });

                Task<Result> Action()
                {
                    executionCount++;
                    return Task.FromResult(resultSequence.Dequeue());
                }

                // Act
                Result result = await PolicyFactory.ExecuteWithPolicyAsync(Action, policy, "TestPolicy");

                // Assert
                result.IsSuccess.ShouldBeTrue();
                executionCount.ShouldBe(retryCount+1); // 3 retries + 1 initial attempt
            }

            [Fact]
            public async Task CreatePolicy_RetriesConfiguredNumberOfTimes_ErrorInErrors()
            {
                // Arrange
                int retryCount = 3;
                int executionCount = 0;

                var policy = PolicyFactory.CreatePolicy<Result>(retryCount: retryCount, retryDelay: TimeSpan.Zero, policyTag: "TestPolicy");

                var resultSequence = new Queue<Result>(new[]
                {
                    Result.Failure(new List<String>(){"DeadlineExceeded"}),
                    Result.Failure(new List<String>(){"DeadlineExceeded"}),
                    Result.Failure(new List<String>(){"DeadlineExceeded"}),
                    Result.Success()
                });

                Task<Result> Action()
                {
                    executionCount++;
                    return Task.FromResult(resultSequence.Dequeue());
                }

                // Act
                Result result = await PolicyFactory.ExecuteWithPolicyAsync(Action, policy, "TestPolicy");

                // Assert
                result.IsSuccess.ShouldBeTrue();
                executionCount.ShouldBe(retryCount + 1); // 3 retries + 1 initial attempt
            }

            [Fact]
            public async Task CreatePolicy_RetriesNotSpecified_UsesDefaultCount()
            {
                // Arrange
                int defaultRetryCount = 5;
                int executionCount = 0;

                var policy = PolicyFactory.CreatePolicy<Result>(retryDelay: TimeSpan.Zero, policyTag: "TestPolicy");

                var resultSequence = new Queue<Result>(new[]
                {
                    Result.Failure("DeadlineExceeded"),
                    Result.Failure("DeadlineExceeded"),
                    Result.Failure("DeadlineExceeded"),
                    Result.Failure("DeadlineExceeded"),
                    Result.Failure("DeadlineExceeded"),
                    Result.Success()
                });

                Task<Result> Action()
                {
                    executionCount++;
                    return Task.FromResult(resultSequence.Dequeue());
                }

                // Act
                Result result = await PolicyFactory.ExecuteWithPolicyAsync(Action, policy, "TestPolicy");

                // Assert
                result.IsSuccess.ShouldBeTrue();
                executionCount.ShouldBe(defaultRetryCount + 1); // 5 retries + 1 initial attempt
            }

            [Fact]
            public async Task CreatePolicy_RetriesConfiguredNumberOfTimes_CustomShouldRetry()
            {
                // Arrange
                int retryCount = 3;
                int executionCount = 0;

                var policy = PolicyFactory.CreatePolicy<Result>(retryCount: retryCount, retryDelay: TimeSpan.Zero, policyTag: "TestPolicy", ShouldRetry);

                var resultSequence = new Queue<Result>(new[]
                {
                    Result.Conflict(),
                    Result.Conflict(),
                    Result.Conflict(),
                    Result.Success()
                });

                Task<Result> Action()
                {
                    executionCount++;
                    return Task.FromResult(resultSequence.Dequeue());
                }

                // Act
                Result result = await PolicyFactory.ExecuteWithPolicyAsync(Action, policy, "TestPolicy");

                // Assert
                result.IsSuccess.ShouldBeTrue();
                executionCount.ShouldBe(retryCount + 1); // 3 retries + 1 initial attempt
            }

            [Fact]
            public async Task CreatePolicy_NoRetriesNeeded_ExceptionThrown()
            {
                // Arrange
                int retryCount = 3;
                int executionCount = 0;

                IAsyncPolicy<Result> policy = PolicyFactory.CreatePolicy<Result>(retryCount: retryCount, retryDelay: TimeSpan.Zero, policyTag: "TestPolicy");

                Task<Result> Action()
                {
                    executionCount++;
                    if (executionCount < retryCount)
                    {
                        throw new Exception("Test exception");
                    }
                    return Task.FromResult(Result.Success());
                }

                // Act
                Should.Throw<Exception>(async () => await PolicyFactory.ExecuteWithPolicyAsync(Action, policy, "TestPolicy"));

                // Assert
                executionCount.ShouldBe(1);
            }

            [Fact]
            public async Task CreatePolicy_RetriesConfiguredNumberOfTimes_CustomShouldRetryOnException()
            {
                // Arrange
                int retryCount = 3;
                int executionCount = 0;

                IAsyncPolicy<Result> policy = PolicyFactory.CreatePolicy<Result>(retryCount: retryCount, retryDelay: TimeSpan.Zero, policyTag: "TestPolicy", null, ShouldRetryException);

                Task<Result> Action()
                {
                    executionCount++;
                    if (executionCount <= retryCount)
                    {
                        throw new TaskCanceledException("Test exception", new TimeoutException());
                    }
                    return Task.FromResult(Result.Success());
                }

                // Act
                var result =  await PolicyFactory.ExecuteWithPolicyAsync(Action, policy, "TestPolicy");

                // Assert
                result.IsSuccess.ShouldBeTrue();
                executionCount.ShouldBe(retryCount + 1);
            }


            private static bool ShouldRetry(ResultBase result)
            {
                return result switch
                {
                    { Status: ResultStatus.Conflict} => true,
                    _ => false
                };
            }

            private static bool ShouldRetryException(Exception exception)
            {
                return exception switch
                {
                    TaskCanceledException { InnerException: TimeoutException } => true,
                    _ => false
                };
            }


        }
    }
