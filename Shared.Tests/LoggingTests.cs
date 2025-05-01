using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Moq;
using Shared.General;
using Shouldly;
using Xunit;

namespace Shared.Tests{
    using Microsoft.AspNetCore.Http;
    using System.IO;
    using System.Threading.Tasks;

    public partial class SharedTests
    {
        [Fact]
        public void Logger_Initialise_IsInitialised(){
            TestHelpers.InitialiseLogger();
            Logger.Logger.IsInitialised.ShouldBeTrue();
        }

        [Theory]
        [InlineData(LogLevel.Critical)]
        [InlineData(LogLevel.Debug)]
        [InlineData(LogLevel.Error)]
        [InlineData(LogLevel.Information)]
        [InlineData(LogLevel.Trace)]
        [InlineData(LogLevel.Warning)]
        public void Logger_LogMethods_LogWrittenNoErrors(LogLevel loglevel){
            TestHelpers.InitialiseLogger();
            String message = "Log Message";
            
            switch(loglevel){
                case LogLevel.Critical:
                    Should.NotThrow(() => Logger.Logger.LogCritical(new Exception(message)));
                    break;
                case LogLevel.Debug:
                    Should.NotThrow(() => Logger.Logger.LogDebug(message));
                    break;
                case LogLevel.Error:
                    Should.NotThrow(() => Logger.Logger.LogError(new Exception(message)));
                    break;
                case LogLevel.Information:
                    Should.NotThrow(() => Logger.Logger.LogInformation(message));
                    break;
                case LogLevel.Trace:
                    Should.NotThrow(() => Logger.Logger.LogTrace(message));
                    break;
                case LogLevel.Warning:
                    Should.NotThrow(() => Logger.Logger.LogWarning(message));
                    break;
                default:
                    throw new InvalidDataException($"invlaid log level {loglevel}");
            }
        }
    }
}
