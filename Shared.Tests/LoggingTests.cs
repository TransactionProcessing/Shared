using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shared.General;
using Shouldly;
using Xunit;

namespace Logging.Tests
{
    public class LoggingTests
    {
        [Fact]
        public void Logger_Initialise_IsInitialised()
        {
            ILogger logger = NullLogger.Instance;
            Logger.Initialise(logger);

            Logger.IsInitialised.ShouldBeTrue();
        }

        [Fact]
        public void Logger_Initialise_NullLogger_ErrorThrown()
        {
            Should.Throw<ArgumentNullException>(() => { Logger.Initialise(null); });
        }

        [Theory]
        [InlineData(LogLevel.Critical)]
        [InlineData(LogLevel.Debug)]
        [InlineData(LogLevel.Error)]
        [InlineData(LogLevel.Information)]
        [InlineData(LogLevel.Trace)]
        [InlineData(LogLevel.Warning)]
        public void Logger_LogMethods_LogWrittenNoErrors(LogLevel loglevel)
        {
            ILogger logger = NullLogger.Instance;

            String message = "Log Message";
            Logger.Initialise(logger);

            switch (loglevel)
            {
                case LogLevel.Critical:
                    Should.NotThrow(() => Logger.LogCritical(new Exception(message)));
                    break;
                case LogLevel.Debug:
                    Should.NotThrow(() => Logger.LogDebug(message));
                    break;
                case LogLevel.Error:
                    Should.NotThrow(() => Logger.LogError(new Exception(message)));
                    break;
                case LogLevel.Information:
                    Should.NotThrow(() => Logger.LogInformation(message));
                    break;
                case LogLevel.Trace:
                    Should.NotThrow(() => Logger.LogTrace(message));
                    break;
                case LogLevel.Warning:
                    Should.NotThrow(() => Logger.LogWarning(message));
                    break;
            }
        }        
    }
}
