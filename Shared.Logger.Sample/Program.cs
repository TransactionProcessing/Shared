using NLog;
using Shared.Logger.TennantContext;
using StaticLogger = Shared.Logger.Logger;

Boolean generateHighVolumeLogs = true;
const Int32 HighVolumeLogEventCount = 1000;

LogManager.Setup().LoadConfigurationFromFile("NLog.config");

NLog.Logger nlogLogger = LogManager.GetLogger("Shared.Logger.Sample");
StaticLogger.Initialise(nlogLogger, "shared-logger-sample");

// Simulate the tenant context normally populated by the service middleware.
TenantContext tenantContext = new();
tenantContext.Initialise(
    new TenantIdentifiers(
        EstateId: Guid.Parse("1e543b2a-d100-4ec1-9735-8590aab2c819"),
        MerchantId: Guid.Parse("579267d9-46a6-4057-8c66-0b34d7494a28")),
    perTenantLogsEnabled: false);
tenantContext.SetCorrelationId(CorrelationId.From(Guid.Parse("b0354c7e-b6de-4a27-9a54-715dc3e56566")));
TenantContext.CurrentTenant = tenantContext;

Guid transactionId = Guid.NewGuid();
StaticLogger.LogTrace("Starting sample run {runId}", Guid.NewGuid());
StaticLogger.LogDebug("Loaded {recordCount} records for {tenantName}", 12, "Example Estate");
StaticLogger.LogInformation("Processed transaction {transactionId} for {amount} {currency}", transactionId, 125.50m, "GBP");
StaticLogger.LogWarning("Transaction {transactionId} required a retry after {attemptCount} attempts", transactionId, 2);

try
{
    throw new InvalidOperationException("Example failure for the structured exception log.");
}
catch (InvalidOperationException exception)
{
    StaticLogger.LogError(exception, "Could not process transaction {transactionId}", transactionId);
}

StaticLogger.LogCritical("Sample critical event for {component}", "payment processor");

if (generateHighVolumeLogs)
{
    for (Int32 eventNumber = 1; eventNumber <= HighVolumeLogEventCount; eventNumber++)
    {
        TenantContext volumeTenantContext = new();
        volumeTenantContext.Initialise(
            new TenantIdentifiers(EstateId: Guid.NewGuid(), MerchantId: Guid.NewGuid()),
            perTenantLogsEnabled: true);
        volumeTenantContext.SetCorrelationId(CorrelationId.From(Guid.NewGuid()));
        TenantContext.CurrentTenant = volumeTenantContext;

        StaticLogger.LogInformation(
            "Volume sample event {eventNumber} of {totalEvents} for transaction {transactionId} with amount {amount}",
            eventNumber,
            HighVolumeLogEventCount,
            Guid.NewGuid(),
            (eventNumber % 500) + 0.25m);
    }
}

TenantContext.CurrentTenant = new TenantContext();
LogManager.Shutdown();

Console.WriteLine($"Structured log entries written under: {Path.Combine(AppContext.BaseDirectory, "logs")}");
