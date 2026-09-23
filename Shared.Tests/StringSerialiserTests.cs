using System;

namespace Shared.Tests;

using System.Text.Json;
using System.Text.Json.Serialization;
using Shared.Serialisation;
using Shouldly;
using Xunit;

public partial class SharedTests
{
    [Fact]
    public void StringSerialiser_Initialise_SetsIsInitialised()
    {
        var serializer = new SystemTextJsonSerializer(new JsonSerializerOptions());

        StringSerialiser.Initialise(serializer);

        StringSerialiser.IsInitialised.ShouldBeTrue();
    }

    [Fact]
    public void StringSerialiser_Serialise_UsesProvidedSerializer()
    {
        var serializer = new SystemTextJsonSerializer(new JsonSerializerOptions());

        StringSerialiser.Initialise(serializer);

        var person = new Person { Name = "Alice", Age = 25 };

        var json = StringSerialiser.Serialise(person);

        json.ShouldContain("\"Name\":\"Alice\"");
        json.ShouldContain("\"Age\":25");
    }

    [Fact]
    public void StringSerialiser_Serialise_InterfaceReference_UsesRuntimeType()
    {
        var serializer = new SystemTextJsonSerializer(new JsonSerializerOptions());

        StringSerialiser.Initialise(serializer);

        ITestPerson person = new DetailedPerson { Name = "Alice", Age = 25 };

        var json = StringSerialiser.Serialise(person);

        json.ShouldContain("\"Name\":\"Alice\"");
        json.ShouldContain("\"Age\":25");
    }

    [Fact]
    public void StringSerialiser_Deserialize_ReturnsObject()
    {
        var serializer = new SystemTextJsonSerializer(new JsonSerializerOptions());

        StringSerialiser.Initialise(serializer);

        var expected = new Person { Name = "Dan", Age = 40 };
        var json = JsonSerializer.Serialize(expected);

        var result = StringSerialiser.Deserialise<Person>(json);

        result.Name.ShouldBe(expected.Name);
        result.Age.ShouldBe(expected.Age);
    }

    [Fact]
    public void StringSerialiser_Methods_ThrowWhenNotInitialised()
    {
        StringSerialiser.IsInitialised = false;

        Should.Throw<InvalidOperationException>(() => StringSerialiser.Serialise(new Person()));
        Should.Throw<InvalidOperationException>(() => StringSerialiser.Deserialise<Person>("{}"));
        Should.Throw<InvalidOperationException>(() => StringSerialiser.DeserialiseAnonymousType("{}", new Person()));
        Should.Throw<InvalidOperationException>(() => StringSerialiser.DeserializeObject<Person>("{}", typeof(Person)));
        Should.Throw<InvalidOperationException>(() => StringSerialiser.GetValue<String>("{}", "Name"));

        TestHelpers.InitialiseStringSerialiser();
    }

    [Fact]
    public void StringSerialiser_DeserialiseAnonymousType_ReturnsObject()
    {
        TestHelpers.InitialiseStringSerialiser();

        Person result = StringSerialiser.DeserialiseAnonymousType(
            "{\"Name\":\"Alice\",\"Age\":25}",
            new Person());

        result.Name.ShouldBe("Alice");
        result.Age.ShouldBe(25);
    }

    [Fact]
    public void StringSerialiser_DeserializeObject_ReturnsObject()
    {
        TestHelpers.InitialiseStringSerialiser();

        Person result = StringSerialiser.DeserializeObject<Person>(
            "{\"Name\":\"Alice\",\"Age\":25}",
            typeof(Person));

        result.Name.ShouldBe("Alice");
        result.Age.ShouldBe(25);
    }

    [Theory]
    [InlineData(SerialiserPropertyFormat.CamelCase, "firstName")]
    [InlineData(SerialiserPropertyFormat.SnakeCase, "first_name")]
    [InlineData(SerialiserPropertyFormat.CamelCaseUpper, "FIRST_NAME")]
    [InlineData(SerialiserPropertyFormat.KebabCase, "first-name")]
    [InlineData(SerialiserPropertyFormat.KeabCaseUpper, "FIRST-NAME")]
    public void SystemTextJsonSerializer_Serialise_UsesRequestedPropertyFormat(
        SerialiserPropertyFormat propertyFormat,
        String expectedPropertyName)
    {
        SystemTextJsonSerializer serializer = new(new JsonSerializerOptions());

        String json = serializer.Serialize(
            new NamedPerson { FirstName = "Alice" },
            new SerialiserOptions(propertyFormat));

        json.ShouldContain($"\"{expectedPropertyName}\"");
    }

    [Fact]
    public void SystemTextJsonSerializer_GetValue_FindsNestedArrayValue()
    {
        SystemTextJsonSerializer serializer = new(new JsonSerializerOptions());

        Int32? value = serializer.GetValue<Int32>(
            "{\"items\":[{\"name\":\"first\"},{\"name\":\"second\",\"value\":42}]}",
            "value");

        value.ShouldBe(42);
    }

    [Fact]
    public void SystemTextJsonSerializer_GetValue_ReturnsDefaultForMissingOrInvalidValues()
    {
        SystemTextJsonSerializer serializer = new(new JsonSerializerOptions());

        serializer.GetValue<Int32>("{\"name\":\"Alice\"}", "missing").ShouldBe(0);
        serializer.GetValue<Int32>("{\"value\":\"not-a-number\"}", "value").ShouldBe(0);
    }

