using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Shared.Middleware;

[ExcludeFromCodeCoverage]
public record RequestResponseMiddlewareLoggingConfig(LogLevel LoggingLevel, Boolean LogRequests, Boolean LogResponses);