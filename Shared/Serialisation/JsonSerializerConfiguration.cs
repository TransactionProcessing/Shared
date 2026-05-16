using System.Text.Json;

namespace Shared.Serialisation;

public static class JsonSerializerConfiguration
{
    public static void ConfigureMinimalApi(JsonSerializerOptions serializerOptions)
    {
        JsonSerializerOptions defaultOptions = SystemTextJsonSerializer.GetDefaultJsonSerializerOptions();
        serializerOptions.PropertyNamingPolicy = defaultOptions.PropertyNamingPolicy;
        serializerOptions.DictionaryKeyPolicy = defaultOptions.DictionaryKeyPolicy;
        serializerOptions.ReferenceHandler = defaultOptions.ReferenceHandler;
        serializerOptions.WriteIndented = defaultOptions.WriteIndented;
        serializerOptions.Converters.Add(new DateTimeSpaceConverter());
    }
}