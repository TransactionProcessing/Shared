using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Shared.EntityFramework;

public sealed class ResolvedDbContext<TContext> : IDisposable where TContext : DbContext
{
    public TContext Context { get; }
    private readonly IServiceScope _scope;

    public ResolvedDbContext(IServiceScope scope)
    {
        this._scope = scope ?? throw new ArgumentNullException(nameof(scope));
        this.Context = this._scope.ServiceProvider.GetRequiredService<TContext>();
    }

    public void Dispose() => this._scope.Dispose();
}