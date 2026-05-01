using System;

namespace Shared.Tests;

using System.Text.Json;
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
}
