using System;
using System.Collections.Generic;
using System.Text.Json.Serialization.Metadata;

namespace Shared.Serialisation;

public static class JsonTypeInfoModifierExtensions
{
    /// <summary>
    /// Apply a modifier only to a specific type T
    /// </summary>
    public static Action<JsonTypeInfo> ForType<T>(Action<JsonTypeInfo> modifier)
    {
        return typeInfo =>
        {
            if (typeInfo.Type == typeof(T))
            {
                modifier(typeInfo);
            }
        };
    }

    /// <summary>
    /// Apply a modifier to multiple specific types
    /// </summary>
    public static Action<JsonTypeInfo> ForTypes(IEnumerable<Type> types, Action<JsonTypeInfo> modifier)
    {
        var set = new HashSet<Type>(types);

        return typeInfo =>
        {
            if (set.Contains(typeInfo.Type))
            {
                modifier(typeInfo);
            }
        };
    }

    /// <summary>
    /// Apply a modifier to a type and all types assignable to it (base class / interface)
    /// </summary>
    public static Action<JsonTypeInfo> ForAssignableTo<TBase>(Action<JsonTypeInfo> modifier)
    {
        return typeInfo =>
        {
            if (typeof(TBase).IsAssignableFrom(typeInfo.Type))
            {
                modifier(typeInfo);
            }
        };
    }

    /// <summary>
    /// Combine multiple modifiers into one
    /// </summary>
    public static Action<JsonTypeInfo> Combine(params Action<JsonTypeInfo>[] modifiers)
    {
        return typeInfo =>
        {
            foreach (var modifier in modifiers)
            {
                modifier(typeInfo);
            }
        };
    }
}