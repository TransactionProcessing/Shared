namespace Shared.EventStore;

using System;
using Shared.General;

internal static class EventStoreGrpcRetrySettings
{
    private const String SettingsSection = "AppSettings";
    private const String MaxAttemptsKey = "GrpcRetryMaxAttempts";
    private const String BaseDelayMillisecondsKey = "GrpcRetryBaseDelayMilliseconds";
    private const String MaxDelayMillisecondsKey = "GrpcRetryMaxDelayMilliseconds";
    private const String UseJitterKey = "GrpcRetryUseJitter";

    internal static Int32 MaxAttempts => Math.Max(1, ConfigurationReader.GetValueOrDefault(SettingsSection, MaxAttemptsKey, 5));

    internal static TimeSpan BaseDelay => TimeSpan.FromMilliseconds(Math.Max(1, ConfigurationReader.GetValueOrDefault(SettingsSection, BaseDelayMillisecondsKey, 100)));

    internal static TimeSpan MaxDelay => TimeSpan.FromMilliseconds(Math.Max(1, ConfigurationReader.GetValueOrDefault(SettingsSection, MaxDelayMillisecondsKey, 2000)));

    internal static Boolean UseJitter => ConfigurationReader.GetValueOrDefault(SettingsSection, UseJitterKey, true);
}
