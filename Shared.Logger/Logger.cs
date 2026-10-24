namespace Shared.Logger;

using System;
using System.Diagnostics.CodeAnalysis;
using NLog;
using TennantContext;

[ExcludeFromCodeCoverage]
public static class Logger {
    private static ILogger? LoggerObject;

    public static Boolean IsInitialised { get; set; }

    public static void Initialise(NLog.Logger loggerObject,
                                  String fileName) {
        NlogLogger logger = new();
        logger.Initialise(loggerObject, fileName);
        Initialise(logger);
    }

    public static void Initialise(ILogger loggerObject) {
        LoggerObject = loggerObject ?? throw new ArgumentNullException(nameof(loggerObject));
        IsInitialised = true;
    }

    public static void LogTrace(String messageTemplate,
                                params Object?[] args) {
        ValidateLoggerObject();
        Write(logger => logger.LogTrace(messageTemplate, args), true);
    }

    public static void LogDebug(String messageTemplate,
                                params Object?[] args) {
        ValidateLoggerObject();
        Write(logger => logger.LogDebug(messageTemplate, args), true);
    }

    public static void LogInformation(String messageTemplate,
                                      params Object?[] args) {
        ValidateLoggerObject();
        Write(logger => logger.LogInformation(messageTemplate, args), true);
    }

    public static void LogWarning(String message){
        ValidateLoggerObject();
        Write(logger => logger.LogWarning(message, null), true);
    }

    public static void LogWarning(String messageTemplate,
                                  params Object?[] args) {
        ValidateLoggerObject();
        Write(logger => logger.LogWarning(messageTemplate, args), true);
    }

    public static void LogError(String messageTemplate,
                                params Object?[] args) {
        ValidateLoggerObject();
        Write(logger => logger.LogError(messageTemplate, args), true);
    }

    public static void LogError(Exception exception,
                                String messageTemplate,
                                params Object?[] args) {
        ValidateLoggerObject();
        Write(logger => logger.LogError(exception, messageTemplate, args), true);
    }

    public static void LogCritical(String messageTemplate,
                                   params Object?[] args) {
        ValidateLoggerObject();
        Write(logger => logger.LogCritical(messageTemplate, args), true);
    }

    public static void LogCritical(Exception exception,
                                   String messageTemplate,
                                   params Object?[] args) {
        ValidateLoggerObject();
        Write(logger => logger.LogCritical(exception, messageTemplate, args), true);
    }

#pragma warning restore S1133

    [MemberNotNull(nameof(LoggerObject))]
    private static void ValidateLoggerObject() {
        if (LoggerObject == null) {
            throw new InvalidOperationException("Logger has not been initialised");
        }
    }

    private static void Write(Action<ILogger> write, Boolean isStructured = false) {
        TenantContext tenantContext = TenantContext.CurrentTenant;
        if (tenantContext == null) {
            write(LoggerObject!);
            return;
        }

        String correlationId = isStructured switch {
                                   true => tenantContext.CorrelationId.ToString(),
                                   _ => $"Correlation ID: {tenantContext.CorrelationId}"
                               };

        using (ScopeContext.PushProperty(TenantContext.KeyNameCorrelationId, correlationId)) {
            write(LoggerObject!);
            if (tenantContext.PerTenantLogsEnabled && tenantContext.EstateId != Guid.Empty) {
                using (ScopeContext.PushProperty("tenantId", $"_{tenantContext.EstateId}")) {
                    write(LoggerObject!);
                }
            }
        }
    }
}