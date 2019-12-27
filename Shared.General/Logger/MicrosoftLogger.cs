namespace Shared.Logger
{
    using System;
    using Microsoft.Extensions.Logging;

    public class MicrosoftLogger : ILogger
    {
        /// <summary>
        /// The logger object
        /// </summary>
        private Microsoft.Extensions.Logging.ILogger LoggerObject;

        /// <summary>
        /// Initializes a new instance of the <see cref="Shared.Logger" /> class.
        /// </summary>
        /// <param name="loggerObject">The logger object.</param>
        /// <param name="fileName">Name of the scenario.</param>
        public void Initialise(Microsoft.Extensions.Logging.ILogger loggerObject)
        {
            this.LoggerObject = loggerObject;
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
            this.LogMessage(LogLevel.Critical, exception.Message, exception);
        }

        /// <summary>
        /// Logs the debug.
        /// </summary>
        /// <param name="message">The message.</param>
        public void LogDebug(String message)
        {
            this.LogMessage(LogLevel.Debug, message);
        }

        /// <summary>
        /// Logs the error.
        /// </summary>
        /// <param name="exception">The exception.</param>
        public void LogError(Exception exception)
        {
            this.LogMessage(LogLevel.Error, exception.Message, exception);
        }

        /// <summary>
        /// Logs the information.
        /// </summary>
        /// <param name="message">The message.</param>
        public void LogInformation(String message)
        {
            this.LogMessage(LogLevel.Information, message);
        }

        /// <summary>
        /// Logs the trace.
        /// </summary>
        /// <param name="message">The message.</param>
        public void LogTrace(String message)
        {
            this.LogMessage(LogLevel.Trace, message);
        }

        /// <summary>
        /// Logs the warning.
        /// </summary>
        /// <param name="message">The message.</param>
        public void LogWarning(String message)
        {
            this.LogMessage(LogLevel.Warning, message);
        }

        /// <summary>
        /// Logs the message.
        /// </summary>
        /// <param name="logLevel">The log level.</param>
        /// <param name="message">The message.</param>
        /// <param name="exception">The exception.</param>
        /// <exception cref="InvalidOperationException">LoggerObject has not been set</exception>
        private void LogMessage(LogLevel logLevel, String message, Exception exception = null)
        {
            if (this.LoggerObject != null)
            {
                this.LoggerObject.Log(logLevel, new EventId(),message, exception, (state,ex) => message );
            }
            else
            {
                throw new InvalidOperationException("LoggerObject has not been set");
            }
        }
    }
}