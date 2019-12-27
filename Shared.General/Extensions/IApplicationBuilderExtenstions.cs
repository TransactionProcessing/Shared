﻿namespace Shared.General.Extensions
{
    using Microsoft.AspNetCore.Builder;
    using Middleware;

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
    }
}
