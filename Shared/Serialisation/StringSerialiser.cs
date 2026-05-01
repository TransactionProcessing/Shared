using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Shared.Serialisation;

public enum SerialiserPropertyFormat
{
    CamelCase,
    SnakeCase,
}
public record SerialiserOptions(SerialiserPropertyFormat PropertyFormat, Boolean IgnoreNullValues = true, Boolean WriteIndented = false);


public interface IStringSerialiser {
    string Serialize<T>(T obj, SerialiserOptions serialiserOptions= null);
    T Deserialize<T>(string json, SerialiserOptions serialiserOptions = null);

    T DeserializeAnonymousType<T>(string json,
                                  T anonymousTypeObject, SerialiserOptions serialiserOptions = null);

    T DeserializeObject<T>(string json, Type type, SerialiserOptions serialiserOptions = null);

    T? GetValue<T>(string json, string propertyName);
}

public class SystemTextJsonSerializer : IStringSerialiser
{
    private readonly JsonSerializerOptions Options;

    public SystemTextJsonSerializer(JsonSerializerOptions options) {
        Options = options;
    }

    private JsonSerializerOptions BuildSerialiserOptions(SerialiserOptions serialiserOptions)
    {
        if (serialiserOptions == null) {
            return Options;
        }

        var options = new JsonSerializerOptions(Options);
        if (serialiserOptions.IgnoreNullValues)
        {
            options.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
        }
        if (serialiserOptions.WriteIndented)
        {
            options.WriteIndented = true;
        }
        options.PropertyNamingPolicy = serialiserOptions.PropertyFormat switch
        {
            SerialiserPropertyFormat.CamelCase => JsonNamingPolicy.CamelCase,
            SerialiserPropertyFormat.SnakeCase => JsonNamingPolicy.SnakeCaseLower,
            _ => options.PropertyNamingPolicy
        };
        return options;
    }

    public string Serialize<T>(T obj, SerialiserOptions serialiserOptions = null) {
        
        var options = BuildSerialiserOptions(serialiserOptions);
        return obj is null
            ? JsonSerializer.Serialize(obj, options)
            : JsonSerializer.Serialize(obj, obj.GetType(), options);
    }

    public T Deserialize<T>(string json, SerialiserOptions serialiserOptions = null) {
        var options = BuildSerialiserOptions(serialiserOptions);
        return JsonSerializer.Deserialize<T>(json, options)!;
    }

    public T DeserializeAnonymousType<T>(String json,
                                         T anonymousTypeObject, SerialiserOptions serialiserOptions = null) {
        var options = BuildSerialiserOptions(serialiserOptions);
        return JsonSerializer.Deserialize<T>(json, options)!;
    }

    public T DeserializeObject<T>(String json,
                                  Type type, SerialiserOptions serialiserOptions = null) {
        var options = BuildSerialiserOptions(serialiserOptions);
        return (T)JsonSerializer.Deserialize(json, type, options)!;
    }

    public T GetValue<T>(String json,
                         String propertyName) {
        using var doc = JsonDocument.Parse(json);
        var _root = doc.RootElement.Clone(); // important to avoid disposed doc issues

        if (TryFindProperty(_root, propertyName, out var value))
        {
            try
            {
                return value.Deserialize<T>();
            }
            catch
            {
                return default;
            }
        }

        return default;
    }

    private static bool TryFindProperty(
        JsonElement element,
        string propertyName,
        out JsonElement found)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var prop in element.EnumerateObject())
                {
                    if (string.Equals(prop.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                    {
                        found = prop.Value;
                        return true;
                    }

                    if (TryFindProperty(prop.Value, propertyName, out found))
                        return true;
                }
                break;

            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    if (TryFindProperty(item, propertyName, out found))
                        return true;
                }
                break;
        }

        found = default;
        return false;
    }
}

public static class StringSerialiser{
    public static Boolean IsInitialised { get; set; }
    private static IStringSerialiser Serializer;

    public static void Initialise(IStringSerialiser serialiser) {
        Serializer = serialiser;
        IsInitialised = true;
    }

    public static string Serialise<T>(T obj, SerialiserOptions serialiserOptions = null) {
        if (!IsInitialised) throw new InvalidOperationException("StringSerialiser is not initialised.");
        return Serializer.Serialize(obj, serialiserOptions);
    }

    public static T Deserialise<T>(string json, SerialiserOptions serialiserOptions = null) {
        if (!IsInitialised) throw new InvalidOperationException("StringSerialiser is not initialised.");
        return Serializer.Deserialize<T>(json, serialiserOptions);
    }

    public static T DeserialiseAnonymousType<T>(String json, T anonymousTypeObject, SerialiserOptions serialiserOptions = null) {
        if (!IsInitialised) throw new InvalidOperationException("StringSerialiser is not initialised.");
        return Serializer.DeserializeAnonymousType(json, anonymousTypeObject, serialiserOptions);
    }

    public static T DeserializeObject<T>(String json, Type type, SerialiserOptions serialiserOptions = null) {
        if (!IsInitialised) throw new InvalidOperationException("StringSerialiser is not initialised.");
        return Serializer.DeserializeObject<T>(json, type, serialiserOptions);
    }

    public static T GetValue<T>(String json, String propertyName, SerialiserOptions serialiserOptions = null) {
        if (!IsInitialised) throw new InvalidOperationException("StringSerialiser is not initialised.");
        return Serializer.GetValue<T>(json, propertyName);
    }
}
