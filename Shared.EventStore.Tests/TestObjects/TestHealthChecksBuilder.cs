namespace Shared.EventStore.Tests.TestObjects;

using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

public class TestHealthChecksBuilder : IHealthChecksBuilder
{
    public List<HealthCheckRegistration> Registrations = new List<HealthCheckRegistration>();

    public TestHealthChecksBuilder()
    {
        Services = new ServiceCollection();
    }

    public IHealthChecksBuilder Add(HealthCheckRegistration registration)
    {
        Registrations.Add(registration);
        return this;
    }

    public IServiceCollection Services { get; }
}