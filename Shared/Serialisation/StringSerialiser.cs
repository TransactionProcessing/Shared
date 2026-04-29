using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Shared.Serialisation;

public interface IStringSerialiser {
    string Serialize<T>(T obj);
    T Deserialize<T>(string json);
}

public class SystemTextJsonSerializer : IStringSerialiser
{
    private readonly JsonSerializerOptions Options;

    public SystemTextJsonSerializer(JsonSerializerOptions options) {
        Options = options;
    }

    public string Serialize<T>(T obj) {
        return JsonSerializer.Serialize(obj, Options);
    }

    public T Deserialize<T>(string json) {
        return JsonSerializer.Deserialize<T>(json, Options)!;
    }
}

public static class StringSerialiser{
    public static Boolean IsInitialised { get; set; }
    private static IStringSerialiser Serializer;

    public static void Initialise(IStringSerialiser serialiser) {
        Serializer = serialiser;
        IsInitialised = true;
    }

    public static string Serialise<T>(T obj) {
        return Serializer.Serialize(obj);
    }

    public static T Deserialize<T>(string json) {
        return Serializer.Deserialize<T>(json);
    }
}