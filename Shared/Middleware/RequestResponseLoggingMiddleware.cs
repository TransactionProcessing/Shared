#nullable enable
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Middleware;

public class RequestResponseLoggingMiddleware
{
    private readonly RequestDelegate next;

    public RequestResponseLoggingMiddleware(RequestDelegate next) {
        this.next = next;
    }

    public static Boolean IsHealthCheckRequest(String url) => url switch {
        _ when url.EndsWith("/health", StringComparison.OrdinalIgnoreCase) => true,
        _ when url.EndsWith("/healthui", StringComparison.OrdinalIgnoreCase) => true,
        _ => false,
    };

    internal static void LogMessage(String url, (String messageTemplate, Object?[] args) messageToLog, LogLevel logLevel) {

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

        String url = $"{context.Request.PathBase}{context.Request.Path}";
        Stream originalRequestBody = context.Request.Body;
        Stream originalResponseBody = context.Response.Body;

        Byte[] requestBody = await CaptureRequestBodyAsync(context, configuration.LogRequests);
        ResponseLoggingMemoryStream? responseBodyStream = SetupResponseCapture(context, configuration.LogResponses);

        try {
            await this.next(context);

            LogLevel effectiveLogLevel = context.Response.StatusCode is < 200 or > 299
                ? LogLevel.Warning
                : configuration.LoggingLevel;

            if (configuration.LogRequests) {
                (String messageTemplate, Object?[] args) requestLog = BuildRequestLog(context, url, requestBody, configuration);
                LogMessage(url, requestLog, effectiveLogLevel);
            }

            if (configuration.LogResponses && responseBodyStream != null) {
                await LogResponse(context, url, responseBodyStream, originalResponseBody, effectiveLogLevel, configuration);
            }
        }
        finally {
            context.Request.Body = originalRequestBody;
            context.Response.Body = originalResponseBody;
        }
    }

    private static async Task<Byte[]> CaptureRequestBodyAsync(HttpContext context, Boolean logRequests) {
        if (!logRequests)
            return Array.Empty<Byte>();

        MemoryStream requestBodyStream = new();
        await context.Request.Body.CopyToAsync(requestBodyStream);
        requestBodyStream.Seek(0, SeekOrigin.Begin);
        Byte[] body = requestBodyStream.ToArray();
        requestBodyStream.Position = 0;
        context.Request.Body = requestBodyStream;
        return body;
    }

    private static ResponseLoggingMemoryStream? SetupResponseCapture(HttpContext context, Boolean logResponses) {
        if (!logResponses)
            return null;

        ResponseLoggingMemoryStream responseBodyStream = new();
        context.Response.Body = responseBodyStream;
        return responseBodyStream;
    }

    private static (String messageTemplate, Object?[] args) BuildRequestLog(HttpContext context, String url, Byte[] requestBody, RequestResponseMiddlewareLoggingConfig configuration) {
        IReadOnlyDictionary<String, String[]> query = SensitiveDataRedactor.RedactQuery(context.Request.Query, configuration.SensitiveFieldNames);
        IReadOnlyDictionary<String, String> headers = SensitiveDataRedactor.FilterHeaders(context.Request.Headers, configuration.AllowedRequestHeaders, configuration.SensitiveHeaderNames);
        BodyCaptureResult body = requestBody.Length == 0
            ? new(null, "empty")
            : SensitiveDataRedactor.RedactBody(requestBody, context.Request.ContentType, configuration.SensitiveFieldNames, configuration.SupportedBodyContentTypes);

        StringBuilder messageTemplate = new("Request: Method: {method} Url: {url} ContentType: {contentType} Query: {query} Headers:");
        List<Object?> args = [context.Request.Method, url, context.Request.ContentType ?? String.Empty, query];
        AppendHeaderFields(messageTemplate, args, headers);
        messageTemplate.Append(" BodyStatus: {bodyStatus} Body: {requestBody}");
        args.Add(body.Status);
        args.Add(body.Body);
        return (messageTemplate.ToString(), args.ToArray());
    }

    private static async Task LogResponse(HttpContext context, String url, 
                                          ResponseLoggingMemoryStream responseBodyStream, Stream originalResponseBody, LogLevel logLevel,
                                          RequestResponseMiddlewareLoggingConfig configuration) {
        try {
            responseBodyStream.Seek(0, SeekOrigin.Begin);
            using MemoryStream capturedBody = new();
            await responseBodyStream.CopyToAsync(capturedBody);
            BodyCaptureResult body = capturedBody.Length == 0
                ? new(null, "empty")
                : SensitiveDataRedactor.RedactBody(capturedBody.ToArray(), context.Response.ContentType, configuration.SensitiveFieldNames, configuration.SupportedBodyContentTypes);

            (String messageTemplate, Object?[] args) responseLog = BuildResponseLog(context, url, body, configuration);
            LogMessage(url, responseLog, logLevel);
        }
        finally {
            responseBodyStream.Seek(0, SeekOrigin.Begin);
            await responseBodyStream.CopyToAsync(originalResponseBody);
            if (responseBodyStream.IsDisposed() && context.Request.Headers.ContainsKey("SOAPAction")) {
                responseBodyStream.ForceClose();
            }
        }
    }

    private static (String messageTemplate, Object?[] args) BuildResponseLog(HttpContext context, String url, BodyCaptureResult body, RequestResponseMiddlewareLoggingConfig configuration) {
        IReadOnlyDictionary<String, String> headers = SensitiveDataRedactor.FilterHeaders(context.Response.Headers, configuration.AllowedResponseHeaders, configuration.SensitiveHeaderNames);
        StringBuilder messageTemplate = new("Response: Url: {url} Status Code: {httpStatusCode} ContentType: {contentType} Headers:");
        List<Object?> args = [url, context.Response.StatusCode, context.Response.ContentType ?? String.Empty];
        AppendHeaderFields(messageTemplate, args, headers);
        messageTemplate.Append(" BodyStatus: {bodyStatus} Body: {responseBody}");
        args.Add(body.Status);
        args.Add(body.Body);
        return (messageTemplate.ToString(), args.ToArray());
    }

    private static void AppendHeaderFields(StringBuilder messageTemplate, List<Object?> args, IReadOnlyDictionary<String, String> headers) {
        HashSet<String> propertyNames = new(StringComparer.Ordinal);
        foreach (KeyValuePair<String, String> header in headers) {
            StringBuilder propertyName = new("header_");
            foreach (Char character in header.Key) {
                propertyName.Append(Char.IsLetterOrDigit(character) || character == '_' ? character : '_');
            }

            String candidate = propertyName.ToString();
            Int32 duplicateIndex = 2;
            while (!propertyNames.Add(candidate)) {
                candidate = $"{propertyName}_{duplicateIndex++}";
            }

            messageTemplate.Append(' ').Append(header.Key).Append(": {").Append(candidate).Append('}');
            args.Add(header.Value);
        }
    }
}
