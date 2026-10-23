namespace Shared.Logger;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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
        Object?[] sanitizedArgs = Logger.SanitizeLogArgs(args);
        Logger.Write(logger => logger.LogTrace(messageTemplate, sanitizedArgs), true);
    }

    public static void LogDebug(String messageTemplate,
                                params Object?[] args)
    {
        Logger.ValidateLoggerObject();
        Object?[] sanitizedArgs = Logger.SanitizeLogArgs(args);
        Logger.Write(logger => logger.LogDebug(messageTemplate, sanitizedArgs), true);
    }

    public static void LogInformation(String messageTemplate,
                                      params Object?[] args)
    {
        Logger.ValidateLoggerObject();
        Object?[] sanitizedArgs = Logger.SanitizeLogArgs(args);
        Logger.Write(logger => logger.LogInformation(messageTemplate, sanitizedArgs), true);
    }

    public static void LogWarning(String messageTemplate,
                                  params Object?[] args)
    {
        Logger.ValidateLoggerObject();
        Object?[] sanitizedArgs = Logger.SanitizeLogArgs(args);
        Logger.Write(logger => logger.LogWarning(messageTemplate, sanitizedArgs), true);
    }

    public static void LogError(String messageTemplate,
                                params Object?[] args)
    {
        Logger.ValidateLoggerObject();
        Object?[] sanitizedArgs = Logger.SanitizeLogArgs(args);
        Logger.Write(logger => logger.LogError(messageTemplate, sanitizedArgs), true);
    }

    public static void LogError(Exception exception,
                                String messageTemplate,
                                params Object?[] args)
    {
        Logger.ValidateLoggerObject();
        Object?[] sanitizedArgs = Logger.SanitizeLogArgs(args);
        Logger.Write(logger => logger.LogError(exception, messageTemplate, sanitizedArgs), true);
    }

    public static void LogCritical(String messageTemplate,
                                   params Object?[] args)
    {
        Logger.ValidateLoggerObject();
        Object?[] sanitizedArgs = Logger.SanitizeLogArgs(args);
        Logger.Write(logger => logger.LogCritical(messageTemplate, sanitizedArgs), true);
    }

    public static void LogCritical(Exception exception,
                                   String messageTemplate,
                                   params Object?[] args)
    {
        Logger.ValidateLoggerObject();
        Object?[] sanitizedArgs = Logger.SanitizeLogArgs(args);
        Logger.Write(logger => logger.LogCritical(exception, messageTemplate, sanitizedArgs), true);
    }

    // Compatibility overloads below remain available for existing callers and can be removed only in a breaking release.
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
        String sanitizedMessage = Logger.SanitizeForPlainTextLog(message);
        Logger.Write(logger => logger.LogError(sanitizedMessage));
    }

    [Obsolete("Use LogError(Exception exception, String messageTemplate, params Object?[] args).")] // NOSONAR - retained for source compatibility until a breaking release.
    public static void LogError(String message,
                                Exception exception)
    {
        Logger.ValidateLoggerObject();
        String sanitizedMessage = Logger.SanitizeForPlainTextLog(message);
        Logger.Write(logger => logger.LogError(sanitizedMessage, exception));
    }

    [Obsolete("Use LogInformation(String messageTemplate, params Object?[] args).")] // NOSONAR - retained for source compatibility until a breaking release.
    public static void LogInformation(String message)
    {
        Logger.ValidateLoggerObject();
        String sanitizedMessage = Logger.SanitizeForPlainTextLog(message);
        Logger.Write(logger => logger.LogInformation(sanitizedMessage));
    }

    [Obsolete("Use LogTrace(String messageTemplate, params Object?[] args).")] // NOSONAR - retained for source compatibility until a breaking release.
    public static void LogTrace(String message)
    {
        Logger.ValidateLoggerObject();
        String sanitizedMessage = Logger.SanitizeForPlainTextLog(message);
        Logger.Write(logger => logger.LogTrace(sanitizedMessage));
    }

    [Obsolete("Use LogWarning(String messageTemplate, params Object?[] args).")] // NOSONAR - retained for source compatibility until a breaking release.
    public static void LogWarning(String message)
    {
        Logger.ValidateLoggerObject();
        String sanitizedMessage = Logger.SanitizeForPlainTextLog(message);
        Logger.Write(logger => logger.LogWarning(sanitizedMessage));
    }

    private static String SanitizeForPlainTextLog(String value)
    {
        return value.Replace("\r", String.Empty)
                    .Replace("\n", String.Empty);
    }

    private static Object?[] SanitizeLogArgs(Object?[] args) =>
        args.Select(Logger.SanitizeLogValue).ToArray();

    private static Object? SanitizeLogValue(Object? value)
    {
        if (value == null) return null;

        if (value is String text) return Logger.SanitizeForLog(text);

        if (value is IDictionary dictionary)
        {
            Dictionary<Object?, Object?> sanitized = new();
            foreach (DictionaryEntry entry in dictionary)
                sanitized[Logger.SanitizeLogValue(entry.Key)] = Logger.SanitizeLogValue(entry.Value);
            return sanitized;
        }

        if (value is IEnumerable enumerable && value is not String)
            return enumerable.Cast<Object?>().Select(Logger.SanitizeLogValue).ToArray();

        String? normalized = Convert.ToString(value);
        return normalized == null ? value : Logger.SanitizeForLog(normalized);
    }

    private static String SanitizeForLog(String value) =>
        value.Replace("\r", "\\r", StringComparison.Ordinal)
             .Replace("\n", "\\n", StringComparison.Ordinal);

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
