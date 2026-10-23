#nullable enable
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Shared.Middleware;

public class RequestResponseLoggingMiddleware
{
    private readonly RequestDelegate next;

    public RequestResponseLoggingMiddleware(RequestDelegate next) => this.next = next;

    public async Task Invoke(HttpContext context, RequestResponseMiddlewareLoggingConfig configuration)
    {
        String requestId = context.TraceIdentifier;
        String path = context.Request.Path.Value ?? String.Empty;
        Boolean healthCheck = Helpers.IsHealthCheckRequest(path);
        Stream requestBody = context.Request.Body;
        Stream responseBody = context.Response.Body;
        ResponseLoggingMemoryStream? captureStream = configuration.LogResponses
            ? new ResponseLoggingMemoryStream(responseBody, configuration.MaximumResponseBodyBytes)
            : null;
        Stopwatch timer = Stopwatch.StartNew();
        if (captureStream != null) context.Response.Body = captureStream;

        BodyCaptureResult requestCapture = new(null, "disabled");
        IReadOnlyDictionary<String, String[]> query = SensitiveDataRedactor.RedactQuery(context.Request.Query, configuration.SensitiveFieldNames);
        IReadOnlyDictionary<String, String> requestHeaders = SensitiveDataRedactor.FilterHeaders(context.Request.Headers, configuration.AllowedRequestHeaders, configuration.SensitiveHeaderNames);
        Dictionary<String, Object?>? requestFields = null;
        try
        {
            if (configuration.LogRequests)
            {
                requestCapture = await CaptureRequestAsync(context, configuration);
                requestFields = new()
                {
                    ["requestId"] = requestId, ["method"] = context.Request.Method, ["path"] = path,
                    ["query"] = query, ["headers"] = requestHeaders, ["bodyStatus"] = requestCapture.Status,
                    ["body"] = requestCapture.Body, ["healthCheck"] = healthCheck
                };
            }
            await this.next(context);
        }
        finally
        {
            timer.Stop();
            context.Request.Body = requestBody;
            if (requestFields != null)
            {
                LogLevel requestLevel = context.Response.StatusCode is < 200 or > 299 ? LogLevel.Warning : configuration.LoggingLevel;
                WriteLog(requestLevel, "HTTP request {requestId} {method} {path} {query} {headers} {bodyStatus} {body} {eventType}", requestFields);
            }
            if (captureStream != null)
            {
                context.Response.Body = responseBody;
                if (configuration.LogResponses)
                {
                    Byte[] responseBytes = captureStream.GetCapturedBytes();
                    BodyCaptureResult responseCapture = SensitiveDataRedactor.RedactBody(responseBytes, context.Response.ContentType,
                        configuration.SensitiveFieldNames, configuration.MaximumResponseBodyBytes, configuration.SupportedBodyContentTypes);
                    LogLevel level = context.Response.StatusCode is < 200 or > 299 ? LogLevel.Warning : configuration.LoggingLevel;
                    Dictionary<String, Object?> responseFields = new()
                    {
                        ["requestId"] = requestId, ["statusCode"] = context.Response.StatusCode, ["elapsedMs"] = timer.ElapsedMilliseconds,
                        ["headers"] = SensitiveDataRedactor.FilterHeaders(context.Response.Headers, configuration.AllowedResponseHeaders, configuration.SensitiveHeaderNames),
                        ["bodyStatus"] = responseCapture.Status, ["body"] = responseCapture.Body, ["healthCheck"] = healthCheck
                    };
                    WriteLog(level, "HTTP response {requestId} {statusCode} in {elapsedMs}ms {headers} {bodyStatus} {body} {eventType}", responseFields);
                    if (captureStream.IsDisposed() && context.Request.Headers.ContainsKey("SOAPAction")) captureStream.ForceClose();
                }
            }
        }
    }

    private static async Task<BodyCaptureResult> CaptureRequestAsync(HttpContext context, RequestResponseMiddlewareLoggingConfig configuration)
    {
        if (context.Request.ContentLength == 0) return new(null, "empty");
        Int32 limit = Math.Max(0, configuration.MaximumRequestBodyBytes);
        Stream? body = null;
        try
        {
            context.Request.EnableBuffering();
            body = context.Request.Body;
            using MemoryStream buffer = new();
            Byte[] chunk = new Byte[Math.Min(8192, Math.Max(1, limit + 1))];
            Int32 remaining = limit + 1;
            while (remaining > 0)
            {
                Int32 read = await body.ReadAsync(chunk.AsMemory(0, Math.Min(chunk.Length, remaining)));
                if (read == 0) break;
                await buffer.WriteAsync(chunk.AsMemory(0, read));
                remaining -= read;
            }
            return SensitiveDataRedactor.RedactBody(buffer.ToArray(), context.Request.ContentType, configuration.SensitiveFieldNames,
                limit, configuration.SupportedBodyContentTypes);
        }
        catch { return new(null, "redaction_failed"); }
        finally
        {
            if (body?.CanSeek == true) body.Position = 0;
        }
    }

    private static void WriteLog(LogLevel level, String template, Dictionary<String, Object?> fields)
    {
        Object? eventType = fields["healthCheck"] is true ? "HEALTH_CHECK" : "HTTP";
        Object?[] values = fields.ContainsKey("method")
            ? [fields["requestId"], fields["method"], fields["path"], fields["query"], fields["headers"], fields["bodyStatus"], fields["body"], eventType]
            : [fields["requestId"], fields["statusCode"], fields["elapsedMs"], fields["headers"], fields["bodyStatus"], fields["body"], eventType];
        try
        {
            switch (level)
            {
                case LogLevel.Trace: Logger.Logger.LogTrace(template, values); break;
                case LogLevel.Debug: Logger.Logger.LogDebug(template, values); break;
                case LogLevel.Warning: Logger.Logger.LogWarning(template, values); break;
                case LogLevel.Error: Logger.Logger.LogError(template, values); break;
                case LogLevel.Critical: Logger.Logger.LogCritical(template, values); break;
                default: Logger.Logger.LogInformation(template, values); break;
            }
        }
        catch { /* Logging must not break request processing or mask an application exception. */ }
    }
}
