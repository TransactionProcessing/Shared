using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using Shared.EventStore.Aggregate;
using Shared.Results;
using Shouldly;
using SimpleResults;
using Xunit;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Object = System.Object;
using String = System.String;

namespace Shared.Tests
{
    public class ResultHelpersTests {
        [Fact]
        public void CreateFailedResult_ResultCreated() {
            var test = new { Id = "EA9C81FC-C522-4C70-AEAF-661B0C585D24" };

            var result = ResultHelpers.CreateFailedResult(test);

            result.IsFailed.ShouldBeTrue();
            result.Data.Id.ShouldBe(test.Id);
        }

        [Fact]
        public void UnknownFailure() {
            Result result = Result.Success();

            Should.Throw<InvalidDataException>(() => ResultHelpers.CreateFailure(result));
        }

        [Fact]
        public void UnknownFailure_Generic()
        {
            Result<Object> result = Result.Success<System.Object>(new Object());

            Should.Throw<InvalidDataException>(() => ResultHelpers.CreateFailure(result));
        }

        [Theory]

        [InlineData(ResultStatus.Invalid, "message", -1)]
        [InlineData(ResultStatus.Invalid, "", 1)]
        [InlineData(ResultStatus.Invalid, null, 1)]
        [InlineData(ResultStatus.Invalid, null, 0)]

        [InlineData(ResultStatus.NotFound, "message", -1)]
        [InlineData(ResultStatus.NotFound, "", 1)]
        [InlineData(ResultStatus.NotFound, null, 1)]
        [InlineData(ResultStatus.NotFound, null, 0)]

        [InlineData(ResultStatus.Unauthorized, "message", -1)]
        [InlineData(ResultStatus.Unauthorized, "", 1)]
        [InlineData(ResultStatus.Unauthorized, null, 1)]
        [InlineData(ResultStatus.Unauthorized, null, 0)]

        [InlineData(ResultStatus.Conflict, "message", -1)]
        [InlineData(ResultStatus.Conflict, "", 1)]
        [InlineData(ResultStatus.Conflict, null, 1)]
        [InlineData(ResultStatus.Conflict, null, 0)]

        [InlineData(ResultStatus.Failure, "message", -1)]
        [InlineData(ResultStatus.Failure, "", 1)]
        [InlineData(ResultStatus.Failure, null, 1)]
        [InlineData(ResultStatus.Failure, null, 0)]

        [InlineData(ResultStatus.CriticalError, "message", -1)]
        [InlineData(ResultStatus.CriticalError, "", 1)]
        [InlineData(ResultStatus.CriticalError, null, 1)]
        [InlineData(ResultStatus.CriticalError, null, 0)]

        [InlineData(ResultStatus.Forbidden, "message", -1)]
        [InlineData(ResultStatus.Forbidden, "", 1)]
        [InlineData(ResultStatus.Forbidden, null, 1)]
        [InlineData(ResultStatus.Forbidden, null, 0)]
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
        [InlineData(ResultStatus.Ok, null,0)]
        [InlineData(ResultStatus.Invalid, "message", -1)]
        [InlineData(ResultStatus.Invalid, "", 1)]
        [InlineData(ResultStatus.Invalid, null, 1)]
        [InlineData(ResultStatus.Invalid, null, 0)]

        [InlineData(ResultStatus.NotFound, "message", -1)]
        [InlineData(ResultStatus.NotFound, "", 1)]
        [InlineData(ResultStatus.NotFound, null, 1)]
        [InlineData(ResultStatus.NotFound, null, 0)]

        [InlineData(ResultStatus.Unauthorized, "message", -1)]
        [InlineData(ResultStatus.Unauthorized, "", 1)]
        [InlineData(ResultStatus.Unauthorized, null, 1)]
        [InlineData(ResultStatus.Unauthorized, null, 0)]

        [InlineData(ResultStatus.Conflict, "message", -1)]
        [InlineData(ResultStatus.Conflict, "", 1)]
        [InlineData(ResultStatus.Conflict, null, 1)]
        [InlineData(ResultStatus.Conflict, null, 0)]

        [InlineData(ResultStatus.Failure, "message", -1)]
        [InlineData(ResultStatus.Failure, "", 1)]
        [InlineData(ResultStatus.Failure, null, 1)]
        [InlineData(ResultStatus.Failure, null, 0)]

        [InlineData(ResultStatus.CriticalError, "message", -1)]
        [InlineData(ResultStatus.CriticalError, "", 1)]
        [InlineData(ResultStatus.CriticalError, null, 1)]
        [InlineData(ResultStatus.CriticalError, null, 0)]

        [InlineData(ResultStatus.Forbidden, "message", -1)]
        [InlineData(ResultStatus.Forbidden, "", 1)]
        [InlineData(ResultStatus.Forbidden, null, 1)]
        [InlineData(ResultStatus.Forbidden, null, 0)]
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

            if (result.Status == ResultStatus.Ok) {
                newresult.Status.ShouldBe(ResultStatus.Failure);
            }
            else {
                newresult.Status.ShouldBe(result.Status);
                if (String.IsNullOrEmpty(message) == false) {
                    newresult.Message.ShouldBe(message);
                }
                else {
                    newresult.Errors.Count().ShouldBe(numberErrors);
                }
            }
        }
        private Result CreateTestResult(ResultStatus status,
                                        String message,
                                        List<String> errors) {
            if (status == ResultStatus.Ok) {
                return new Result {
                    IsSuccess = false,
                    Status = status
                };
            }

            if (String.IsNullOrEmpty(message) == false) {
                return status switch {
                    ResultStatus.Invalid => Result.Invalid(message),
                    ResultStatus.NotFound => Result.NotFound(message),
                    ResultStatus.Unauthorized => Result.Unauthorized(message),
                    ResultStatus.Conflict => Result.Conflict(message),
                    ResultStatus.Failure => Result.Failure(message),
                    ResultStatus.CriticalError => Result.CriticalError(message),
                    ResultStatus.Forbidden => Result.Forbidden(message),
                };
            }
            else {
                return status switch {
                    ResultStatus.Invalid => Result.Invalid(errors),
                    ResultStatus.NotFound => Result.NotFound(errors),
                    ResultStatus.Unauthorized => Result.Unauthorized(errors),
                    ResultStatus.Conflict => Result.Conflict(errors),
                    ResultStatus.Failure => Result.Failure(errors),
                    ResultStatus.CriticalError => Result.CriticalError(errors),
                    ResultStatus.Forbidden => Result.Forbidden(errors)
                };
            }
        }
    }
}
