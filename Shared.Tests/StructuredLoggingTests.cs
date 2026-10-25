#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NLog.Config;
using NLog.Targets;
using Shared.Logger;
using Shared.Logger.TennantContext;
using Shared.Middleware;
using Shouldly;
using Xunit;

namespace Shared.Tests;

[Collection("Sequential")]
public class StructuredLoggingTests
{
    [Fact]
    public void NlogLogger_EmitsTemplatePropertiesAndEventMetadata()
    {
        using NLog.LogFactory factory = new();
        MemoryTarget target = new("memory")
        {
            Layout = "${message}|${event-properties:item=messageTemplate}|${event-properties:item=eventId}|${event-properties:item=timestampUtc}|${event-properties:item=serviceName}|${event-properties:item=severity}|${event-properties:item=traceId}|${event-properties:item=spanId}|${event-properties:item=correlationId}|${event-properties:item=tenantId}|${event-properties:item=merchantId}"
        };
        LoggingConfiguration configuration = new(factory);
        configuration.AddTarget(target);
        configuration.AddRuleForAllLevels(target);
        factory.Configuration = configuration;
        NlogLogger logger = new();
        logger.Initialise(factory.GetLogger("structured-test"), "test.log");

        using Activity activity = new("structured-test");
        activity.SetIdFormat(ActivityIdFormat.W3C);
        activity.Start();
        Guid correlationId = Guid.Parse("0e4cb1b8-0f6d-44d7-a648-8fdfed4f6a77");
        Guid estateId = Guid.Parse("2db81bd4-3da1-4f16-a55d-7701c668ea06");
        Guid merchantId = Guid.Parse("967df918-7ef5-47d3-aac2-6a896a2377cd");
        TenantContext tenant = new();
        tenant.SetCorrelationId(CorrelationId.From(correlationId));
        tenant.Initialise(new TenantIdentifiers(estateId, merchantId), false);
        TenantContext.CurrentTenant = tenant;
        try
        {
            logger.LogInformation("Processed {count} items", 3);
        }
        finally
        {
            TenantContext.CurrentTenant = null;
        }

        String[] fields = target.Logs.ShouldHaveSingleItem().Split('|');
        fields[0].ShouldBe("Processed 3 items");
        fields[1].ShouldBe("Processed {count} items");
        Guid.TryParse(fields[2], out _).ShouldBeTrue();
        fields[3].ShouldNotBeNullOrEmpty();
        fields[4].ShouldNotBeNullOrEmpty();
        fields[5].ShouldBe("Info");
        fields[6].ShouldBe(activity.TraceId.ToString());
        fields[7].ShouldBe(activity.SpanId.ToString());
        fields[8].ShouldBe(correlationId.ToString());
        fields[9].ShouldBe(estateId.ToString());
        fields[10].ShouldBe(merchantId.ToString());
    }

    [Fact]
    public async Task Middleware_RedactsSensitiveQueryHeadersAndJsonBody()
    {
        CapturingLogger logger = new();
        Shared.Logger.Logger.Initialise(logger);
        DefaultHttpContext context = CreateContext("{\"username\":\"alice\",\"password\":\"body-secret\"}", "application/json");
        context.Request.QueryString = new QueryString("?token=query-secret&search=visible");
        context.Request.Headers.Authorization = "header-secret";
        context.Request.Headers["X-Request-Id"] = "request-123";
        context.Request.Headers["X-Unlisted"] = "unlisted-secret";
        RequestResponseLoggingMiddleware middleware = new(_ => Task.CompletedTask);

        await middleware.Invoke(context, new RequestResponseMiddlewareLoggingConfig(LogLevel.Information, true, false));

        StructuredLogEntry request = logger.Entries.ShouldHaveSingleItem();
        request.Template.ShouldContain("{query}");
        request.Template.ShouldContain("{header_Authorization}");
        request.Template.ShouldContain("{header_X_Request_Id}");
        request.Template.ShouldNotContain("X-Unlisted");
        String query = JsonSerializer.Serialize(GetStructuredValue(request, "query"));
        query.ShouldContain("[REDACTED]");
        query.ShouldContain("visible");
        String headers = JsonSerializer.Serialize(new[]
        {
            GetStructuredValue(request, "header_Authorization"),
            GetStructuredValue(request, "header_X_Request_Id")
        });
        headers.ShouldContain("[REDACTED]");
        headers.ShouldContain("request-123");
        String body = GetStructuredValue(request, "requestBody")!.ToString()!;
        body.ShouldContain("[REDACTED]");
        body.ShouldContain("alice");
        String allFields = String.Join('|', request.Arguments);
        allFields.ShouldNotContain("query-secret");
        allFields.ShouldNotContain("header-secret");
        allFields.ShouldNotContain("body-secret");
        allFields.ShouldNotContain("unlisted-secret");
    }

