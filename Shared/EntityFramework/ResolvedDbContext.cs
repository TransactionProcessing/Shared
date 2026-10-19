using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Shared.EntityFramework;

public sealed class ResolvedDbContext<TContext> : IDisposable where TContext : DbContext
{
    public TContext Context { get; }
    private readonly IServiceScope _scope;
    private readonly bool _ownsContext;
    private bool _disposed;

    public ResolvedDbContext(IServiceScope scope)
    {
        this._scope = scope ?? throw new ArgumentNullException(nameof(scope));
        this.Context = this._scope.ServiceProvider.GetRequiredService<TContext>();
    }

    internal ResolvedDbContext(IServiceScope scope, TContext context)
    {
        this._scope = scope ?? throw new ArgumentNullException(nameof(scope));
        this.Context = context ?? throw new ArgumentNullException(nameof(context));
        this._ownsContext = true;
    }

    public void Dispose()
    {
        if (this._disposed)
            return;

        this._disposed = true;
        try
        {
            if (this._ownsContext)
                this.Context.Dispose();
        }
        finally
        {
            this._scope.Dispose();
        }
    }
}
