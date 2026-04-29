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
        var fake = new FakeSerialiser();

        StringSerialiser.Initialise(fake);

        StringSerialiser.IsInitialised.ShouldBeTrue();
    }

    [Fact]
    public void StringSerialiser_Serialise_UsesProvidedSerializer()
    {
        var fake = new FakeSerialiser();
        StringSerialiser.Initialise(fake);

        var person = new Person { Name = "Alice", Age = 25 };

        var json = StringSerialiser.Serialise(person);

        fake.LastObj.ShouldBe(person);
        json.ShouldBe(fake.LastJson);
    }

    [Fact]
    public void StringSerialiser_Deserialize_ReturnsObject()
    {
        var fake = new FakeSerialiser();
        StringSerialiser.Initialise(fake);

        var expected = new Person { Name = "Dan", Age = 40 };
        var json = JsonSerializer.Serialize(expected);

        var result = StringSerialiser.Deserialize<Person>(json);

        result.Name.ShouldBe(expected.Name);
        result.Age.ShouldBe(expected.Age);
    }

    [Fact]
    public void StringSerialiser_Initialise_ReplacesPreviousSerializer()
    {
        var first = new FakeSerialiser();
        var second = new FakeSerialiser();

        StringSerialiser.Initialise(first);
        var p1 = new Person { Name = "X", Age = 1 };
        StringSerialiser.Serialise(p1);
        first.LastObj.ShouldBe(p1);

        StringSerialiser.Initialise(second);
        var p2 = new Person { Name = "Y", Age = 2 };
        StringSerialiser.Serialise(p2);
        second.LastObj.ShouldBe(p2);
    }

    private class FakeSerialiser : IStringSerialiser
    {
        public object? LastObj { get; private set; }
        public string? LastJson { get; private set; }

        public string Serialize<T>(T obj)
        {
            LastObj = obj!;
            LastJson = JsonSerializer.Serialize(obj);
            return LastJson!;
        }

        public T Deserialize<T>(string json)
        {
            LastJson = json;
            return JsonSerializer.Deserialize<T>(json)!;
        }
    }

    private class Person
    {
        public string Name { get; set; }
        public int Age { get; set; }
    }
}
