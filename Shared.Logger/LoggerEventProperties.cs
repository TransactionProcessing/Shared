namespace Shared.Logger;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Shared.Logger.TennantContext;

internal static class LoggerEventProperties
{
    public static Dictionary<String, Object?> Create(String severity)
    {
        Dictionary<String, Object?> properties = new(StringComparer.OrdinalIgnoreCase)
        {
            ["eventId"] = Guid.NewGuid(),
            ["timestampUtc"] = DateTimeOffset.UtcNow,
            ["serviceName"] = Assembly.GetEntryAssembly()?.GetName().Name ?? "UnknownService",
            ["severity"] = severity
        };

        TenantContext tenantContext = TenantContext.CurrentTenant;
        if (tenantContext != null)
        {
            Guid correlationId = tenantContext.CorrelationId.Value;
            if (correlationId != Guid.Empty)
            {
                properties["correlationId"] = correlationId;
            }

            if (tenantContext.EstateId != Guid.Empty)
            {
                properties["tenantId"] = tenantContext.EstateId;
            }

            if (tenantContext.MerchantId != Guid.Empty)
            {
                properties["merchantId"] = tenantContext.MerchantId;
            }
        }

        Activity? activity = Activity.Current;
        if (activity != null)
        {
            if (activity.TraceId != default)
            {
                properties["traceId"] = activity.TraceId.ToString();
            }

            if (activity.SpanId != default)
            {
                properties["spanId"] = activity.SpanId.ToString();
            }
        }

        return properties;
    }
}
