using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace TransactionProcessing.UnionResults.AspNetCore;

/// <summary>
/// Adds OpenAPI response metadata for endpoints that return UnionResults values.
/// </summary>
public static class UnionResultOpenApiExtensions
{
    private static readonly int[] DefaultProblemStatusCodes =
    [
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden,
        StatusCodes.Status404NotFound,
        StatusCodes.Status409Conflict,
        StatusCodes.Status500InternalServerError,
    ];

    /// <summary>
    /// Adds success and default Problem Details responses for a non-generic <see cref="Result"/> endpoint.
    /// </summary>
    /// <param name="endpoint">The endpoint to describe.</param>
    /// <param name="additionalProblemStatusCodes">Additional Problem Details status codes used by application-specific mappings.</param>
    /// <returns>The same endpoint builder.</returns>
    public static RouteHandlerBuilder ProducesUnionResult(
        this RouteHandlerBuilder endpoint,
        params int[] additionalProblemStatusCodes)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentNullException.ThrowIfNull(additionalProblemStatusCodes);

        endpoint.Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status204NoContent);

        endpoint.AddOpenApiOperationTransformer(static (operation, _, _) =>
        {
            if (operation.Responses?.TryGetValue("200", out var ok) == true) ok.Content?.Clear();
            if (operation.Responses?.TryGetValue("204", out var noContent) == true) noContent.Content?.Clear();
            return Task.CompletedTask;
        });

        return AddProblemResponses(endpoint, additionalProblemStatusCodes);
    }

    /// <summary>
    /// Adds success and default Problem Details responses for a generic <see cref="Result{T}"/> endpoint.
    /// </summary>
    /// <typeparam name="T">The successful response body type.</typeparam>
    /// <param name="endpoint">The endpoint to describe.</param>
    /// <param name="additionalProblemStatusCodes">Additional Problem Details status codes used by application-specific mappings.</param>
    /// <returns>The same endpoint builder.</returns>
    public static RouteHandlerBuilder ProducesUnionResult<T>(
        this RouteHandlerBuilder endpoint,
        params int[] additionalProblemStatusCodes)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentNullException.ThrowIfNull(additionalProblemStatusCodes);

        endpoint.Produces<T>(StatusCodes.Status200OK)
            .Produces<T>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status204NoContent);

        endpoint.AddOpenApiOperationTransformer(async (operation, context, cancellationToken) =>
        {
            var schema = await context.GetOrCreateSchemaAsync(typeof(T), null, cancellationToken).ConfigureAwait(false);
            if (operation.Responses?.TryGetValue("200", out var ok) == true && ok.Content?.ContainsKey("application/json") == true)
                ok.Content["application/json"] = new OpenApiMediaType { Schema = schema };
            if (operation.Responses?.TryGetValue("201", out var created) == true && created.Content?.ContainsKey("application/json") == true)
                created.Content["application/json"] = new OpenApiMediaType { Schema = schema };
            if (operation.Responses?.TryGetValue("204", out var noContent) == true) noContent.Content?.Clear();
        });

        return AddProblemResponses(endpoint, additionalProblemStatusCodes);
    }

    private static RouteHandlerBuilder AddProblemResponses(
        RouteHandlerBuilder endpoint,
        IEnumerable<int> additionalProblemStatusCodes)
    {
        foreach (var statusCode in DefaultProblemStatusCodes.Concat(additionalProblemStatusCodes).Distinct())
        {
            if (statusCode is < 400 or > 599)
            {
                throw new ArgumentOutOfRangeException(nameof(additionalProblemStatusCodes), statusCode, "Problem Details status codes must be between 400 and 599.");
            }

            endpoint.ProducesProblem(statusCode);
        }

        return endpoint;
    }
}