    [Fact]
    public async Task Middleware_PreservesQueryAndHeaderCollectionsWithLegacyLogger()
    {
        LegacyCapturingLogger logger = new();
        Shared.Logger.Logger.Initialise(logger);
        DefaultHttpContext context = CreateContext(String.Empty, null);
        context.Request.QueryString = new QueryString("?search=visible&token=query-secret");
        context.Request.Headers.Authorization = "header-secret";
        context.Request.Headers["X-Request-Id"] = "request-123";
        RequestResponseLoggingMiddleware middleware = new(_ => Task.CompletedTask);

        await middleware.Invoke(context, new RequestResponseMiddlewareLoggingConfig(LogLevel.Information, true, false));

        String log = logger.Entries.ShouldHaveSingleItem();
        log.ShouldContain("search");
        log.ShouldContain("visible");
        log.ShouldContain("X-Request-Id");
        log.ShouldContain("request-123");
        log.ShouldContain("[REDACTED]");
        log.ShouldNotContain("query-secret");
        log.ShouldNotContain("header-secret");
        log.ShouldNotContain("System.Collections.Generic.Dictionary");
    }

    [Fact]
    public async Task Middleware_RedactsRequestBodyAndRestoresRequestStream()
    {
        CapturingLogger logger = new();
        Shared.Logger.Logger.Initialise(logger);
        DefaultHttpContext context = CreateContext("{\"data\":\"123456789\"}", "application/json");
        Stream originalBody = context.Request.Body;
        RequestResponseLoggingMiddleware middleware = new(_ => Task.CompletedTask);

        await middleware.Invoke(context, new RequestResponseMiddlewareLoggingConfig(LogLevel.Information, true, false));

        StructuredLogEntry request = logger.Entries.ShouldHaveSingleItem();
        GetStructuredValue(request, "bodyStatus").ShouldBe("captured");
        GetStructuredValue(request, "requestBody")!.ToString().ShouldContain("123456789");
        context.Request.Body.ShouldBeSameAs(originalBody);
    }

    [Fact]
    public async Task Middleware_RedactsSensitiveJsonResponseBodyAndPreservesResponse()
    {
        CapturingLogger logger = new();
        Shared.Logger.Logger.Initialise(logger);
        DefaultHttpContext context = CreateContext(String.Empty, null);
        context.Response.Body = new MemoryStream();
        const String response = "{\"status\":\"ok\",\"access_token\":\"response-secret\"}";
        RequestResponseLoggingMiddleware middleware = new(inner =>
        {
            inner.Response.ContentType = "application/json";
            return inner.Response.WriteAsync(response, TestContext.Current.CancellationToken);
        });

        await middleware.Invoke(context, new RequestResponseMiddlewareLoggingConfig(LogLevel.Information, false, true));

        StructuredLogEntry log = logger.Entries.ShouldHaveSingleItem();
        String body = GetStructuredValue(log, "responseBody")!.ToString()!;
        body.ShouldContain("[REDACTED]");
        body.ShouldNotContain("response-secret");
        context.Response.Body.Position = 0;
        using StreamReader reader = new(context.Response.Body, Encoding.UTF8, leaveOpen: true);
        (await reader.ReadToEndAsync(TestContext.Current.CancellationToken)).ShouldBe(response);
    }

