using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Shared.Extensions;

public static class EndpointConventionBuilderExtensions
{
    public static RouteHandlerBuilder WithStandardProduces<TSuccess, TError>(this RouteHandlerBuilder builder, Int32 successCode = StatusCodes.Status200OK)
    {
        builder.Produces<TSuccess>(successCode)
            .Produces<TError>(StatusCodes.Status400BadRequest)
            .Produces<TError>(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces<TError>(StatusCodes.Status409Conflict)
            .Produces<TError>(StatusCodes.Status500InternalServerError)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces<TError>(StatusCodes.Status501NotImplemented);

        return builder;
    }

    public static RouteHandlerBuilder WithStandardProduces<TError>(this RouteHandlerBuilder builder, Int32 successCode = StatusCodes.Status200OK)
    {
        builder.Produces(successCode)
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