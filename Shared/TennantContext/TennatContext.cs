using Microsoft.EntityFrameworkCore.Diagnostics;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Shared.TennantContext
{
    public record TenantIdentifiers(Guid EstateId,
                                    Guid MerchantId)
    {
        #region Methods

        public static TenantIdentifiers Default()
        {
            return new TenantIdentifiers(Guid.Empty, Guid.Empty);
        }

        #endregion
    }

    public class TenantContext
    {
        #region Fields

        private static readonly AsyncLocal<TenantContext> Current = new AsyncLocal<TenantContext>();

        #endregion

        #region Properties

        public Guid CorrelationId { get; internal set; } = Guid.NewGuid();

        public static TenantContext CurrentTenant
        {
            get => TenantContext.Current?.Value ?? new TenantContext();
            set => TenantContext.Current.Value = value;
        }

        public Guid EstateId { get; internal set; }

        public Boolean PerTenantLogsEnabled { get; internal set; }

        public Guid MerchantId { get; internal set; }

        #endregion
    }

    public static class TenantContextExtensionMethods
    {
        #region Methods

        public static void SetCorrelationId(this TenantContext context,
                                            Guid correlationId)
        {
            context.CorrelationId = correlationId;
        }

        public static void Initialise(this TenantContext tenantContext,
                                      TenantIdentifiers identifiers,
                                      Boolean perTenantLogsEnabled)
        {
            tenantContext.EstateId = identifiers.EstateId;
            tenantContext.MerchantId = identifiers.MerchantId;
            tenantContext.PerTenantLogsEnabled = perTenantLogsEnabled;
        }

        //public static void WriteExceptionToLog(this TenantContext tenantContext,
        //                                       Exception exception)
        //{
        //    using (MappedDiagnosticsLogicalContext.SetScoped("correlationId", $"Correlation ID: {tenantContext.CorrelationId.ToString()}"))
        //    {
        //        // Write to the normal log
        //        Logger.Logger.LogError(exception);

        //        if (tenantContext.PerTenantLogsEnabled && tenantContext.EstateId != Guid.Empty)
        //        {
        //            // Write to the tenant log
        //            using (MappedDiagnosticsLogicalContext.SetScoped("tenantId", $"_{tenantContext.EstateId.ToString()}"))
        //            {
        //                Logger.Logger.LogError(exception);
        //            }
        //        }
        //    }
        //}

        //public static void WriteMessageToLog(this TenantContext tenantContext,
        //                                     LogLevel logLevel,
        //                                     String message)
        //{
        //    using (MappedDiagnosticsLogicalContext.SetScoped("correlationId", $"Correlation ID: {tenantContext.CorrelationId.ToString()}"))
        //    {
        //        // Write to the normal log
        //        Logger.WriteToLog(LoggerCategory.General, eventType, message);

        //        if (tenantContext.PerTenantLogsEnabled && tenantContext.OrganisationId != Guid.Empty)
        //        {
        //            // Write to the tenant log
        //            using (MappedDiagnosticsLogicalContext.SetScoped("tenantId", $"_{tenantContext.OrganisationId.ToString()}"))
        //            {
        //                Logger.WriteToLog(LoggerCategory.General, eventType, message);
        //            }
        //        }
        //    }
        //}

        #endregion
    }
}
