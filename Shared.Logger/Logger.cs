namespace Shared.Logger;

using System;
using System.Diagnostics.CodeAnalysis;
using NLog;
using Shared.Logger.TennantContext;

[ExcludeFromCodeCoverage]
public static class Logger
{
    private static ILogger? LoggerObject;

    public static Boolean IsInitialised { get; set; }

    public static void Initialise(NLog.Logger loggerObject,
                                  String fileName)
    {
        NlogLogger logger = new();
        logger.Initialise(loggerObject, fileName);
        Logger.Initialise(logger);
    }

    public static void Initialise(ILogger loggerObject)
    {
        Logger.LoggerObject = loggerObject ?? throw new ArgumentNullException(nameof(loggerObject));
        Logger.IsInitialised = true;
    }

    public static void LogTrace(String messageTemplate,
                                params Object?[] args)
    {
        Logger.ValidateLoggerObject();
        Logger.Write(logger => logger.LogTrace(messageTemplate, args), true);
    }

    public static void LogDebug(String messageTemplate,
                                params Object?[] args)
    {
        Logger.ValidateLoggerObject();
        Logger.Write(logger => logger.LogDebug(messageTemplate, args), true);
    }

    public static void LogInformation(String messageTemplate,
                                      params Object?[] args)
    {
        Logger.ValidateLoggerObject();
        Logger.Write(logger => logger.LogInformation(messageTemplate, args), true);
    }

    public static void LogWarning(String messageTemplate,
                                  params Object?[] args)
    {
        Logger.ValidateLoggerObject();
        Logger.Write(logger => logger.LogWarning(messageTemplate, args), true);
    }

    public static void LogError(String messageTemplate,
                                params Object?[] args)
    {
        Logger.ValidateLoggerObject();
        Logger.Write(logger => logger.LogError(messageTemplate, args), true);
    }

    public static void LogError(Exception exception,
                                String messageTemplate,
                                params Object?[] args)
    {
        Logger.ValidateLoggerObject();
        Logger.Write(logger => logger.LogError(exception, messageTemplate, args), true);
    }

    public static void LogCritical(String messageTemplate,
                                   params Object?[] args)
    {
        Logger.ValidateLoggerObject();
        Logger.Write(logger => logger.LogCritical(messageTemplate, args), true);
    }

    public static void LogCritical(Exception exception,
                                   String messageTemplate,
                                   params Object?[] args)
    {
        Logger.ValidateLoggerObject();
        Logger.Write(logger => logger.LogCritical(exception, messageTemplate, args), true);
    }

    // Compatibility overloads below remain available for existing callers and can be removed only in a breaking release.
#pragma warning disable S1133 // These deprecated APIs are retained for source compatibility until a breaking release.
    [Obsolete("Use LogCritical(String messageTemplate, params Object?[] args) or the exception-aware structured overload.")] // NOSONAR - retained for source compatibility until a breaking release.
    public static void LogCritical(Exception exception)
    {
        Logger.ValidateLoggerObject();
        Logger.Write(logger => logger.LogCritical(exception));
    }

    [Obsolete("Use LogCritical(String messageTemplate, params Object?[] args) or the exception-aware structured overload.")] // NOSONAR - retained for source compatibility until a breaking release.
    public static void LogCritical(String message,
                                   Exception exception)
    {
        Logger.ValidateLoggerObject();
        Logger.Write(logger => logger.LogCritical(message, exception));
    }

    [Obsolete("Use LogDebug(String messageTemplate, params Object?[] args).")] // NOSONAR - retained for source compatibility until a breaking release.
    public static void LogDebug(String message)
    {
        Logger.ValidateLoggerObject();
        Logger.Write(logger => logger.LogDebug(message));
    }

    [Obsolete("Use LogError(Exception exception, String messageTemplate, params Object?[] args).")] // NOSONAR - retained for source compatibility until a breaking release.
    public static void LogError(Exception exception)
    {
        Logger.ValidateLoggerObject();
        Logger.Write(logger => logger.LogError(exception));
    }

    [Obsolete("Use LogError(String messageTemplate, params Object?[] args).")] // NOSONAR - retained for source compatibility until a breaking release.
    public static void LogError(String message)
    {
        Logger.ValidateLoggerObject();
        Logger.Write(logger => logger.LogError(message));
    }

    [Obsolete("Use LogError(Exception exception, String messageTemplate, params Object?[] args).")] // NOSONAR - retained for source compatibility until a breaking release.
    public static void LogError(String message,
                                Exception exception)
    {
        Logger.ValidateLoggerObject();
        Logger.Write(logger => logger.LogError(message, exception));
    }

    [Obsolete("Use LogInformation(String messageTemplate, params Object?[] args).")] // NOSONAR - retained for source compatibility until a breaking release.
    public static void LogInformation(String message)
    {
        Logger.ValidateLoggerObject();
        Logger.Write(logger => logger.LogInformation(message));
    }

    [Obsolete("Use LogTrace(String messageTemplate, params Object?[] args).")] // NOSONAR - retained for source compatibility until a breaking release.
    public static void LogTrace(String message)
    {
        Logger.ValidateLoggerObject();
        Logger.Write(logger => logger.LogTrace(message));
    }

    [Obsolete("Use LogWarning(String messageTemplate, params Object?[] args).")] // NOSONAR - retained for source compatibility until a breaking release.
    public static void LogWarning(String message)
    {
        Logger.ValidateLoggerObject();
        Logger.Write(logger => logger.LogWarning(message));
    }
#pragma warning restore S1133

    [MemberNotNull(nameof(LoggerObject))]
    private static void ValidateLoggerObject()
    {
        if (Logger.LoggerObject == null)
        {
            throw new InvalidOperationException("Logger has not been initialised");
        }
    }

    private static void Write(Action<ILogger> write, Boolean isStructured=false)
    {
        TenantContext tenantContext = TenantContext.CurrentTenant;
        if (tenantContext == null)
        {
            write(Logger.LoggerObject!);
            return;
        }

        String correlationId = isStructured switch {
                                   true => tenantContext.CorrelationId.ToString(),
                                   _ => $"Correlation ID: {tenantContext.CorrelationId}"
                               };

        using (ScopeContext.PushProperty(TenantContext.KeyNameCorrelationId, correlationId))
        {
            write(Logger.LoggerObject!);
            if (tenantContext.PerTenantLogsEnabled && tenantContext.EstateId != Guid.Empty)
            {
                using (ScopeContext.PushProperty("tenantId", $"_{tenantContext.EstateId}"))
                {
                    write(Logger.LoggerObject!);
                }
            }
        }
    }
}
