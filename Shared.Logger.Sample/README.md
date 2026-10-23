# Shared.Logger sample

This console app demonstrates structured message-template calls through `Shared.Logger` and NLog. NLog reads [NLog.config](NLog.config) and writes JSON lines with event properties included, so template values and tenant/correlation metadata are available as separate fields. The tenant scope controls file routing: the tenant-scoped event goes into its tenant-specific subfolder, while the regular event goes into `logs/shared`.

Run it from the repository root:

```powershell
dotnet run --project Shared.Logger.Sample
```

The sample writes under `logs` in the app's output directory. With per-tenant logging enabled, each logger call produces the regular event and the tenant-scoped event, which this config routes separately.

To generate additional volume, set `generateHighVolumeLogs` to `true` near the top of `Program.cs`. It then writes 1,000 more structured events, each with a fresh correlation ID, estate ID, and merchant ID (and each routed to the regular and tenant-specific files); change `HighVolumeLogEventCount` to adjust the batch size.
