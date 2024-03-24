namespace Shared.Extensions
{
    using System;
    using System.Net.Http;
    using General;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Diagnostics.HealthChecks;

    /// <summary>
    /// 
    /// </summary>
    public static class HealthChecksBuilderExtensions
    {
        #region Methods

        /// <summary>
        /// Adds the estate management service.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="customHttpHandler">The custom HTTP handler.</param>
        /// <returns></returns>
        public static IHealthChecksBuilder AddEstateManagementService(this IHealthChecksBuilder builder,
                                                                      Func<IServiceProvider, HttpClientHandler> customHttpHandler = null)
        {
            Uri uri = new Uri($"{ConfigurationReader.GetValue("AppSettings", "EstateManagementApi")}/health");

            return builder.AddUrlGroup(uri,
                                       HttpMethod.Get,
                                       "Estate Management Service",
                                       HealthStatus.Unhealthy,
                                       new[] {"estatemanagement"},
                                       configurePrimaryHttpMessageHandler:customHttpHandler);
        }

        
        /// <summary>
        /// Adds the file processor service.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="customHttpHandler">The custom HTTP handler.</param>
        /// <returns></returns>
        public static IHealthChecksBuilder AddFileProcessorService(this IHealthChecksBuilder builder,
                                                                   Func<IServiceProvider, HttpClientHandler> customHttpHandler = null)
        {
            Uri uri = new Uri($"{ConfigurationReader.GetValue("AppSettings", "FileProcessorApi")}/health");

            return builder.AddUrlGroup(uri,
                                       name:"File Processor Service",
                                       httpMethod:HttpMethod.Get,
                                       failureStatus:HealthStatus.Unhealthy,
                                       tags:new[] {"fileprocessing"},
                                       configurePrimaryHttpMessageHandler: customHttpHandler);
        }

        /// <summary>
        /// Adds the messaging service.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="customHttpHandler">The custom HTTP handler.</param>
        /// <returns></returns>
        public static IHealthChecksBuilder AddMessagingService(this IHealthChecksBuilder builder,
                                                               Func<IServiceProvider, HttpClientHandler> customHttpHandler = null)
        {
            Uri uri = new Uri($"{ConfigurationReader.GetValue("AppSettings", "MessagingServiceApi")}/health");

            return builder.AddUrlGroup(uri,
                                       HttpMethod.Get,
                                       "Messaging Service",
                                       HealthStatus.Unhealthy,
                                       new[] {"messaging", "email", "sms"},
                                       configurePrimaryHttpMessageHandler: customHttpHandler);
        }

        /// <summary>
        /// Adds the security service.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="customHttpHandler">The custom HTTP handler.</param>
        /// <returns></returns>
        public static IHealthChecksBuilder AddSecurityService(this IHealthChecksBuilder builder,
                                                              Func<IServiceProvider, HttpClientHandler> customHttpHandler = null)
        {
            Uri uri = new Uri($"{ConfigurationReader.GetValue("SecurityConfiguration", "Authority")}/health");
            return builder.AddUrlGroup(uri,
                                       HttpMethod.Get,
                                       "Security Service",
                                       HealthStatus.Unhealthy,
                                       new[] {"security", "authorisation"},
                                       configurePrimaryHttpMessageHandler: customHttpHandler);
        }

        /// <summary>
        /// Adds the transaction processor service.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="customHttpHandler">The custom HTTP handler.</param>
        /// <returns></returns>
        public static IHealthChecksBuilder AddTransactionProcessorService(this IHealthChecksBuilder builder,
                                                                          Func<IServiceProvider, HttpClientHandler> customHttpHandler = null)
        {
            Uri uri = new Uri($"{ConfigurationReader.GetValue("AppSettings", "TransactionProcessorApi")}/health");

            return builder.AddUrlGroup(uri,
                                       name:"Transaction Processor Service",
                                       httpMethod:HttpMethod.Get,
                                       failureStatus:HealthStatus.Unhealthy,
                                       tags:new[] {"transactionprocessing"},
                                       configurePrimaryHttpMessageHandler: customHttpHandler);
        }
        
        #endregion
    }
}