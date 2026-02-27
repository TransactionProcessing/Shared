using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Middleware;

public class RequestResponseLoggingMiddleware
{
    private static readonly HashSet<String> RequestRedactedHeaders =
        new(StringComparer.OrdinalIgnoreCase) { "Authorization", "Cookie" };

    private static readonly HashSet<String> ResponseRedactedHeaders =
        new(StringComparer.OrdinalIgnoreCase) { "Set-Cookie", "Authorization", "Cookie" };

    private readonly RequestDelegate next;

    public RequestResponseLoggingMiddleware(RequestDelegate next)
    {
        this.next = next;
    }

    public async Task Invoke(HttpContext context, RequestResponseMiddlewareLoggingConfig configuration)
    {
        String url = context.Request.GetDisplayUrl();
        Stream originalRequestBody = context.Request.Body;
        Stream originalResponseBody = context.Response.Body;

        String requestBodyText = await CaptureRequestBodyAsync(context, configuration.LogRequests);
        ResponseLoggingMemoryStream responseBodyStream = SetupResponseCapture(context, configuration.LogResponses);

        await this.next(context);

        LogLevel effectiveLogLevel = (context.Response.StatusCode < 200 || context.Response.StatusCode > 299)
            ? LogLevel.Warning
            : configuration.LoggingLevel;

        if (configuration.LogRequests)
        {
            Helpers.LogMessage(url, BuildRequestLog(context, url, requestBodyText), effectiveLogLevel);
            context.Request.Body = originalRequestBody;
        }

        if (configuration.LogResponses)
        {
            await WriteAndLogResponseAsync(context, url, responseBodyStream, originalResponseBody, effectiveLogLevel);
        }
    }

    private static async Task<String> CaptureRequestBodyAsync(HttpContext context, Boolean logRequests)
    {
        if (!logRequests)
            return String.Empty;

        MemoryStream requestBodyStream = new();
        await context.Request.Body.CopyToAsync(requestBodyStream);
        requestBodyStream.Seek(0, SeekOrigin.Begin);
        String bodyText = await new StreamReader(requestBodyStream).ReadToEndAsync();
        requestBodyStream.Seek(0, SeekOrigin.Begin);
        context.Request.Body = requestBodyStream;
        return bodyText;
    }

    private static ResponseLoggingMemoryStream SetupResponseCapture(HttpContext context, Boolean logResponses)
    {
        if (!logResponses)
            return null;

        ResponseLoggingMemoryStream responseBodyStream = new();
        context.Response.Body = responseBodyStream;
        return responseBodyStream;
    }

    private static StringBuilder BuildRequestLog(HttpContext context, String url, String requestBodyText)
    {
        StringBuilder log = new();
        log.Append($"Request: Method: {context.Request.Method} Url: {url}");
        AppendHeaders(log, context.Request.Headers, RequestRedactedHeaders);
        if (requestBodyText != String.Empty)
        {
            log.Append($" Body: {requestBodyText}");
        }
        return log;
    }

    private static async Task WriteAndLogResponseAsync(HttpContext context, String url,
        ResponseLoggingMemoryStream responseBodyStream, Stream originalResponseBody, LogLevel logLevel)
    {
        responseBodyStream.Seek(0, SeekOrigin.Begin);
        String responseBody = await new StreamReader(responseBodyStream).ReadToEndAsync();
        Helpers.LogMessage(url, BuildResponseLog(context, responseBody), logLevel);
        responseBodyStream.Seek(0, SeekOrigin.Begin);
        await responseBodyStream.CopyToAsync(originalResponseBody);
        if (responseBodyStream.IsDisposed() && context.Request.Headers.ContainsKey("SOAPAction"))
        {
            responseBodyStream.ForceClose();
        }
    }

    private static StringBuilder BuildResponseLog(HttpContext context, String responseBody)
    {
        StringBuilder log = new();
        log.Append($"Response: Status Code: {context.Response.StatusCode}");
        AppendHeaders(log, context.Response.Headers, ResponseRedactedHeaders);
        if (!String.IsNullOrEmpty(responseBody))
        {
            log.Append($" Body: {responseBody}");
        }
        return log;
    }

    private static void AppendHeaders(StringBuilder log, IHeaderDictionary headers, HashSet<String> redactedHeaders)
    {
        if (headers == null || headers.Count == 0)
            return;

        log.Append(" Headers:");
        Boolean firstHeader = true;
        foreach (var header in headers)
        {
            if (!firstHeader)
                log.Append(',');
            firstHeader = false;

            String value = redactedHeaders.Contains(header.Key) ? "***REDACTED***" : header.Value.ToString();
            log.Append(' ');
            log.Append(header.Key);
            log.Append('=');
            log.Append(value);
        }
    }
}