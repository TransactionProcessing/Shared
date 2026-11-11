using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TransactionProcessing.UnionResults;

namespace TransactionProcessing.UnionResults.AspNetCore;

internal sealed class UnionResultEndpointFilter(IOptions<UnionResultOptions> options) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var result = await next(context).ConfigureAwait(false);
        if (result is not IResultOutcome outcome) return result;

        var response = outcome.GetOutcome();
        var http = context.HttpContext;
        switch (response.Kind)
        {
            case ResultKind.Ok:
                if (response.Value is null) { http.Response.StatusCode = StatusCodes.Status200OK; return Results.Empty; }
                return Results.Ok(response.Value);
            case ResultKind.Created:
                if (response.Value is null) throw new InvalidOperationException("A Created result cannot contain a null value.");
                if (response.Location is not null && !Uri.TryCreate(response.Location, UriKind.RelativeOrAbsolute, out _))
                    throw new InvalidOperationException("Created Location must be a valid URI.");
                return Results.Created(response.Location, response.Value);
            case ResultKind.NoContent:
                http.Response.StatusCode = StatusCodes.Status204NoContent;
                return Results.Empty;
            case ResultKind.Error:
                return WriteProblem(http, options.Value, response.Error!, [response.Error!]);
            case ResultKind.ErrorList:
                return WriteProblem(http, options.Value, null, response.Errors!);
            default:
                throw new InvalidOperationException("Unknown union result kind.");
        }
    }

    private static IResult WriteProblem(HttpContext context, UnionResultOptions options, Error? error, IReadOnlyList<Error> errors)
    {
        var status = error is null ? options.Resolve(new ErrorList(errors)) : options.Resolve(error);
        var title = status >= 500 ? "An unexpected error occurred." : error?.Message ?? errors[0].Message;
        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Type = $"https://httpstatuses.com/{status}",
            Instance = context.Request.Path
        };
        if (options.IncludeErrorCodes)
        {
            if (error is not null) problem.Extensions["code"] = error.Code;
            else problem.Extensions["codes"] = errors.Select(static item => item.Code).ToArray();
        }
        if (options.IncludeTraceId) problem.Extensions["traceId"] = context.TraceIdentifier;
        if (status < 500 && errors.Any(static item => item.ErrorType == ErrorType.Validation))
        {
            problem.Extensions["errors"] = errors.Where(static item => item.ErrorType == ErrorType.Validation)
                .GroupBy(static item => item.Field ?? string.Empty)
                .ToDictionary(static group => group.Key, static group => group.Select(static item => item.Message).ToArray());
        }
        if (!options.IncludeTraceId)
            return Results.Json(problem, statusCode: status, contentType: "application/problem+json");

        return Results.Problem(problem.Detail, problem.Instance, problem.Status, problem.Title, problem.Type, extensions: problem.Extensions);
    }
}
