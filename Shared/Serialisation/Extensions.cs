using System;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Shared.Serialisation;

public static class Extensions
{
    public static JsonSerializerOptions AddModifier(
        this JsonSerializerOptions options,
        Action<JsonTypeInfo> modifier)
    {
        if (options.TypeInfoResolver is not DefaultJsonTypeInfoResolver resolver)
            throw new InvalidOperationException("TypeInfoResolver must be DefaultJsonTypeInfoResolver");

        resolver.Modifiers.Add(modifier);
        return options;
    }
}