    [Fact]
    public async Task Middleware_ReportsUnsupportedBodyContentTypeWithoutLoggingPayload()
    {
        CapturingLogger logger = new();
        Shared.Logger.Logger.Initialise(logger);
        DefaultHttpContext context = CreateContext("binary-secret", "application/octet-stream");
        RequestResponseLoggingMiddleware middleware = new(_ => Task.CompletedTask);

        await middleware.Invoke(context, new RequestResponseMiddlewareLoggingConfig(LogLevel.Information, true, false));

        StructuredLogEntry request = logger.Entries.ShouldHaveSingleItem();
        GetStructuredValue(request, "bodyStatus").ShouldBe("unsupported_content_type");
        GetStructuredValue(request, "requestBody").ShouldBeNull();
        String.Join('|', request.Arguments).ShouldNotContain("binary-secret");
    }

    [Theory]
    [InlineData("application/x-www-form-urlencoded", "password=body-secret&name=alice", "password=[REDACTED]&name=alice")]
    [InlineData("application/xml", "<root><password>body-secret</password><name>alice</name></root>", "<root><password>[REDACTED]</password><name>alice</name></root>")]
    [InlineData("application/xml", "<password>root-secret</password>", "<password>[REDACTED]</password>")]
    [InlineData("application/xml", "<root><password token=\"attribute-secret\">body-secret</password></root>", "<root><password token=\"[REDACTED]\">[REDACTED]</password></root>")]
    public async Task Middleware_RedactsSensitiveFormAndXmlFields(String contentType, String payload, String expectedBody)
    {
        CapturingLogger logger = new();
        Shared.Logger.Logger.Initialise(logger);
        DefaultHttpContext context = CreateContext(payload, contentType);
        RequestResponseLoggingMiddleware middleware = new(_ => Task.CompletedTask);

        await middleware.Invoke(context, new RequestResponseMiddlewareLoggingConfig(LogLevel.Information, true, false));

        StructuredLogEntry request = logger.Entries.ShouldHaveSingleItem();
        GetStructuredValue(request, "requestBody").ShouldBe(expectedBody);
        String redactedBody = GetStructuredValue(request, "requestBody") as String ?? String.Empty;
        redactedBody.ShouldNotContain("body-secret");
    }

    [Fact]
    public async Task Middleware_RedactsResponseBodyWithoutSizeLimit()
    {
        CapturingLogger logger = new();
        Shared.Logger.Logger.Initialise(logger);
        DefaultHttpContext context = CreateContext(String.Empty, null);
        const String payload = "{\"result\":\"a value larger than the limit\"}";
        RequestResponseLoggingMiddleware middleware = new(inner =>
        {
            inner.Response.ContentType = "application/json";
            return inner.Response.WriteAsync(payload, TestContext.Current.CancellationToken);
        });

        await middleware.Invoke(context, new RequestResponseMiddlewareLoggingConfig(LogLevel.Information, false, true));

        StructuredLogEntry response = logger.Entries.ShouldHaveSingleItem();
        GetStructuredValue(response, "bodyStatus").ShouldBe("captured");
        GetStructuredValue(response, "responseBody")!.ToString().ShouldContain("a value larger than the limit");
        context.Response.Body.Position = 0;
        using StreamReader reader = new(context.Response.Body, Encoding.UTF8, leaveOpen: true);
        (await reader.ReadToEndAsync(TestContext.Current.CancellationToken)).ShouldBe(payload);
    }

