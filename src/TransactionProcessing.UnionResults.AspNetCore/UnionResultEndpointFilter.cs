using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using HttpResults = Microsoft.AspNetCore.Http.Results;

namespace TransactionProcessing.UnionResults.AspNetCore;

internal sealed class UnionResultEndpointFilter(IOptions<UnionResultOptions> options) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var result = await next(context).ConfigureAwait(false);
        if (result is not IUnion union || !IsResultType(result.GetType()))
        {
            return result;
        }

        var http = context.HttpContext;
        return union.Value switch
        {
            Success or IResultSuccess { Value: null } => HttpResults.Ok(),
            IResultSuccess success => HttpResults.Ok(success.Value),
            Error error => WriteProblem(http, options.Value.Resolve(error.ErrorType), error.ErrorType, [error]),
            ErrorList errors => WriteProblem(http, options.Value.Resolve(errors.ErrorType), errors.ErrorType, errors),
            _ => WriteProblem(http, StatusCodes.Status500InternalServerError, ErrorType.Critical, [])
        };
    }

    private static bool IsResultType(Type type) =>
        type == typeof(Result) || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Result<>));

    private IResult WriteProblem(HttpContext context, int status, ErrorType category, IReadOnlyList<Error> errors)
    {
        var problem = new ProblemDetails
        {
            Status = status,
            Title = status >= StatusCodes.Status500InternalServerError
                ? "An unexpected error occurred."
                : errors[0].Message,
            Type = $"https://httpstatuses.com/{status}",
            Instance = context.Request.Path
        };

        if (options.Value.IncludeTraceId)
        {
            problem.Extensions["traceId"] = context.TraceIdentifier;
        }

        if (status < StatusCodes.Status500InternalServerError && category == ErrorType.Validation)
        {
            problem.Extensions["errors"] = new Dictionary<string, string[]>
            {
                [string.Empty] = errors.Select(static error => error.Message).ToArray()
            };
        }

        return HttpResults.Json(
            problem,
            UnionResultJsonSerializerContext.Default.ProblemDetails,
            "application/problem+json",
            status);
    }
}

[JsonSerializable(typeof(ProblemDetails))]
[JsonSerializable(typeof(Dictionary<string, string[]>))]
internal partial class UnionResultJsonSerializerContext : JsonSerializerContext
{
}
