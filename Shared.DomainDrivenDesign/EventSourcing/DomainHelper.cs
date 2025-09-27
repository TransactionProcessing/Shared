namespace Shared.DomainDrivenDesign.EventSourcing;

using System;

/// <summary>
/// 
/// </summary>
public static class DomainHelper
{
    #region Methods

    /// <summary>
    /// Gets the name of the event type.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <returns></returns>
    internal static String GetEventTypeName(Type type)
    {
        return type.Name;
    }

    #endregion
}