using System.Net;
using Microsoft.AspNetCore.Http;
using SimpleResults;

namespace Shared.Results.Web;

public static class ResponseFactory
{
    public static IResult FromResult<T>(Result<T> result,
                                        Func<T, object> successFactory)
    {
        if (result.IsSuccess) {
            Object response = successFactory(result.Data!);
            return Microsoft.AspNetCore.Http.Results.Ok(response);
        }

        return TranslateResultStatus(result);

    }

    internal static IResult TranslateResultStatus(ResultBase result)
    {
        ErrorResponse errorResponse = new()
        {
            Errors = result.Errors.Any() switch
            {
                true => result.Errors.ToList(),
                _ => [result.Message],
            }
        };

        return result.Status switch
        {
            ResultStatus.Invalid => Microsoft.AspNetCore.Http.Results.BadRequest(errorResponse),
            ResultStatus.NotFound => Microsoft.AspNetCore.Http.Results.NotFound(errorResponse),
            ResultStatus.Unauthorized => Microsoft.AspNetCore.Http.Results.Unauthorized(),
            ResultStatus.Conflict => Microsoft.AspNetCore.Http.Results.Conflict(errorResponse),
            ResultStatus.Failure => Microsoft.AspNetCore.Http.Results.InternalServerError(errorResponse),
            ResultStatus.CriticalError => Microsoft.AspNetCore.Http.Results.InternalServerError(errorResponse),
            ResultStatus.Forbidden => Microsoft.AspNetCore.Http.Results.Forbid(),
            _ => Microsoft.AspNetCore.Http.Results.StatusCode((int)HttpStatusCode.NotImplemented)
        };
    }

    public static IResult FromResult(Result result)
    {
        if (result.IsSuccess)
            return Microsoft.AspNetCore.Http.Results.StatusCode(StatusCodes.Status204NoContent);

        return TranslateResultStatus(result);
    }
}