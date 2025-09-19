using System;
using Microsoft.AspNetCore.Mvc;
using Shared.Results;
using Shared.Results.Web;
using Shouldly;
using SimpleResults;
using Xunit;

namespace Shared.Tests;

public class ResultExtensionsTests {

    [Theory]
    [InlineData(ResultStatus.Ok, typeof(OkObjectResult))]
    [InlineData(ResultStatus.Invalid, typeof(BadRequestObjectResult))]
    [InlineData(ResultStatus.NotFound, typeof(NotFoundObjectResult))]
    [InlineData(ResultStatus.Unauthorized, typeof(UnauthorizedObjectResult))]
    [InlineData(ResultStatus.Conflict, typeof(ConflictObjectResult))]
    [InlineData(ResultStatus.Failure, typeof(ObjectResult))]
    [InlineData(ResultStatus.CriticalError, typeof(ObjectResult))]
    [InlineData(ResultStatus.Forbidden, typeof(ForbidResult))]
    [InlineData((ResultStatus)99, typeof(ObjectResult))]
    public void ToActionResultX_ActionResultIsReturned(ResultStatus status, Type expectedType) {
        Result r = new() {
            Status = status,
            IsSuccess = status == ResultStatus.Ok
        };
            
        var actionResult = r.ToActionResultX();
        actionResult.ShouldBeOfType(expectedType);

        if (expectedType == typeof(ObjectResult))
        {
            if (status is ResultStatus.Failure or ResultStatus.CriticalError)
            {
                ((ObjectResult)actionResult).StatusCode.ShouldBe(500);
            }
            if (status == (ResultStatus)99)
            {
                ((ObjectResult)actionResult).StatusCode.ShouldBe(501);
            }
        }
    }

    [Theory]
    [InlineData(ResultStatus.Ok, typeof(OkObjectResult))]
    [InlineData(ResultStatus.Invalid, typeof(BadRequestObjectResult))]
    [InlineData(ResultStatus.NotFound, typeof(NotFoundObjectResult))]
    [InlineData(ResultStatus.Unauthorized, typeof(UnauthorizedObjectResult))]
    [InlineData(ResultStatus.Conflict, typeof(ConflictObjectResult))]
    [InlineData(ResultStatus.Failure, typeof(ObjectResult))]
    [InlineData(ResultStatus.CriticalError, typeof(ObjectResult))]
    [InlineData(ResultStatus.Forbidden, typeof(ForbidResult))]
    [InlineData((ResultStatus)99, typeof(ObjectResult))]
    public void ToActionResultX_Generic_ActionResultIsReturned(ResultStatus status, Type expectedType)
    {
        Result<Guid> r = new Result<Guid> {
            Status = status,
            Data = Guid.NewGuid(),
            IsSuccess = status == ResultStatus.Ok

        };

        IActionResult actionResult = r.ToActionResultX();
        actionResult.ShouldBeOfType(expectedType);

        if (expectedType == typeof(ObjectResult)) {
            if (status is ResultStatus.Failure or ResultStatus.CriticalError) {
                ((ObjectResult)actionResult).StatusCode.ShouldBe(500);
            }
            if (status == (ResultStatus) 99)
            {
                ((ObjectResult)actionResult).StatusCode.ShouldBe(501);
            }
        }
    }
}