#nullable enable
using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Shared.EntityFramework;

/// <summary>Options for dynamically resolved DbContexts.</summary>
public sealed class DbContextResolverOptions
{
    /// <summary>Gets a retry policy for a connection string key. Returning an empty options object disables retries.</summary>
    public Func<string, SqlServerRetryOptions?>? RetryOptionsFactory { get; init; }

    /// <summary>Gets interceptors to apply to contexts created with a database suffix.</summary>
    public IReadOnlyCollection<DbCommandInterceptor> Interceptors { get; init; } = Array.Empty<DbCommandInterceptor>();
}
