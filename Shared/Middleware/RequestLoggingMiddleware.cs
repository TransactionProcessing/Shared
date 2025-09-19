using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Shared.General;

namespace Shared.Middleware;

public class RequestLoggingMiddleware
{
    #region Fields
    private readonly RequestDelegate next;
    #endregion

    #region Constructors
    public RequestLoggingMiddleware(RequestDelegate next)
    {
        this.next = next;
    }
    #endregion

    #region public async Task Invoke(HttpContext context)
    public async Task Invoke(HttpContext context, RequestResponseMiddlewareLoggingConfig configuration)
    {
        if (!configuration.LogRequests)
        {
            await next(context);
        }
        else
        {
            var requestBodyStream = new MemoryStream();
            var originalRequestBody = context.Request.Body;

            await context.Request.Body.CopyToAsync(requestBodyStream);
            requestBodyStream.Seek(0, SeekOrigin.Begin);

            var url = context.Request.GetDisplayUrl();
            var requestBodyText = await new StreamReader(requestBodyStream).ReadToEndAsync();
            StringBuilder logMessage = new();
            logMessage.Append($"Request: Method: {context.Request.Method} Url: {url}");
            if (requestBodyText != String.Empty)
            {
                logMessage.Append(" ");
                logMessage.Append($"Body: {requestBodyText}");
            }

            Helpers.LogMessage(url, logMessage, configuration.LoggingLevel);

            requestBodyStream.Seek(0, SeekOrigin.Begin);
            context.Request.Body = requestBodyStream;

            await next(context);
            context.Request.Body = originalRequestBody;
        }
    }
    #endregion
}