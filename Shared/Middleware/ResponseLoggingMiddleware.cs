using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Shared.General;

namespace Shared.Middleware
{
    using Microsoft.AspNetCore.Http.Extensions;

    public class ResponseLoggingMiddleware
    {
        #region Fields

        private readonly RequestDelegate next;

        #endregion

        #region Constructors
        public ResponseLoggingMiddleware(RequestDelegate next)
        {
            this.next = next;
        }
        #endregion

        #region Public Methods

        #region public async Task Invoke(HttpContext context)        
        public async Task Invoke(HttpContext context, RequestResponseMiddlewareLoggingConfig configuration)
        {
            var url = context.Request.GetDisplayUrl();
            var bodyStream = context.Response.Body;

            var responseBodyStream = new MemoryStream();
            context.Response.Body = responseBodyStream;

            await next(context);

            if (configuration.LogResponses)
            {
                responseBodyStream.Seek(0, SeekOrigin.Begin);
                var responseBody = await new StreamReader(responseBodyStream).ReadToEndAsync();
                StringBuilder logMessage = new StringBuilder();
                logMessage.Append($"Response: Status Code: {context.Response.StatusCode}");
                if (!String.IsNullOrEmpty(responseBody))
                {
                    logMessage.Append(" ");
                    logMessage.Append($"Body: {responseBody}");
                }

                Helpers.LogMessage(url, logMessage, configuration.LoggingLevel);

                responseBodyStream.Seek(0, SeekOrigin.Begin);
            }

            await responseBodyStream.CopyToAsync(bodyStream);
        }
        #endregion

        #endregion
    }
}