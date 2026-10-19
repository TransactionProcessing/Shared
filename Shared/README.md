# Shared Entity Framework helpers

## Resolving a context for a suffixed database

`DbContextResolver<TContext>` keeps the existing unsuffixed resolution behavior. For a database-per-tenant or database-per-estate context, pass a suffix; the resolver rewrites the connection string's initial catalog and creates the requested generic context within a scope of the application's service provider.

```csharp
DbContextResolverOptions resolverOptions = new()
{
    RetryOptionsFactory = key => configuration
        .GetSection($"AppSettings:SqlServerRetry:{key}")
        .Get<SqlServerRetryOptions>(),
    Interceptors = [queryTimingInterceptor]
};

IDbContextResolver<MyDbContext> resolver = new DbContextResolver<MyDbContext>(
    serviceProvider,
    configuration,
    resolverOptions);

using ResolvedDbContext<MyDbContext> resolved = resolver.Resolve("ReadModel", "estate-123");
// Use resolved.Context while the wrapper is in scope.
```

When `RetryOptionsFactory` returns `null` or is omitted, suffixed contexts use SQL Server's standard retry values (6 retries, 30 seconds maximum delay). Return an empty `SqlServerRetryOptions` instance to disable retries. The factory receives the connection string key, so read and write databases can use different policies.

## Measuring slow EF commands

The shared `QueryTimingInterceptor` logs commands taking at least 500 ms. It measures sync and async reader, scalar, and non-query commands. SQL text is omitted by default; setting `IncludeCommandText` opts into logging the command text.

```csharp
QueryTimingInterceptor queryTimingInterceptor = new(
    loggerFactory.CreateLogger<QueryTimingInterceptor>(),
    new QueryTimingInterceptorOptions
    {
        ThresholdMilliseconds = 500,
        IncludeCommandText = false
    });

optionsBuilder.AddInterceptors(queryTimingInterceptor);
```

Register the interceptor with EF Core's `AddInterceptors` for ordinary DI-created contexts. To use it for suffixed contexts resolved through `DbContextResolver<TContext>`, include it in `DbContextResolverOptions.Interceptors`.
