namespace Shared.EventStore.Extensions
{
    using System;
    using global::EventStore.Client;
    using Grpc.Core.Interceptors;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using Microsoft.Extensions.Logging;

    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddEventStoreProjectionManagerClient(this IServiceCollection services, Action<EventStoreClientSettings>? configureSettings = null)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            EventStoreClientSettings settings = new EventStoreClientSettings();
            configureSettings?.Invoke(settings);

            services.TryAddSingleton(provider => {
                                         settings.LoggerFactory ??= provider.GetService<ILoggerFactory>();
                                         settings.Interceptors ??= provider.GetServices<Interceptor>();

                                         return new EventStoreProjectionManagementClient(settings);
                                     });

            return services;
        }
    }
}
