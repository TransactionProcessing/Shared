namespace Shared.General.Logger
{
    using System;
    using NLog;

    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="ILogger" />
    public class NlogLogger : ILogger
    {
        /// <summary>
        /// The scenario name
        /// </summary>
        private String FileName;

        /// <summary>
        /// The logger object
        /// </summary>
        private NLog.Logger LoggerObject;

        /// <summary>
        /// Initializes a new instance of the <see cref="Shared.Logger" /> class.
        /// </summary>
        /// <param name="loggerObject">The logger object.</param>
        /// <param name="fileName">Name of the scenario.</param>
        public void Initialise(NLog.Logger loggerObject,
                               String fileName)
        {
            this.LoggerObject = loggerObject;
            this.FileName = fileName;
            this.IsInitialised = true;
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is initialised.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is initialised; otherwise, <c>false</c>.
        /// </value>
        public Boolean IsInitialised { get; set; }

        /// <summary>
        /// Logs the critical.
        /// </summary>
        /// <param name="exception">The exception.</param>
        public void LogCritical(Exception exception)
        {
            this.LogMessage(NLog.LogLevel.Fatal, exception.Message, exception);
        }

        /// <summary>
        /// Logs the debug.
        /// </summary>
        /// <param name="message">The message.</param>
        public void LogDebug(String message)
        {
            this.LogMessage(NLog.LogLevel.Debug, message);
        }

        /// <summary>
        /// Logs the error.
        /// </summary>
        /// <param name="exception">The exception.</param>
        public void LogError(Exception exception)
        {
            this.LogMessage(NLog.LogLevel.Error, exception.Message, exception);
        }

        /// <summary>
        /// Logs the information.
        /// </summary>
        /// <param name="message">The message.</param>
        public void LogInformation(String message)
        {
            this.LogMessage(NLog.LogLevel.Info, message);
        }

        /// <summary>
        /// Logs the trace.
        /// </summary>
        /// <param name="message">The message.</param>
        public void LogTrace(String message)
        {
            this.LogMessage(NLog.LogLevel.Trace, message);
        }

        /// <summary>
        /// Logs the warning.
        /// </summary>
        /// <param name="message">The message.</param>
        public void LogWarning(String message)
        {
            this.LogMessage(NLog.LogLevel.Warn, message);
        }

        /// <summary>
        /// Logs the message.
        /// </summary>
        /// <param name="logLevel">The log level.</param>
        /// <param name="message">The message.</param>
        /// <param name="exception">The exception.</param>
        /// <exception cref="InvalidOperationException">LoggerObject has not been set</exception>
        private void LogMessage(NLog.LogLevel logLevel, String message, Exception exception = null)
        {
            if (this.LoggerObject != null)
            {
                LogEventInfo eventInfo = new LogEventInfo(logLevel, "Logger", message);
                eventInfo.Exception = exception;
                eventInfo.Properties["FileName"] = this.FileName;

                this.LoggerObject.Log(eventInfo);
            }
            else
            {
                throw new InvalidOperationException("LoggerObject has not been set");
            }
        }
    }
}