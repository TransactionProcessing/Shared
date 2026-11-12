using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace TransactionProcessing.UnionResults.AspNetCore;

public static class UnionResultServiceCollectionExtensions
{
    public static IServiceCollection AddUnionResults(this IServiceCollection services, Action<UnionResultOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddProblemDetails();
        services.AddOptions<UnionResultOptions>()
            .Configure(options =>
            {
                configure?.Invoke(options);
                options.Freeze();
            })
            .ValidateOnStart();
        return services;
    }

    public static RouteGroupBuilder AddUnionResultFilter(this RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);
        group.AddEndpointFilter<UnionResultEndpointFilter>();
        return group;
    }
}
