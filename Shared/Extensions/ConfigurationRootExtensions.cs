namespace Shared.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using General;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Hosting.Internal;

    /// <summary>
    /// 
    /// </summary>
    public static class ConfigurationRootExtensions
    {
        /// <summary>
        /// Logs the configuration.
        /// </summary>
        /// <param name="configurationBuilder">The configuration builder.</param>
        /// <param name="loggerAction">The logger action.</param>
        public static void LogConfiguration(this IConfigurationRoot configurationBuilder,
                                            Action<String> loggerAction)
        {
            Guard.ThrowIfNull(loggerAction, nameof(loggerAction));

            IEnumerable<IConfigurationSection> sections = configurationBuilder.GetChildren();
            foreach (IConfigurationSection configurationSection in sections)
            {
                ConfigurationRootExtensions.LogConfigurationSettings(configurationSection, loggerAction);
            }
        }

        /// <summary>
        /// Logs the configuration settings.
        /// </summary>
        /// <param name="configSection">The configuration section.</param>
        /// <param name="loggerAction">The logger action.</param>
        private static void LogConfigurationSettings(IConfigurationSection configSection, Action<String> loggerAction)
        {
            IEnumerable<IConfigurationSection> children = configSection.GetChildren();

            if (children.Any())
            {
                loggerAction(string.Empty);
                loggerAction($"Configuration Section: {configSection.Key}");
                foreach (IConfigurationSection c in children)
                {
                    if (String.IsNullOrEmpty(c.Value))
                    {
                        loggerAction($"Key: {c.Key}  Value: No Value");
                    }
                    else
                    {
                        loggerAction($"Key: {c.Key}  Value: {c.Value}");
                    }
                    
                }
            }
        }
    }
}