    private static DefaultHttpContext CreateContext(String body, String? contentType)
    {
        DefaultHttpContext context = new();
        Byte[] bytes = Encoding.UTF8.GetBytes(body);
        context.Request.Body = new MemoryStream(bytes);
        context.Request.ContentLength = bytes.Length;
        context.Request.ContentType = contentType;
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static Object? GetStructuredValue(StructuredLogEntry entry, String propertyName)
    {
        Int32 argumentIndex = 0;
        Int32 searchFrom = 0;
        while (true)
        {
            Int32 placeholderStart = entry.Template.IndexOf('{', searchFrom);
            if (placeholderStart < 0) break;
            Int32 placeholderEnd = entry.Template.IndexOf('}', placeholderStart + 1);
            if (placeholderEnd < 0) break;
            String currentProperty = entry.Template.Substring(placeholderStart + 1, placeholderEnd - placeholderStart - 1);
            if (currentProperty == propertyName) return entry.Arguments[argumentIndex];
            argumentIndex++;
            searchFrom = placeholderEnd + 1;
        }
        throw new InvalidOperationException($"Structured property '{propertyName}' was not found in the log template.");
    }

    private sealed record StructuredLogEntry(String Template, Object?[] Arguments, LogLevel Level);

    private sealed class CapturingLogger : Shared.Logger.ILogger
    {
        public Boolean IsInitialised { get; set; } = true;
        public List<StructuredLogEntry> Entries { get; } = new();

        public void LogCritical(String message) => this.Add(message, Array.Empty<Object?>(), LogLevel.Critical);
        public void LogCritical(Exception exception) => this.Add(exception.Message, Array.Empty<Object?>(), LogLevel.Critical);
        public void LogCritical(String message, Exception exception) => this.Add(message, [exception], LogLevel.Critical);
        public void LogCritical(String messageTemplate, params Object?[] args) => this.Add(messageTemplate, args, LogLevel.Critical);
        public void LogCritical(Exception exception, String messageTemplate, params Object?[] args) => this.Add(messageTemplate, args, LogLevel.Critical);
        public void LogDebug(String message) => this.Add(message, Array.Empty<Object?>(), LogLevel.Debug);
        public void LogDebug(String messageTemplate, params Object?[] args) => this.Add(messageTemplate, args, LogLevel.Debug);
        public void LogError(Exception exception) => this.Add(exception.Message, Array.Empty<Object?>(), LogLevel.Error);
        public void LogError(String message) => this.Add(message, Array.Empty<Object?>(), LogLevel.Error);
        public void LogError(String message, Exception exception) => this.Add(message, [exception], LogLevel.Error);
        public void LogError(String messageTemplate, params Object?[] args) => this.Add(messageTemplate, args, LogLevel.Error);
        public void LogError(Exception exception, String messageTemplate, params Object?[] args) => this.Add(messageTemplate, args, LogLevel.Error);
        public void LogInformation(String message) => this.Add(message, Array.Empty<Object?>(), LogLevel.Information);
        public void LogInformation(String messageTemplate, params Object?[] args) => this.Add(messageTemplate, args, LogLevel.Information);
        public void LogTrace(String message) => this.Add(message, Array.Empty<Object?>(), LogLevel.Trace);
        public void LogTrace(String messageTemplate, params Object?[] args) => this.Add(messageTemplate, args, LogLevel.Trace);
        public void LogWarning(String message) => this.Add(message, Array.Empty<Object?>(), LogLevel.Warning);
        public void LogWarning(String messageTemplate, params Object?[] args) => this.Add(messageTemplate, args, LogLevel.Warning);

        private void Add(String template, Object?[] args, LogLevel level) => this.Entries.Add(new(template, args, level));
    }

    private sealed class LegacyCapturingLogger : Shared.Logger.ILogger
    {
        public Boolean IsInitialised { get; set; } = true;
        public List<String> Entries { get; } = new();

        public void LogCritical(String message) => this.Entries.Add(message);
        public void LogCritical(Exception exception) => this.Entries.Add(exception.Message);
        public void LogCritical(String message, Exception exception) => this.Entries.Add(message);
        public void LogDebug(String message) => this.Entries.Add(message);
        public void LogError(Exception exception) => this.Entries.Add(exception.Message);
        public void LogError(String message) => this.Entries.Add(message);
        public void LogError(String message, Exception exception) => this.Entries.Add(message);
        public void LogInformation(String message) => this.Entries.Add(message);
        public void LogTrace(String message) => this.Entries.Add(message);
        public void LogWarning(String message) => this.Entries.Add(message);
    }
}
