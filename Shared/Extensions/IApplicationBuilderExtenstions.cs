using Shared.Middleware;
using System.Text;

namespace Shared.Extensions;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

public static class IApplicationBuilderExtenstions
{
    #region public static void AddExceptionHandler(this IApplicationBuilder applicationBuilder)        
    /// <summary>
    /// Adds the exception handler.
    /// </summary>
    /// <param name="applicationBuilder">The application builder.</param>
    public static void AddExceptionHandler(this IApplicationBuilder applicationBuilder)
    {
        applicationBuilder.UseMiddleware<ExceptionHandlerMiddleware>();
    }
    #endregion

    #region public static void AddRequestLogging(this IApplicationBuilder applicationBuilder)        
    /// <summary>
    /// Adds the request logging.
    /// </summary>
    /// <param name="applicationBuilder">The application builder.</param>
    public static void AddRequestLogging(this IApplicationBuilder applicationBuilder)
    {
        applicationBuilder.UseMiddleware<RequestLoggingMiddleware>();
    }
    #endregion

    #region public static void AddResponseLogging(this IApplicationBuilder applicationBuilder)        
    /// <summary>
    /// Adds the response logging.
    /// </summary>
    /// <param name="applicationBuilder">The application builder.</param>
    public static void AddResponseLogging(this IApplicationBuilder applicationBuilder)
    {
        applicationBuilder.UseMiddleware<ResponseLoggingMiddleware>();
    }
    #endregion

    #region public static void AddRequestResponseLogging(this IApplicationBuilder applicationBuilder)        
    /// <summary>
    /// Adds combined request and response logging middleware. Non-2xx responses are always logged at Warning level.
    /// </summary>
    /// <param name="applicationBuilder">The application builder.</param>
    public static void AddRequestResponseLogging(this IApplicationBuilder applicationBuilder)
    {
        applicationBuilder.UseMiddleware<RequestResponseLoggingMiddleware>();
    }
    #endregion
}