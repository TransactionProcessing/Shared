using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Shared.Extensions;

public static class EndpointConventionBuilderExtensions
{
    public static RouteHandlerBuilder WithStandardProduces<TSuccess, TError>(this RouteHandlerBuilder builder)
    {
        builder.Produces<TSuccess>(StatusCodes.Status200OK)
            .Produces<TError>(StatusCodes.Status400BadRequest)
            .Produces<TError>(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<TError>(StatusCodes.Status409Conflict)
            .Produces<TError>(StatusCodes.Status500InternalServerError)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces<TError>(StatusCodes.Status501NotImplemented);

        return builder;
    }
}