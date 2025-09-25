using System;
using Xunit;

namespace Shared.Tests;

using General;
using Shouldly;

public partial class SharedTests
{
    [Fact]
    public void TypeMap_AddType_TypeIsAdded(){
        TypeMap.AddType(typeof(String), "Test");

        TypeMap.Map.ContainsKey(typeof(String)).ShouldBeTrue();
        TypeMap.ReverseMap.ContainsKey("Test").ShouldBeTrue();
    }

    [Fact]
    public void TypeMap_AddTypeGeneric_TypeIsAdded()
    {
        TypeMap.AddType<String>("Test");

        TypeMap.Map.ContainsKey(typeof(String)).ShouldBeTrue();
        TypeMap.ReverseMap.ContainsKey("Test").ShouldBeTrue();
    }

    [Fact]
    public void TypeMap_GetType_TypeIsReturned()
    {
        TypeMap.AddType<String>("Test");

        var type = TypeMap.GetType("Test");
        type.FullName.ShouldBe("System.String");
    }

    [Fact]
    public void TypeMap_GetTypeNameGemeric_TypeNameIsReturned()
    {
        TypeMap.AddType<String>("Test");

        var typeName = TypeMap.GetTypeName<String>();
        typeName.ShouldBe("Test");
    }

    [Fact]
    public void TypeMap_GetTypeName_TypeNameIsReturned()
    {
        TypeMap.AddType<String>("Test");

        var typeName = TypeMap.GetTypeName(String.Empty);
        typeName.ShouldBe("Test");
    }
}