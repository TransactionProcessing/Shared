using System;
using Microsoft.Extensions.Logging;

namespace Shared.Middleware;

public record RequestResponseMiddlewareLoggingConfig(LogLevel LoggingLevel, Boolean LogRequests, Boolean LogResponses);