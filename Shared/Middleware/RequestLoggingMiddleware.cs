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
    private readonly RequestDelegate next;

    public RequestLoggingMiddleware(RequestDelegate next)
    {
        this.next = next;
    }

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

            // Append request headers. Redact sensitive headers like Authorization and Cookie.
            if (context.Request.Headers != null && context.Request.Headers.Count > 0)
            {
                logMessage.Append(' ');
                logMessage.Append("Headers:");
                var firstHeader = true;
                foreach (var header in context.Request.Headers)
                {
                    if (!firstHeader)
                        logMessage.Append(',');
                    firstHeader = false;

                    var value = header.Value.ToString();
                    if (string.Equals(header.Key, "Authorization", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(header.Key, "Cookie", StringComparison.OrdinalIgnoreCase))
                    {
                        value = "***REDACTED***";
                    }

                    // Format: Key=Value
                    logMessage.Append(' ');
                    logMessage.Append(header.Key);
                    logMessage.Append('=');
                    logMessage.Append(value);
                }
            }

            if (requestBodyText != String.Empty)
            {
                logMessage.Append(' ');
                logMessage.Append($"Body: {requestBodyText}");
            }

            Helpers.LogMessage(url, logMessage, configuration.LoggingLevel);

            requestBodyStream.Seek(0, SeekOrigin.Begin);
            context.Request.Body = requestBodyStream;

            await next(context);
            context.Request.Body = originalRequestBody;
        }
    }
}