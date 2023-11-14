namespace Shared.EventStore.Tests.TestObjects;

using System;
using ProjectionEngine;

public record TestState : State
{
    public string Name { get; set; }
}