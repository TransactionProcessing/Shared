namespace Shared.EventStore.ProjectionEngine;

using System;
using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public record State{
    #region Properties

    public Boolean ChangesApplied{ get; init; }
    public Boolean IsInitialised => this.Version != null;
    public Boolean IsNotInitialised => this.Version == null;
    public Byte[] Version{ get; init; }

    #endregion
}