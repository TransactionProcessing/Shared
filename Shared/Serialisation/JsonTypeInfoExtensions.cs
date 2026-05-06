using System;
using System.Linq;
using System.Linq.Expressions;
using System.Text.Json.Serialization.Metadata;

namespace Shared.Serialisation;

public static class JsonTypeInfoExtensions
{
    public static void IgnoreProperty<T>(
        this JsonTypeInfo typeInfo,
        Expression<Func<T, object>> selector)
    {
        var name = ExpressionHelpers.GetPropertyName(selector);

        var prop = typeInfo.Properties.FirstOrDefault(p =>
            string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));

        if (prop != null)
        {
            prop.ShouldSerialize = (_, _) => false;
        }
    }

    public static void RenameProperty<T>(
        this JsonTypeInfo typeInfo,
        Expression<Func<T, object>> selector,
        string newName)
    {
        var name = ExpressionHelpers.GetPropertyName(selector);

        var prop = typeInfo.Properties.FirstOrDefault(p =>
            string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));

        if (prop != null)
        {
            prop.Name = newName;
        }
    }
}