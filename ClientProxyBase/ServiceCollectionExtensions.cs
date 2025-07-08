using Microsoft.Extensions.DependencyInjection;
namespace ClientProxyBase;

public static class ServiceCollectionExtensions
{
    public static IHttpClientBuilder RegisterHttpClient<TInterface, TImplementation>(
        this IServiceCollection services)
        where TInterface : class
        where TImplementation : class, TInterface
    {
        services.AddTransient<CorrelationIdHandler>();

        services.AddHttpClient<TInterface, TImplementation>()
            .AddHttpMessageHandler<CorrelationIdHandler>()
            .ConfigurePrimaryHttpMessageHandler(GetSocketsHttpHandler.GetHandler);

        return services.AddHttpClient<TImplementation>();
    }
}