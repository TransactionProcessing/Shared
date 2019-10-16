using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Newtonsoft.Json;
using Shared.Exceptions;
using Shared.General;

namespace Shared.Middleware
{
    public class ExceptionHandlerMiddleware
    {
        #region Fields

        /// <summary>
        /// The next method
        /// </summary>
        private readonly RequestDelegate next;

        #endregion
        
        #region Constructors        
        /// <summary>
        /// Initializes a new instance of the <see cref="ExceptionHandlerMiddleware"/> class.
        /// </summary>
        /// <param name="next">The next.</param>
        public ExceptionHandlerMiddleware(RequestDelegate next)
        {
            this.next = next;
        }
        #endregion

        #region Public Methods        
        
        #region public async Task Invoke(HttpContext context)
        /// <summary>
        /// Invokes the specified context.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        public async Task Invoke(HttpContext context)
        {
            try
            {
                await next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }
        #endregion

        #endregion

        #region Private Methods

        #region private async Task HandleExceptionAsync(HttpContext context, Exception exception)        
        /// <summary>
        /// Handles the exception asynchronous.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="exception">The exception.</param>
        /// <returns></returns>
        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            Exception newException =
                new Exception(
                    $"An unhandled exception has occurred while executing the request. Url: {context.Request.GetDisplayUrl()}",
                    exception);
            Logger.LogError(newException);

            // Set some defaults
            var response = context.Response;
            var statusCode = HttpStatusCode.InternalServerError;
            var message = "Unexpected error";

            if (exception is ArgumentException ||
                exception is InvalidOperationException ||
                exception is InvalidDataException ||
                exception is FormatException ||
                exception is NotSupportedException)
            {
                statusCode = HttpStatusCode.BadRequest;
                message = exception.Message;
            }
            else if (exception is NotFoundException)
            {
                statusCode = HttpStatusCode.NotFound;
                message = exception.Message;
            }
            else if (exception is NotImplementedException)
            {
                statusCode = HttpStatusCode.NotImplemented;
                message = exception.Message;
            }

            response.ContentType = context.Request.ContentType;
            response.StatusCode = (Int32) statusCode;

            await response.WriteAsync(JsonConvert.SerializeObject(new ErrorResponse(message)));
        }
        #endregion

        #endregion
    }
}
