namespace Shared.EventStore.Aggregate;

using System;
using System.Linq;
using System.Text;
using global::EventStore.Client;

/// <summary>
/// 
/// </summary>
public static class ResolvedEventExtensions
{
    #region Methods

    /// <summary>
    /// Gets the resolved event data as string.
    /// </summary>
    /// <param name="resolvedEvent">The resolved event.</param>
    /// <returns></returns>
    public static String GetResolvedEventDataAsString(this ResolvedEvent resolvedEvent)
    {
        return Encoding.Default.GetString(resolvedEvent.Event.Data.ToArray(), 0, resolvedEvent.Event.Data.Length);
    }
        
    #endregion
}