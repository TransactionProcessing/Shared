#nullable enable
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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

    public RequestResponseLoggingMiddleware(RequestDelegate next) {
        this.next = next;
    }

    public static Boolean IsHealthCheckRequest(String url) => url switch {
        _ when url.EndsWith("/health", StringComparison.OrdinalIgnoreCase) => true,
        _ when url.EndsWith("/healthui", StringComparison.OrdinalIgnoreCase) => true,
        _ => false,
    };

    internal static void LogMessage(String url, (String messageTemplate, Object[] args) messageToLog, LogLevel logLevel) {

        if (IsHealthCheckRequest(url))
            return;

        switch (logLevel) {
            case LogLevel.Trace:
                Shared.Logger.Logger.LogTrace(messageToLog.messageTemplate, messageToLog.args);
                break;
            case LogLevel.Debug:
                Shared.Logger.Logger.LogDebug(messageToLog.messageTemplate, messageToLog.args);
                break;
            case LogLevel.Warning:
                Shared.Logger.Logger.LogWarning(messageToLog.messageTemplate, messageToLog.args);
                break;
            case LogLevel.Error:
                Shared.Logger.Logger.LogError(messageToLog.messageTemplate, messageToLog.args);
                break;
            case LogLevel.Critical:
                Shared.Logger.Logger.LogCritical(messageToLog.messageTemplate, messageToLog.args);
                break;
            default:
                Shared.Logger.Logger.LogInformation(messageToLog.messageTemplate, messageToLog.args);
                break;
        }
    }

    public async Task Invoke(HttpContext context, RequestResponseMiddlewareLoggingConfig configuration) {

        String url = context.Request.GetDisplayUrl();
        Stream originalRequestBody = context.Request.Body;
        Stream originalResponseBody = context.Response.Body;

        String requestBodyText = await CaptureRequestBodyAsync(context, configuration.LogRequests);
        ResponseLoggingMemoryStream responseBodyStream = SetupResponseCapture(context, configuration.LogResponses);

        await this.next(context);

        LogLevel effectiveLogLevel = context.Response.StatusCode is < 200 or > 299
            ? LogLevel.Warning
            : configuration.LoggingLevel;

        if (configuration.LogRequests) {
            (String messageTemplate, Object[] args) requestLog = BuildRequestLog(context, url, requestBodyText);
            LogMessage(url, requestLog, effectiveLogLevel);
            context.Request.Body = originalRequestBody;
        }

        if (configuration.LogResponses) {
            await LogResponse(context, url, responseBodyStream, originalResponseBody, effectiveLogLevel);
        }
    }

    private static async Task<String> CaptureRequestBodyAsync(HttpContext context, Boolean logRequests) {
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

    private static ResponseLoggingMemoryStream SetupResponseCapture(HttpContext context, Boolean logResponses) {
        if (!logResponses)
            return null;

        ResponseLoggingMemoryStream responseBodyStream = new();
        context.Response.Body = responseBodyStream;
        return responseBodyStream;
    }

    private static (String messageTemplate, Object[]? args) BuildRequestLog(HttpContext context, String url, String requestBodyText) {
        StringBuilder log = new();

        StringBuilder messageTemplate = new StringBuilder();
        List<Object> args = new List<Object>();
        messageTemplate.Append("Request: Method: {method} Url: {url} ");
        args.Add(context.Request.Method);
        args.Add(url);

        AddHeaders(context.Response.Headers, messageTemplate, args);

        if (requestBodyText != String.Empty) {
            messageTemplate.Append(" Body: {requestBody}");
            args.Add(requestBodyText);
        }

        return (messageTemplate.ToString(), args.ToArray());
    }

    private static async Task LogResponse(HttpContext context, String url, 
                                          ResponseLoggingMemoryStream responseBodyStream, Stream originalResponseBody, LogLevel logLevel) {
        responseBodyStream.Seek(0, SeekOrigin.Begin);
        String responseBody = await new StreamReader(responseBodyStream).ReadToEndAsync();

        (String messageTemplate, Object[] args) responseLog = BuildResponseLog(context, responseBody);
        LogMessage(url, responseLog, logLevel);

        responseBodyStream.Seek(0, SeekOrigin.Begin);
        await responseBodyStream.CopyToAsync(originalResponseBody);
        if (responseBodyStream.IsDisposed() && context.Request.Headers.ContainsKey("SOAPAction")) {
            responseBodyStream.ForceClose();
        }
    }

    private static (String messageTemplate, Object[]? args) BuildResponseLog(HttpContext context, String responseBody) {
        StringBuilder messageTemplate = new();
        List<object> args = new List<object>();
        messageTemplate.Append("Response: Status Code: {httpStatusCode}");
        args.Add(context.Response.StatusCode);

        AddHeaders(context.Response.Headers, messageTemplate, args);

        if (!String.IsNullOrEmpty(responseBody)) {
            messageTemplate.Append(" Body: {responseBody}");
            args.Add(responseBody);
        }
        return (messageTemplate.ToString(), args.ToArray());
    }

    internal static void AddHeaders(IHeaderDictionary headerDictionary, StringBuilder messageTemplate, List<object> args) {
        if (headerDictionary != null && headerDictionary.Any()) {
            messageTemplate.Append(" Headers: ");
            foreach (KeyValuePair<String, StringValues> header in headerDictionary) {
                String value = RequestRedactedHeaders.Contains(header.Key) || ResponseRedactedHeaders.Contains(header.Key)
                    ? "***REDACTED***"
                    : header.Value.ToString();
                messageTemplate.Append($"{header.Key}={{{header.Key}}}");
                args.Add(value);
            }
        }
    }
}
