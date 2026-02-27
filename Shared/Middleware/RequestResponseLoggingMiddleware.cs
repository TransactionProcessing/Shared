using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Primitives;

namespace Shared.Middleware;

public class RequestResponseLoggingMiddleware {
    private readonly RequestDelegate next;

    public RequestResponseLoggingMiddleware(RequestDelegate next) {
        this.next = next;
    }

    public async Task Invoke(HttpContext context,
                             RequestResponseMiddlewareLoggingConfig configuration) {
        String url = context.Request.GetDisplayUrl();

        // --- Request Logging ---
        String requestBodyText = String.Empty;
        MemoryStream requestBodyStream = null;
        Stream originalRequestBody = context.Request.Body;

        if (configuration.LogRequests) {
            requestBodyStream = new MemoryStream();
            await context.Request.Body.CopyToAsync(requestBodyStream);
            requestBodyStream.Seek(0, SeekOrigin.Begin);
            requestBodyText = await new StreamReader(requestBodyStream).ReadToEndAsync();

            requestBodyStream.Seek(0, SeekOrigin.Begin);
            context.Request.Body = requestBodyStream;
        }

        // --- Intercept Response ---
        Stream originalResponseBody = context.Response.Body;
        ResponseLoggingMemoryStream responseBodyStream = null;

        if (configuration.LogResponses) {
            responseBodyStream = new ResponseLoggingMemoryStream();
            context.Response.Body = responseBodyStream;
        }

        await this.next(context);

        Boolean isNonSuccess = context.Response.StatusCode < 200 || context.Response.StatusCode > 299;
        LogLevel effectiveLogLevel = isNonSuccess ? LogLevel.Warning : configuration.LoggingLevel;

        // --- Log Request ---
        if (configuration.LogRequests) {
            StringBuilder requestLog = new();
            requestLog.Append($"Request: Method: {context.Request.Method} Url: {url}");

            if (context.Request.Headers != null && context.Request.Headers.Count > 0) {
                requestLog.Append(' ');
                requestLog.Append("Headers:");
                Boolean firstHeader = true;
                foreach (KeyValuePair<String, StringValues> header in context.Request.Headers) {
                    if (!firstHeader)
                        requestLog.Append(',');
                    firstHeader = false;

                    String value = header.Value.ToString();
                    if (string.Equals(header.Key, "Authorization", StringComparison.OrdinalIgnoreCase) || string.Equals(header.Key, "Cookie", StringComparison.OrdinalIgnoreCase)) {
                        value = "***REDACTED***";
                    }

                    requestLog.Append(' ');
                    requestLog.Append(header.Key);
                    requestLog.Append('=');
                    requestLog.Append(value);
                }
            }

            if (requestBodyText != String.Empty) {
                requestLog.Append(' ');
                requestLog.Append($"Body: {requestBodyText}");
            }

            Helpers.LogMessage(url, requestLog, effectiveLogLevel);

            context.Request.Body = originalRequestBody;
        }

        // --- Log Response ---
        if (configuration.LogResponses) {
            responseBodyStream.Seek(0, SeekOrigin.Begin);
            String responseBody = await new StreamReader(responseBodyStream).ReadToEndAsync();

            StringBuilder responseLog = new();
            responseLog.Append($"Response: Status Code: {context.Response.StatusCode}");

            if (context.Response.Headers != null && context.Response.Headers.Count > 0) {
                responseLog.Append(' ');
                responseLog.Append("Headers:");
                var firstHeader = true;
                foreach (KeyValuePair<String, StringValues> header in context.Response.Headers) {
                    if (!firstHeader)
                        responseLog.Append(',');
                    firstHeader = false;

                    String value = header.Value.ToString();
                    if (string.Equals(header.Key, "Set-Cookie", StringComparison.OrdinalIgnoreCase) || string.Equals(header.Key, "Authorization", StringComparison.OrdinalIgnoreCase) || string.Equals(header.Key, "Cookie", StringComparison.OrdinalIgnoreCase)) {
                        value = "***REDACTED***";
                    }

                    responseLog.Append(' ');
                    responseLog.Append(header.Key);
                    responseLog.Append('=');
                    responseLog.Append(value);
                }
            }

            if (!String.IsNullOrEmpty(responseBody)) {
                responseLog.Append(' ');
                responseLog.Append($"Body: {responseBody}");
            }

            Helpers.LogMessage(url, responseLog, effectiveLogLevel);

            responseBodyStream.Seek(0, SeekOrigin.Begin);
            await responseBodyStream.CopyToAsync(originalResponseBody);

            if (responseBodyStream.IsDisposed() && context.Request.Headers.ContainsKey("SOAPAction")) {
                responseBodyStream.ForceClose();
            }
        }
    }
}