using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using Microsoft.Data.SqlClient;

namespace Shared.IntegrationTesting.TestContainers;

public sealed class SqlServerTestRunFixture : IAsyncDisposable
{
    private const String SqlServerImage = "mcr.microsoft.com/mssql/server:2025-latest";
    private const String SqlServerPassword = "thisisalongpassword123!";
    private const Int32 HealthCheckAttempts = 10;

    private readonly IContainer container;
    private readonly INetwork network;
    private Boolean IsDisposed;

    private SqlServerTestRunFixture(
        String runId,
        DateTimeOffset startedAt,
        String containerName,
        IContainer container,
        INetwork network)
    {
        this.RunId = runId;
        this.StartedAt = startedAt;
        this.ContainerName = containerName;
        this.container = container;
        this.network = network;
        this.Network = network;
        this.UserName = "sa";
        this.Password = SqlServerPassword;
    }

    public String ContainerName { get; }

    public String RunId { get; }

    public DateTimeOffset StartedAt { get; }

    public UInt16 MappedPort { get; private set; }

    public INetwork Network { get; }

    public String UserName { get; }

    public String Password { get; }

    public static async Task<SqlServerTestRunFixture> StartAsync(CancellationToken cancellationToken = default)
    {
        String runId = Guid.NewGuid().ToString("N");
        DateTimeOffset startedAt = DateTimeOffset.Now;
        String containerName = $"shared-sqlserver-{runId}";
        INetwork network = new NetworkBuilder()
            .WithName($"shared-test-run-{runId}")
            .WithLabel("test-run-id", runId)
            .Build();
        IContainer container = new ContainerBuilder(SqlServerImage)
            .WithName(containerName)
            .WithNetwork(network)
            .WithNetworkAliases(containerName)
            .WithEnvironment("ACCEPT_EULA", "Y")
            .WithEnvironment("SA_PASSWORD", SqlServerPassword)
            .WithPortBinding(DockerPorts.SqlServerDockerPort, true)
            .Build();
        SqlServerTestRunFixture fixture = new(runId, startedAt, containerName, container, network);

        try
        {
            await container.StartAsync(cancellationToken).ConfigureAwait(false);
            fixture.MappedPort = container.GetMappedPublicPort(DockerPorts.SqlServerDockerPort);
            await fixture.WaitForReadiness(cancellationToken).ConfigureAwait(false);
            return fixture;
        }
        catch (Exception startupException)
        {
            try
            {
                await fixture.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception cleanupException)
            {
                throw new AggregateException(
                    "SQL Server test-run fixture startup failed and cleanup also failed.",
                    startupException,
                    cleanupException);
            }

            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (this.IsDisposed)
        {
            return;
        }

        this.IsDisposed = true;
        List<Exception> cleanupErrors = [];

        try
        {
            await this.container.DisposeAsync().ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            cleanupErrors.Add(exception);
        }

        try
        {
            await this.network.DeleteAsync(CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            cleanupErrors.Add(exception);
        }

        try
        {
            await this.network.DisposeAsync().ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            cleanupErrors.Add(exception);
        }

        if (cleanupErrors.Count > 0)
        {
            throw new AggregateException("Failed to clean up SQL Server test-run resources.", cleanupErrors);
        }
    }

    private async Task WaitForReadiness(CancellationToken cancellationToken)
    {
        String connectionString = new SqlConnectionStringBuilder
        {
            DataSource = $"127.0.0.1,{this.MappedPort}",
            InitialCatalog = "master",
            UserID = "sa",
            Password = this.Password,
            Encrypt = false,
            ConnectTimeout = 5
        }.ConnectionString;

        SqlException lastException = null;
        for (Int32 attempt = 1; attempt <= HealthCheckAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await using SqlConnection connection = new(connectionString);
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
                await using SqlCommand command = new("SELECT 1", connection);
                await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
                return;
            }
            catch (SqlException exception) when (attempt < HealthCheckAttempts)
            {
                lastException = exception;
                await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken).ConfigureAwait(false);
            }
            catch (SqlException exception)
            {
                lastException = exception;
            }
        }

        throw new InvalidOperationException(
            $"SQL Server did not become ready after {HealthCheckAttempts} health checks.",
            lastException);
    }
}
