using Shared.Serialisation;

namespace Shared.General;

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

public static class ConfigurationReader
{
    private static IConfigurationRoot ConfigurationRoot;

    public static Boolean IsInitialised { get; private set; }
    
    public static Uri GetBaseServerUri(String serviceName) {
        var uriString = ConfigurationReader.ConfigurationRoot.GetSection("AppSettings")[serviceName];

        return new Uri(uriString);
    }

    public static String GetConnectionString(String keyName) {
        return ConfigurationReader.GetValueFromSection("ConnectionStrings", keyName);
    }

    public static String GetValue(String keyName) {
        return ConfigurationReader.GetValueFromSection("AppSettings", keyName);
    }

    public static String GetValue(String sectionName,
                                  String keyName) {
        return ConfigurationReader.GetValueFromSection(sectionName, keyName);
    }

    public static T GetValueFromSection<T>(String sectionName,
                                           String keyName) {
        return ConfigurationReader.GetTypedValueFromSection<T>(sectionName, keyName);
    }

    public static T GetValueOrDefault<T>(String sectionName,
                                         String keyName,
                                         T defaultValue) {
        try {
            var value = ConfigurationReader.GetValue(sectionName, keyName);

            if (String.IsNullOrEmpty(value)) {
                return defaultValue;
            }

            if (typeof(T).IsEnum) {
                return (T)Enum.Parse(typeof(T), value, ignoreCase: true);
            }

            return (T)Convert.ChangeType(value, typeof(T));
        }
        catch (KeyNotFoundException) {
            return defaultValue;
        }
    }

    public static void Initialise(IConfigurationRoot configurationRoot) {
        ConfigurationReader.ConfigurationRoot = configurationRoot ?? throw new ArgumentNullException(nameof(configurationRoot));
        ConfigurationReader.IsInitialised = true;
    }

    private static String GetValueFromSection(String sectionName,
                                              String keyName) {
        if (!ConfigurationReader.IsInitialised) {
            throw new InvalidOperationException("Configuration Reader has not been initialised");
        }

        IConfigurationSection section = null;
        try {
            section = ConfigurationReader.ConfigurationRoot.GetRequiredSection(sectionName);
        }
        catch (InvalidOperationException) {
            throw new KeyNotFoundException($"Section [{sectionName}] not found.");
        }

        if (section[keyName] == null) {
            throw new KeyNotFoundException($"No configuration value was found for key [{sectionName}:{keyName}]");
        }

        return section[keyName];
    }

    private static T GetTypedValueFromSection<T>(String sectionName,
                                                 String keyName) {
        if (!IsInitialised) {
            throw new InvalidOperationException("Configuration Reader has not been initialised");
        }

        IConfigurationSection section;
        try
        {
            section = ConfigurationRoot.GetRequiredSection(sectionName);
        }
        catch (InvalidOperationException)
        {
            throw new KeyNotFoundException($"Section [{sectionName}] not found.");
        }

        String value = section[keyName];

        if (value == null){
            throw new KeyNotFoundException($"No configuration value was found for key [{sectionName}:{keyName}]");
        }
            
        T returnValue = default;

        returnValue = StringSerialiser.DeserialiseAnonymousType(value, returnValue);
            
        return returnValue;
    }
}