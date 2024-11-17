using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Shared.EventStore.Aggregate;
using Shared.Results;
using Shouldly;
using SimpleResults;
using Xunit;

namespace Shared.Tests
{
    public class ResultHelpersTests {
        [Theory]
        [InlineData(ResultStatus.Invalid, "message", -1)]
        [InlineData(ResultStatus.Invalid, "", 1)]
        [InlineData(ResultStatus.Invalid, null, 1)]

        [InlineData(ResultStatus.NotFound, "message", -1)]
        [InlineData(ResultStatus.NotFound, "", 1)]
        [InlineData(ResultStatus.NotFound, null, 1)]

        [InlineData(ResultStatus.Unauthorized, "message", -1)]
        [InlineData(ResultStatus.Unauthorized, "", 1)]
        [InlineData(ResultStatus.Unauthorized, null, 1)]

        [InlineData(ResultStatus.Conflict, "message", -1)]
        [InlineData(ResultStatus.Conflict, "", 1)]
        [InlineData(ResultStatus.Conflict, null, 1)]

        [InlineData(ResultStatus.Failure, "message", -1)]
        [InlineData(ResultStatus.Failure, "", 1)]
        [InlineData(ResultStatus.Failure, null, 1)]

        [InlineData(ResultStatus.CriticalError, "message", -1)]
        [InlineData(ResultStatus.CriticalError, "", 1)]
        [InlineData(ResultStatus.CriticalError, null, 1)]

        [InlineData(ResultStatus.Forbidden, "message", -1)]
        [InlineData(ResultStatus.Forbidden, "", 1)]
        [InlineData(ResultStatus.Forbidden, null, 1)]
        public void ResultHelpers_CreateFailure_NonGeneric_OutputMatchesInput(ResultStatus status,
                                                String message,
                                                Int32 numberErrors) {
            // Create the result 
            List<String> errors = numberErrors switch {
                < 0 => null,
                0 => new List<String>(),
                > 0 => ["message1"]
            };
            Result result = CreateTestResult(status, message, errors);

            Result newresult = ResultHelpers.CreateFailure(result);

            newresult.Status.ShouldBe(result.Status);
            if (String.IsNullOrEmpty(message) == false) {
                newresult.Message.ShouldBe(message);
            }
            else {
                newresult.Errors.Count().ShouldBe(numberErrors);
            }
        }

        [Theory]
        [InlineData(ResultStatus.Invalid, "message", -1)]
        [InlineData(ResultStatus.Invalid, "", 1)]
        [InlineData(ResultStatus.Invalid, null, 1)]

        [InlineData(ResultStatus.NotFound, "message", -1)]
        [InlineData(ResultStatus.NotFound, "", 1)]
        [InlineData(ResultStatus.NotFound, null, 1)]

        [InlineData(ResultStatus.Unauthorized, "message", -1)]
        [InlineData(ResultStatus.Unauthorized, "", 1)]
        [InlineData(ResultStatus.Unauthorized, null, 1)]

        [InlineData(ResultStatus.Conflict, "message", -1)]
        [InlineData(ResultStatus.Conflict, "", 1)]
        [InlineData(ResultStatus.Conflict, null, 1)]

        [InlineData(ResultStatus.Failure, "message", -1)]
        [InlineData(ResultStatus.Failure, "", 1)]
        [InlineData(ResultStatus.Failure, null, 1)]

        [InlineData(ResultStatus.CriticalError, "message", -1)]
        [InlineData(ResultStatus.CriticalError, "", 1)]
        [InlineData(ResultStatus.CriticalError, null, 1)]

        [InlineData(ResultStatus.Forbidden, "message", -1)]
        [InlineData(ResultStatus.Forbidden, "", 1)]
        [InlineData(ResultStatus.Forbidden, null, 1)]
        public void ResultHelpers_CreateFailure_Generic_OutputMatchesInput(ResultStatus status,
                                       String message,
                                       Int32 numberErrors) {
            // Create the result 
            List<String> errors = numberErrors switch {
                < 0 => null,
                0 => new List<String>(),
                > 0 => ["message1"]
            };
            Result<String> result = CreateTestResult(status,message, errors);

            Result newresult = ResultHelpers.CreateFailure(result);

            newresult.Status.ShouldBe(result.Status);
            if (String.IsNullOrEmpty(message) == false) {
                newresult.Message.ShouldBe(message);
            }
            else {
                newresult.Errors.Count().ShouldBe(numberErrors);
            }
        }
        private Result CreateTestResult(ResultStatus status,
                                        String message,
                                        List<String> errors) {
            if (String.IsNullOrEmpty(message) == false) {
                return status switch {
                    ResultStatus.Invalid => Result.Invalid(message),
                    ResultStatus.NotFound => Result.Invalid(message),
                    ResultStatus.Unauthorized => Result.Invalid(message),
                    ResultStatus.Conflict => Result.Invalid(message),
                    ResultStatus.Failure => Result.Failure(message),
                    ResultStatus.CriticalError => Result.Invalid(message),
                    ResultStatus.Forbidden => Result.Invalid(message),
                };
            }
            else {
                return status switch {
                    ResultStatus.Invalid => Result.Invalid(errors),
                    ResultStatus.NotFound => Result.Invalid(errors),
                    ResultStatus.Unauthorized => Result.Invalid(errors),
                    ResultStatus.Conflict => Result.Invalid(errors),
                    ResultStatus.Failure => Result.Failure(errors),
                    ResultStatus.CriticalError => Result.Invalid(errors),
                    ResultStatus.Forbidden => Result.Invalid(errors)
                };
            }
        }
    }
}
