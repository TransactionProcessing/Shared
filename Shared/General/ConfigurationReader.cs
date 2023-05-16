namespace Shared.General
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Extensions.Configuration;
    using Newtonsoft.Json;

    public static class ConfigurationReader
    {
        #region Fields

        /// <summary>
        /// The configuration root
        /// </summary>
        private static IConfigurationRoot ConfigurationRoot;

        #endregion

        #region Properties

        /// <summary>
        /// Gets a value indicating whether this instance is initialised.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is initialised; otherwise, <c>false</c>.
        /// </value>
        public static Boolean IsInitialised { get; private set; }

        #endregion

        #region Methods

        /// <summary>
        /// Gets the base server URI.
        /// </summary>
        /// <param name="serviceName">Name of the service.</param>
        /// <returns></returns>
        public static Uri GetBaseServerUri(String serviceName) {
            var uriString = ConfigurationReader.ConfigurationRoot.GetSection("AppSettings")[serviceName];

            return new Uri(uriString);
        }

        /// <summary>
        /// Gets the connection string.
        /// </summary>
        /// <param name="keyName">Name of the key.</param>
        /// <returns></returns>
        public static String GetConnectionString(String keyName) {
            return ConfigurationReader.GetValueFromSection("ConnectionStrings", keyName);
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <param name="keyName">Name of the key.</param>
        /// <returns></returns>
        public static String GetValue(String keyName) {
            return ConfigurationReader.GetValueFromSection("AppSettings", keyName);
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <param name="sectionName">Name of the section.</param>
        /// <param name="keyName">Name of the key.</param>
        /// <returns></returns>
        public static String GetValue(String sectionName,
                                      String keyName) {
            return ConfigurationReader.GetValueFromSection(sectionName, keyName);
        }

        public static T GetValueFromSection<T>(String sectionName,
                                               String keyName) {
            return ConfigurationReader.GetTypedValueFromSection<T>(sectionName, keyName);
        }

        /// <summary>
        /// Initialises the specified configuration root.
        /// </summary>
        /// <param name="configurationRoot">The configuration root.</param>
        /// <exception cref="ArgumentNullException">configurationRoot</exception>
        public static void Initialise(IConfigurationRoot configurationRoot) {
            ConfigurationReader.ConfigurationRoot = configurationRoot ?? throw new ArgumentNullException(nameof(configurationRoot));
            ConfigurationReader.IsInitialised = true;
        }

        private static String GetValueFromSection(String sectionName, String keyName)
        {
            if (!ConfigurationReader.IsInitialised)
            {
                throw new InvalidOperationException("Configuration Reader has not been initialised");
            }

            IConfigurationSection section = null;
            try{
                section = ConfigurationReader.ConfigurationRoot.GetRequiredSection(sectionName);
            }
            catch(InvalidOperationException){
                throw new KeyNotFoundException($"Section [{sectionName}] not found.");
            }

            if (section[keyName] == null)
            {
                throw new KeyNotFoundException($"No configuration value was found for key [{sectionName}:{keyName}]");
            }

            return section[keyName];
        }

        private static T GetTypedValueFromSection<T>(String sectionName,
                                                String keyName) {
            if (!ConfigurationReader.IsInitialised) {
                throw new InvalidOperationException("Configuration Reader has not been initialised");
            }

            IConfigurationSection section = null;
            try
            {
                section = ConfigurationReader.ConfigurationRoot.GetRequiredSection(sectionName);
            }
            catch (InvalidOperationException)
            {
                throw new KeyNotFoundException($"Section [{sectionName}] not found.");
            }

            String value = section[keyName];

            if (value == null){
                throw new KeyNotFoundException($"No configuration value was found for key [{sectionName}:{keyName}]");
            }
            
            T returnValue = default(T);

            returnValue = JsonConvert.DeserializeAnonymousType(value, returnValue);
            
            return returnValue;
        }

        #endregion
    }
}