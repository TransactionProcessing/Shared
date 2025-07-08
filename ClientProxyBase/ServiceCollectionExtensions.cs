using Microsoft.Extensions.DependencyInjection;

namespace ClientProxyBase;

public static class ServiceCollectionExtensions
{
    public static IHttpClientBuilder RegisterHttpClient<T>(this IServiceCollection services) where T : class {
        return services.AddHttpClient<T>().AddHttpMessageHandler<CorrelationIdHandler>()
            .ConfigurePrimaryHttpMessageHandler(GetSocketsHttpHandler.GetHandler);
    }
}