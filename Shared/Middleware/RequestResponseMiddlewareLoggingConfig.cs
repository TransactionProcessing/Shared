using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Shared.Middleware;

[ExcludeFromCodeCoverage]
public record RequestResponseMiddlewareLoggingConfig(LogLevel LoggingLevel, Boolean LogRequests, Boolean LogResponses)
{
    public IReadOnlySet<String> SensitiveFieldNames { get; init; } = new HashSet<String>(new[] { "password", "token", "access_token", "refresh_token", "client_secret", "api_key", "cardnumber", "cvv", "pin" }, StringComparer.OrdinalIgnoreCase);
    public IReadOnlySet<String> SensitiveHeaderNames { get; init; } = new HashSet<String>(new[] { "Authorization", "Proxy-Authorization", "Cookie", "Set-Cookie", "X-API-Key" }, StringComparer.OrdinalIgnoreCase);
    public IReadOnlySet<String> AllowedRequestHeaders { get; init; } = new HashSet<String>(new[] { "Accept", "Content-Type", "User-Agent", "X-Request-Id", "correlationId", "Authorization", "Cookie" }, StringComparer.OrdinalIgnoreCase);
    public IReadOnlySet<String> AllowedResponseHeaders { get; init; } = new HashSet<String>(new[] { "Cache-Control", "Content-Length", "Content-Type", "ETag", "X-Response-Id", "Set-Cookie" }, StringComparer.OrdinalIgnoreCase);
    public IReadOnlySet<String> SupportedBodyContentTypes { get; init; } = new HashSet<String>(new[] { "application/json", "application/*+json", "application/x-www-form-urlencoded", "application/xml", "text/xml", "application/soap+xml" }, StringComparer.OrdinalIgnoreCase);
    public Int32 MaximumRequestBodyBytes { get; init; } = 32768;
    public Int32 MaximumResponseBodyBytes { get; init; } = 32768;
}
