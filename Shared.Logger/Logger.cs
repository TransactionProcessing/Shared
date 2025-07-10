using Shared.Logger.TennantContext;

namespace Shared.Logger
{
    using NLog;
    using System;
    using System.Diagnostics.CodeAnalysis;

    [ExcludeFromCodeCoverage]
    public static class Logger {
        private static ILogger LoggerObject;

        public static Boolean IsInitialised { get; set; }

        public static void Initialise(NLog.Logger loggerObject,
                                      String fileName) {
            NlogLogger logger = new NlogLogger();
            logger.Initialise(loggerObject, fileName);
            Logger.Initialise(logger);
        }

        public static void Initialise(Microsoft.Extensions.Logging.ILogger loggerObject) {
            MicrosoftLogger logger = new MicrosoftLogger();
            logger.Initialise(loggerObject);
            Logger.Initialise(logger);
        }

        public static void Initialise(ILogger loggerObject) {
            Logger.LoggerObject = loggerObject ?? throw new ArgumentNullException(nameof(loggerObject));

            Logger.IsInitialised = true;
        }

        public static void LogCritical(Exception exception) {
            ValidateLoggerObject();

            TenantContext tenantContext = TenantContext.CurrentTenant;

            if (tenantContext == null) {
                LoggerObject.LogCritical(exception);
                return;
            }
            using (ScopeContext.PushProperty("correlationId", $"Correlation ID: {tenantContext.CorrelationId.ToString()}")) {
                // Write to the normal log
                LoggerObject.LogCritical(exception);

                if (tenantContext.PerTenantLogsEnabled && tenantContext.EstateId != Guid.Empty) {
                    // Write to the tenant log
                    using (ScopeContext.PushProperty("tenantId", $"_{tenantContext.EstateId.ToString()}")) {
                        LoggerObject.LogCritical(exception);
                    }
                }
            }
        
        }

        public static void LogDebug(String message) {
            ValidateLoggerObject();
            TenantContext tenantContext = TenantContext.CurrentTenant;

            if (tenantContext == null) {
                LoggerObject.LogDebug(message);
                return;
            }

            using (ScopeContext.PushProperty("correlationId", $"Correlation ID: {tenantContext.CorrelationId.ToString()}")) {
                // Write to the normal log
                LoggerObject.LogDebug(message);

                if (tenantContext.PerTenantLogsEnabled && tenantContext.EstateId != Guid.Empty) {
                    // Write to the tenant log
                    using (ScopeContext.PushProperty("tenantId", $"_{tenantContext.EstateId.ToString()}")) {
                        LoggerObject.LogDebug(message);
                    }
                }
            }
        }

        public static void LogError(Exception exception) {
            ValidateLoggerObject();

            TenantContext tenantContext = TenantContext.CurrentTenant;

            if (tenantContext == null) {
                LoggerObject.LogError(exception);
                return;
            }

            using (ScopeContext.PushProperty("correlationId", $"Correlation ID: {tenantContext.CorrelationId.ToString()}")) {
                // Write to the normal log
                LoggerObject.LogError(exception);

                if (tenantContext.PerTenantLogsEnabled && tenantContext.EstateId != Guid.Empty) {
                    // Write to the tenant log
                    using (ScopeContext.PushProperty("tenantId", $"_{tenantContext.EstateId.ToString()}")) {
                        LoggerObject.LogError(exception);
                    }
                }
            }
        }

        public static void LogError(String message,
                                    Exception exception) {
            ValidateLoggerObject();

            TenantContext tenantContext = TenantContext.CurrentTenant;
            if (tenantContext == null) {
                LoggerObject.LogError(message, exception);
                return;
            }
            using (ScopeContext.PushProperty("correlationId", $"Correlation ID: {tenantContext.CorrelationId.ToString()}")) {
                // Write to the normal log
                LoggerObject.LogError(message, exception);

                if (tenantContext.PerTenantLogsEnabled && tenantContext.EstateId != Guid.Empty) {
                    // Write to the tenant log
                    using (ScopeContext.PushProperty("tenantId", $"_{tenantContext.EstateId.ToString()}")) {
                        LoggerObject.LogError(message, exception);
                    }
                }
            }
        }

        public static void LogInformation(String message) {
            ValidateLoggerObject();

            TenantContext tenantContext = TenantContext.CurrentTenant;
            if (tenantContext == null) {
                Logger.LoggerObject.LogInformation(message);
                return;
            }
            using (ScopeContext.PushProperty("correlationId", $"Correlation ID: {tenantContext.CorrelationId.ToString()}")) {
                // Write to the normal log
                Logger.LoggerObject.LogInformation(message);

                if (tenantContext.PerTenantLogsEnabled && tenantContext.EstateId != Guid.Empty) {
                    // Write to the tenant log
                    using (ScopeContext.PushProperty("tenantId", $"_{tenantContext.EstateId.ToString()}")) {
                        Logger.LoggerObject.LogInformation(message);
                    }
                }
            }
        }

        public static void LogTrace(String message) {
            ValidateLoggerObject();

            TenantContext tenantContext = TenantContext.CurrentTenant;
            if (tenantContext == null) {
                Logger.LoggerObject.LogTrace(message);
                return;
            }
            using (ScopeContext.PushProperty("correlationId", $"Correlation ID: {tenantContext.CorrelationId.ToString()}")) {
                // Write to the normal log
                Logger.LoggerObject.LogTrace(message);

                if (tenantContext.PerTenantLogsEnabled && tenantContext.EstateId != Guid.Empty) {
                    // Write to the tenant log
                    using (ScopeContext.PushProperty("tenantId", $"_{tenantContext.EstateId.ToString()}")) {
                        Logger.LoggerObject.LogTrace(message);
                    }
                }
            }
        }

        public static void LogWarning(String message) {
            ValidateLoggerObject();

            TenantContext tenantContext = TenantContext.CurrentTenant;
            if (tenantContext == null) {
                Logger.LoggerObject.LogWarning(message);
                return;
            }
            using (ScopeContext.PushProperty("correlationId", $"Correlation ID: {tenantContext.CorrelationId.ToString()}")) {
                // Write to the normal log
                Logger.LoggerObject.LogWarning(message);

                if (tenantContext.PerTenantLogsEnabled && tenantContext.EstateId != Guid.Empty) {
                    // Write to the tenant log
                    using (ScopeContext.PushProperty("tenantId", $"_{tenantContext.EstateId.ToString()}")) {
                        Logger.LoggerObject.LogWarning(message);
                    }
                }
            }
        }

        private static void ValidateLoggerObject() {
            if (Logger.LoggerObject == null) {
                throw new InvalidOperationException("Logger has not been initialised");
            }
        }
    }
}