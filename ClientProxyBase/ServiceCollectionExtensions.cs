using Microsoft.Extensions.DependencyInjection;

namespace ClientProxyBase;

public static class ServiceCollectionExtensions
{
    public static IHttpClientBuilder RegisterHttpClient(this IServiceCollection services, string name = "") {
        return services.AddHttpClient(name).AddHttpMessageHandler<CorrelationIdHandler>()
            .ConfigurePrimaryHttpMessageHandler(GetSocketsHttpHandler.GetHandler);
    }
}