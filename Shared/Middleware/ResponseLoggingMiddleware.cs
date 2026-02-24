using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Shared.General;

namespace Shared.Middleware;

    using Microsoft.AspNetCore.Http.Extensions;

    public class ResponseLoggingMiddleware
    {
        private readonly RequestDelegate next;

        public ResponseLoggingMiddleware(RequestDelegate next)
        {
            this.next = next;
        }

        public async Task Invoke(HttpContext context, RequestResponseMiddlewareLoggingConfig configuration)
        {
            if (!configuration.LogResponses)
            {
                await next(context);
            }
            else
            {
                var url = context.Request.GetDisplayUrl();
                var bodyStream = context.Response.Body;

                var responseBodyStream = new ResponseLoggingMemoryStream();
                context.Response.Body = responseBodyStream;

                await next(context);

                responseBodyStream.Seek(0, SeekOrigin.Begin);
                var responseBody = await new StreamReader(responseBodyStream).ReadToEndAsync();
                StringBuilder logMessage = new();
                logMessage.Append($"Response: Status Code: {context.Response.StatusCode}");

                // Append response headers. Redact sensitive headers like Set-Cookie and Authorization.
                if (context.Response.Headers != null && context.Response.Headers.Count > 0)
                {
                    logMessage.Append(' ');
                    logMessage.Append("Headers:");
                    var firstHeader = true;
                    foreach (var header in context.Response.Headers)
                    {
                        if (!firstHeader)
                            logMessage.Append(',');
                        firstHeader = false;

                        var value = header.Value.ToString();
                        if (string.Equals(header.Key, "Set-Cookie", StringComparison.OrdinalIgnoreCase)
                            || string.Equals(header.Key, "Authorization", StringComparison.OrdinalIgnoreCase)
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

                if (!String.IsNullOrEmpty(responseBody))
                {
                    logMessage.Append(' ');
                    logMessage.Append($"Body: {responseBody}");
                }

                Helpers.LogMessage(url, logMessage, configuration.LoggingLevel);

                responseBodyStream.Seek(0, SeekOrigin.Begin);
                await responseBodyStream.CopyToAsync(bodyStream);

                if (responseBodyStream.IsDisposed() && context.Request.Headers.ContainsKey("SOAPAction"))
                {
                    responseBodyStream.ForceClose();
                }
            }
        }
    }

    [ExcludeFromCodeCoverage]
    public class ResponseLoggingMemoryStream : MemoryStream
    {
        public override void Close()
        {
            // Dont close by default
        }

        public void ForceClose()
        {
            base.Close();
        }

        public bool IsDisposed()
        {
            return this.CanRead && this.CanSeek && this.CanWrite;
        }

    }