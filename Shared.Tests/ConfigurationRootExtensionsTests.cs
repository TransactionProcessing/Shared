namespace Shared.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Castle.Components.DictionaryAdapter;
    using Extensions;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Hosting.Internal;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;
    using Shouldly;
    using Xunit;

    /// <summary>
    /// 
    /// </summary>
    public class ConfigurationRootExtensionsTests
    {
        #region Properties

        /// <summary>
        /// Gets the default application settings.
        /// </summary>
        /// <value>
        /// The default application settings.
        /// </value>
        public static IReadOnlyDictionary<String, String> DefaultAppSettings { get; } = new Dictionary<String, String>
        {
            ["AppSettings:Test"] = "",
            ["AppSettings:ClientId"] = "clientId",
            ["AppSettings:ClientSecret"] = "Secret1",
            ["EventStoreSettings:ConnectionString"] = "https://192.168.1.133:2113",
            ["ConnectionStrings:HealthCheck"] =
                                                                                                "server=192.168.1.133;database=master;user id=sa;password=Sc0tland",
            ["AppSettings:EventHandlerConfiguration:ResponseReceivedFromEmailProviderEvent:0"] =
                                                                                                "MessagingService.BusinessLogic.EventHandling.EmailDomainEventHandler, MessagingService.BusinessLogic",
            ["AppSettings:EventHandlerConfiguration:ResponseReceivedFromSMSProviderEvent:0"] =
                                                                                                "MessagingService.BusinessLogic.EventHandling.SMSDomainEventHandler, MessagingService.BusinessLogic"
        };
        
        #endregion

        #region Methods

        /// <summary>
        /// Configurations the root extensions log configuration configuration is logged.
        /// </summary>
        [Fact]
        public void ConfigurationRootExtensions_LogConfiguration_ConfigurationIsLogged()
        {
             IConfigurationBuilder builder = new ConfigurationBuilder().AddInMemoryCollection(ConfigurationRootExtensionsTests.DefaultAppSettings).AddEnvironmentVariables();

            IConfigurationRoot configuration = builder.Build();

            TestLogger testLogger = new TestLogger();
            Action<String> loggerAction = message => { testLogger.Log(LogLevel.None, message); };

            configuration.LogConfiguration(loggerAction);

            String[] loggedEntries = testLogger.GetLogEntries();
            Int32 expectedCount = ConfigurationRootExtensionsTests.DefaultAppSettings.Count + 7; // 3 blank lines & 4 headers
            loggedEntries.Length.ShouldBe(expectedCount);
            loggedEntries.Where(l => l.Contains("No Value")).Count().ShouldBe(1);
        }

        /// <summary>
        /// Configurations the root extensions log configuration no configuration no configuration is logged.
        /// </summary>
        [Fact]
        public void ConfigurationRootExtensions_LogConfiguration_NoConfiguration_NoConfigurationIsLogged()
        {
            IConfigurationBuilder builder = new ConfigurationBuilder();

            IConfigurationRoot configuration = builder.Build();

            TestLogger testLogger = new TestLogger();
            Action<String> loggerAction = message => { testLogger.Log(LogLevel.None, message); };

            configuration.LogConfiguration(loggerAction);

            String[] loggedEntries = testLogger.GetLogEntries();
            loggedEntries.Length.ShouldBe(0);
        }

        /// <summary>
        /// Configurations the root extensions log configuration null action error is thrown.
        /// </summary>
        [Fact]
        public void ConfigurationRootExtensions_LogConfiguration_NullAction_ErrorIsThrown()
        {
            IConfigurationBuilder builder = new ConfigurationBuilder().AddInMemoryCollection(ConfigurationRootExtensionsTests.DefaultAppSettings).AddEnvironmentVariables();

            IConfigurationRoot configuration = builder.Build();

            Action<String> loggerAction = null;

            Should.Throw<ArgumentNullException>(() => { configuration.LogConfiguration(loggerAction); });
        }

        #endregion
    }
}