namespace Shared.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Shared.General;
using Shared.Serialisation;
using Shouldly;
using Xunit;

public class SerializationCoverageTests
{
    [Fact]
    public void GetPropertyName_ReturnsMemberNameForReferenceAndValueProperties()
    {
        ExpressionHelpers.GetPropertyName<SerializationPerson>(person => person.Name)
            .ShouldBe(nameof(SerializationPerson.Name));
        ExpressionHelpers.GetPropertyName<SerializationPerson>(person => person.Age)
            .ShouldBe(nameof(SerializationPerson.Age));
    }

    [Fact]
    public void GetPropertyName_RejectsNonMemberExpressions()
    {
        Should.Throw<ArgumentException>(() =>
            ExpressionHelpers.GetPropertyName<SerializationPerson>(_ => new object()));
    }

    [Fact]
    public void AddModifier_AddsModifierToDefaultResolver()
    {
        JsonSerializerOptions options = new()
        {
            TypeInfoResolver = new DefaultJsonTypeInfoResolver()
        };
        Boolean called = false;

        options.AddModifier(_ => called = true);
        options.GetTypeInfo(typeof(SerializationPerson));

        called.ShouldBeTrue();
    }

    [Fact]
    public void AddModifier_RejectsNonDefaultResolver()
    {
        JsonSerializerOptions options = new() { TypeInfoResolver = new JsonSerializerContextResolver() };

        Should.Throw<InvalidOperationException>(() => options.AddModifier(_ => { }));
    }

    [Fact]
    public void ConfigureMinimalApi_CopiesDefaultsAndAddsDateConverter()
    {
        JsonSerializerOptions options = new();

        JsonSerializerConfiguration.ConfigureMinimalApi(options);

        options.PropertyNamingPolicy.ShouldBe(JsonNamingPolicy.SnakeCaseLower);
        options.WriteIndented.ShouldBeTrue();
        options.Converters.ShouldContain(converter => converter is DateTimeSpaceConverter);
    }

    [Fact]
    public void JsonTypeInfoExtensions_CanIgnoreAndRenameProperties()
    {
        JsonSerializerOptions options = new()
        {
            TypeInfoResolver = new DefaultJsonTypeInfoResolver()
        };
        JsonTypeInfo typeInfo = options.GetTypeInfo(typeof(SerializationPerson));

        typeInfo.IgnoreProperty<SerializationPerson>(person => person.Name);
        typeInfo.RenameProperty<SerializationPerson>(person => person.Age, "years");
        typeInfo.IgnoreProperty<OtherPerson>(person => person.NotPresent);
        typeInfo.RenameProperty<OtherPerson>(person => person.NotPresent, "missing");
        typeInfo.Properties.Single(property => property.Name == nameof(SerializationPerson.Name))
            .ShouldSerialize(new SerializationPerson(), null)
            .ShouldBeFalse();

        typeInfo.Properties.Any(property => property.Name == nameof(SerializationPerson.Name)
            && property.ShouldSerialize is not null).ShouldBeTrue();
        typeInfo.Properties.Any(property => property.Name == "years").ShouldBeTrue();
    }

    [Fact]
    public void JsonTypeInfoModifierExtensions_ApplyExpectedModifiers()
    {
        JsonSerializerOptions options = new()
        {
            TypeInfoResolver = new DefaultJsonTypeInfoResolver()
        };
        JsonTypeInfo personTypeInfo = options.GetTypeInfo(typeof(SerializationPerson));
        JsonTypeInfo derivedTypeInfo = options.GetTypeInfo(typeof(DerivedSerializationPerson));
        Int32 callCount = 0;

        JsonTypeInfoModifierExtensions.ForType<SerializationPerson>(_ => callCount++)(personTypeInfo);
        JsonTypeInfoModifierExtensions.ForType<SerializationPerson>(_ => callCount += 100)(derivedTypeInfo);
        JsonTypeInfoModifierExtensions.ForTypes(
            new[] { typeof(SerializationPerson) },
            _ => callCount += 2)(personTypeInfo);
        JsonTypeInfoModifierExtensions.ForTypes(
            new[] { typeof(DerivedSerializationPerson) },
            _ => callCount += 200)(personTypeInfo);
        JsonTypeInfoModifierExtensions.ForAssignableTo<SerializationPerson>(_ => callCount += 3)(derivedTypeInfo);
        JsonTypeInfoModifierExtensions.ForAssignableTo<DerivedSerializationPerson>(_ => callCount += 300)(personTypeInfo);
        JsonTypeInfoModifierExtensions.Combine(_ => callCount += 4, _ => callCount += 5)(personTypeInfo);

        callCount.ShouldBe(15);
    }

    [Theory]
    [InlineData("PascalCase", "pascal_case")]
    [InlineData("HTTPServer", "h_t_t_p_server")]
    [InlineData("name", "name")]
    public void SnakeCaseNamingPolicy_ConvertsNames(String input, String expected)
    {
        new SnakeCaseNamingPolicy().ConvertName(input).ShouldBe(expected);
    }

    private class SerializationPerson
    {
        public String Name { get; set; }

        public Int32 Age { get; set; }

    }

    private sealed class DerivedSerializationPerson : SerializationPerson;

    private sealed class OtherPerson
    {
        public String NotPresent => String.Empty;
    }

    private sealed class JsonSerializerContextResolver : IJsonTypeInfoResolver
    {
        public JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
        {
            return null;
        }
    }
}
