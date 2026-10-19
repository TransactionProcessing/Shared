using Asp.Versioning;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace TransactionProcessing.ApiDefaults;

internal sealed record MinimumApiVersionMetadata(ApiVersion Version);

internal sealed class MinimumApiVersionApiDescriptionProvider : Microsoft.AspNetCore.Mvc.ApiExplorer.IApiDescriptionProvider
{
    public int Order => int.MaxValue;

    public void OnProvidersExecuting(Microsoft.AspNetCore.Mvc.ApiExplorer.ApiDescriptionProviderContext context)
    {
    }

    public void OnProvidersExecuted(Microsoft.AspNetCore.Mvc.ApiExplorer.ApiDescriptionProviderContext context)
    {
        for (int index = context.Results.Count - 1; index >= 0; index--)
        {
            Microsoft.AspNetCore.Mvc.ApiExplorer.ApiDescription description = context.Results[index];
            MinimumApiVersionMetadata? minimumVersion = description.ActionDescriptor.EndpointMetadata
                .OfType<MinimumApiVersionMetadata>()
                .LastOrDefault();
            ApiVersionMetadata? versionMetadata = description.ActionDescriptor.EndpointMetadata
                .OfType<ApiVersionMetadata>()
                .LastOrDefault();

            if (minimumVersion is not null && versionMetadata is not null)
            {
                versionMetadata.Deconstruct(out ApiVersionModel apiModel, out ApiVersionModel endpointModel, out string name);
                static bool IsAvailable(ApiVersion version, ApiVersion first) => version.CompareTo(first) >= 0;
                ApiVersionModel filteredEndpointModel = new(
                    endpointModel.DeclaredApiVersions.Where(version => IsAvailable(version, minimumVersion.Version)),
                    endpointModel.SupportedApiVersions.Where(version => IsAvailable(version, minimumVersion.Version)),
                    endpointModel.DeprecatedApiVersions.Where(version => IsAvailable(version, minimumVersion.Version)),
                    [],
                    []);

                if (!filteredEndpointModel.DeclaredApiVersions.Any() &&
                    !filteredEndpointModel.SupportedApiVersions.Any() &&
                    !filteredEndpointModel.DeprecatedApiVersions.Any())
                {
                    context.Results.RemoveAt(index);
                    continue;
                }

                int metadataIndex = description.ActionDescriptor.EndpointMetadata.IndexOf(versionMetadata);
                description.ActionDescriptor.EndpointMetadata[metadataIndex] = new ApiVersionMetadata(apiModel, filteredEndpointModel, name);
            }
        }
    }
}

/// <summary>
/// Maps GET endpoints that inherit the newest handler declared for an earlier API version.
/// </summary>
public static class VersionFallbackEndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps a GET endpoint that uses the handler for the highest declared API version less than
    /// or equal to the requested version. The endpoint inherits the API versions of its route group.
    /// </summary>
    /// <param name="endpoints">The versioned route group.</param>
    /// <param name="pattern">The route pattern.</param>
    /// <param name="handlers">Handlers keyed by the API version in which they were introduced.</param>
    /// <returns>The mapped endpoint builder.</returns>
    /// <remarks>
    /// A handler registered for version 2 is used for versions 2 and later until a later handler
    /// is registered. Versions earlier than the first handler return 404.
    /// </remarks>
    public static RouteHandlerBuilder MapGetWithVersionFallback(
        this IEndpointRouteBuilder endpoints,
        string pattern,
        IReadOnlyDictionary<ApiVersion, Func<HttpContext, IResult>> handlers)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);
        ArgumentNullException.ThrowIfNull(handlers);

        SortedDictionary<ApiVersion, Func<HttpContext, IResult>> orderedHandlers = CreateOrderedHandlers(handlers);

        RouteHandlerBuilder endpoint = endpoints.MapGet(pattern, (HttpContext context) =>
        {
            ApiVersion? requestedVersion = context.ApiVersioningFeature.RequestedApiVersion;
            if (requestedVersion is null)
            {
                return Results.NotFound();
            }

            foreach ((ApiVersion version, Func<HttpContext, IResult> handler) in orderedHandlers.Reverse())
            {
                if (version.CompareTo(requestedVersion) <= 0)
                {
                    return handler(context);
                }
            }

            return Results.NotFound();
        });

        endpoint.WithMetadata(new MinimumApiVersionMetadata(orderedHandlers.Keys.First()));
        return endpoint;
    }

    /// <summary>
    /// Maps an asynchronous GET endpoint that uses the handler for the highest declared API version
    /// less than or equal to the requested version. The endpoint inherits the API versions of its route group.
    /// </summary>
    /// <param name="endpoints">The versioned route group.</param>
    /// <param name="pattern">The route pattern.</param>
    /// <param name="handlers">Handlers keyed by the API version in which they were introduced.</param>
    /// <returns>The mapped endpoint builder.</returns>
    /// <remarks>
    /// A handler registered for version 2 is used for versions 2 and later until a later handler
    /// is registered. Versions earlier than the first handler return 404.
    /// </remarks>
    public static RouteHandlerBuilder MapGetWithVersionFallbackAsync(
        this IEndpointRouteBuilder endpoints,
        string pattern,
        IReadOnlyDictionary<ApiVersion, Func<HttpContext, CancellationToken, Task<IResult>>> handlers)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);
        ArgumentNullException.ThrowIfNull(handlers);

        SortedDictionary<ApiVersion, Func<HttpContext, CancellationToken, Task<IResult>>> orderedHandlers = CreateOrderedHandlers(handlers);

        RouteHandlerBuilder endpoint = endpoints.MapGet(pattern, async (HttpContext context, CancellationToken cancellationToken) =>
        {
            ApiVersion? requestedVersion = context.ApiVersioningFeature.RequestedApiVersion;
            if (requestedVersion is null)
            {
                return Results.NotFound();
            }

            foreach ((ApiVersion version, Func<HttpContext, CancellationToken, Task<IResult>> handler) in orderedHandlers.Reverse())
            {
                if (version.CompareTo(requestedVersion) <= 0)
                {
                    return await handler(context, cancellationToken).ConfigureAwait(false);
                }
            }

            return Results.NotFound();
        });

        endpoint.WithMetadata(new MinimumApiVersionMetadata(orderedHandlers.Keys.First()));
        return endpoint;
    }

    private static SortedDictionary<ApiVersion, THandler> CreateOrderedHandlers<THandler>(
        IReadOnlyDictionary<ApiVersion, THandler> handlers)
        where THandler : Delegate
    {
        if (handlers.Count == 0)
        {
            throw new ArgumentException("At least one versioned handler is required.", nameof(handlers));
        }

        SortedDictionary<ApiVersion, THandler> orderedHandlers = new();
        foreach ((ApiVersion version, THandler handler) in handlers)
        {
            ArgumentNullException.ThrowIfNull(version);
            ArgumentNullException.ThrowIfNull(handler);
            orderedHandlers.Add(version, handler);
        }

        return orderedHandlers;
    }
}