    [Fact]
    public void SystemTextJsonSerializer_GetValue_ReturnsDefaultWhenArrayDoesNotContainProperty()
    {
        SystemTextJsonSerializer serializer = new(new JsonSerializerOptions());

        serializer.GetValue<Int32>("{\"items\":[{\"name\":\"first\"}]}", "missing").ShouldBe(0);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("\"\"")]
    [InlineData("\"not-a-date\"")]
    public void DateTimeSpaceConverter_Read_HandlesInvalidDateValues(String json)
    {
        JsonSerializerOptions options = new();
        options.Converters.Add(new DateTimeSpaceConverter());

        if (json is "null" or "\"\"")
        {
            JsonSerializer.Deserialize<DateTime>(json, options).ShouldBe(default(DateTime));
        }
        else
        {
            Should.Throw<JsonException>(() => JsonSerializer.Deserialize<DateTime>(json, options));
        }
    }

    [Fact]
    public void DateTimeSpaceConverter_Read_HandlesUnixSeconds()
    {
        JsonSerializerOptions options = new();
        options.Converters.Add(new DateTimeSpaceConverter());

        DateTime result = JsonSerializer.Deserialize<DateTime>("0", options);

        result.ShouldBe(DateTimeOffset.FromUnixTimeSeconds(0).LocalDateTime);
    }

    [Fact]
    public void DateTimeSpaceConverter_Write_UsesSpaceSeparatedFormat()
    {
        JsonSerializerOptions options = new();
        options.Converters.Add(new DateTimeSpaceConverter());

        String json = JsonSerializer.Serialize(
            new DateTime(2026, 5, 7, 6, 3, 18),
            options);

        json.ShouldBe("\"2026-05-07 06:03:18\"");
    }

    [Fact]
    public void SystemTextJsonSerializer_DefaultOptions_HideEventMetadata()
    {
        SystemTextJsonSerializer serializer = new(
            SystemTextJsonSerializer.GetDefaultJsonSerializerOptions());

        String json = serializer.Serialize(new EventMetadataPerson
        {
            Name = "Alice",
            AggregateId = Guid.NewGuid(),
            EventType = "Created"
        });

        json.ShouldContain("name");
        json.ShouldNotContain("aggregate_id");
        json.ShouldNotContain("event_type");
    }

    [Fact]
    public void SystemTextJsonSerializer_Serialise_WithIndentedOptionsWritesIndentedJson()
    {
        SystemTextJsonSerializer serializer = new(new JsonSerializerOptions());

        String json = serializer.Serialize(
            new NamedPerson { FirstName = "Alice" },
            new SerialiserOptions(SerialiserPropertyFormat.CamelCase, true, true));

        json.ShouldContain(Environment.NewLine);
        json.ShouldContain("firstName");
    }

    [Fact]
    public void SystemTextJsonSerializer_Serialise_WithUnknownPropertyFormatKeepsExistingPolicy()
    {
        SystemTextJsonSerializer serializer = new(new JsonSerializerOptions());

        String json = serializer.Serialize(
            new NamedPerson { FirstName = "Alice" },
            new SerialiserOptions((SerialiserPropertyFormat)999));

        json.ShouldContain("FirstName");
    }

    [Fact]
    public void SystemTextJsonSerializer_Serialise_NullReturnsJsonNull()
    {
        SystemTextJsonSerializer serializer = new(new JsonSerializerOptions());

        serializer.Serialize<NamedPerson>(null).ShouldBe("null");
    }

    [Fact]
    public void SystemTextJsonSerializer_Serialise_CanKeepNullValues()
    {
        SystemTextJsonSerializer serializer = new(new JsonSerializerOptions());

        String json = serializer.Serialize(
            new NamedPerson { FirstName = null },
            new SerialiserOptions(SerialiserPropertyFormat.CamelCase, false));

        json.ShouldContain("firstName");
    }

    [Fact]
    public void StringSerialiser_GetValue_UsesConfiguredSerializer()
    {
        TestHelpers.InitialiseStringSerialiser();

        StringSerialiser.GetValue<Int32>("{\"value\":42}", "value").ShouldBe(42);
    }

    [Theory]
    [InlineData("2026-05-07 06:03:18")]
    [InlineData("2026-05-07T06:03:18")]
    [InlineData("2026-05-07")]
    public void DateTimeSpaceConverter_Read_HandlesExactAndGeneralDateFormats(String value)
    {
        JsonSerializerOptions options = new();
        options.Converters.Add(new DateTimeSpaceConverter());

        Should.NotThrow(() => JsonSerializer.Deserialize<DateTime>($"\"{value}\"", options));
    }

    [Fact]
    public void DateTimeSpaceConverter_Read_RejectsUnexpectedToken()
    {
        JsonSerializerOptions options = new();
        options.Converters.Add(new DateTimeSpaceConverter());

        Should.Throw<JsonException>(() => JsonSerializer.Deserialize<DateTime>("true", options));
    }
    
    private class Person
    {
        public string Name { get; set; }
        public int Age { get; set; }
    }

    private interface ITestPerson
    {
        string Name { get; set; }
    }

    private class DetailedPerson : ITestPerson
    {
        public string Name { get; set; }
        public int Age { get; set; }
    }

    private class NamedPerson
    {
        public String FirstName { get; set; }
    }

    private class EventMetadataPerson
    {
        public String Name { get; set; }

        [JsonPropertyName("AggregateId")]
        public Guid AggregateId { get; set; }

        [JsonPropertyName("EventType")]
        public String EventType { get; set; }
    }
}
