using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Authorisation
{
    public static class AuthorizationExtensions {
        public static IServiceCollection AddClientCredentialsHandler(this IServiceCollection services) {
            services.AddSingleton<IAuthorizationHandler, ClientCredentialsHandler>();
            return services;
        }

        public static IServiceCollection AddClientCredentialsOnlyPolicy(this IServiceCollection services) {
            services.AddAuthorization(options => {
                options.AddPolicy("ClientCredentialsOnly", policy => 
                    policy.Requirements.Add(new ClientCredentialsRequirement()));
            });

            return services;
        }

        public static IServiceCollection AddPasswordTokenHandler(this IServiceCollection services)
        {
            services.AddSingleton<IAuthorizationHandler, PasswordTokenHandler>();
            return services;
        }

        public static IServiceCollection AddPasswordTokenPolicy(this IServiceCollection services)
        {
            services.AddAuthorization(options =>
            {
                options.AddPolicy("PasswordToken", policy =>
                    policy.Requirements.Add(new PasswordTokenRequirement()));
            });

            return services;
        }


        private sealed class PasswordTokenRequirement : IAuthorizationRequirement;

        private sealed class ClientCredentialsRequirement : IAuthorizationRequirement;

        private sealed class PasswordTokenHandler : AuthorizationHandler<PasswordTokenRequirement> {
            protected override Task HandleRequirementAsync(AuthorizationHandlerContext context,
                                                           PasswordTokenRequirement requirement) {
                // Your logic from IsPasswordToken()
                Claim? userIdClaim = context.User.Claims.SingleOrDefault(c => c.Type == ClaimTypes.NameIdentifier);

                if (userIdClaim != null) {
                    context.Succeed(requirement);
                }

                return Task.CompletedTask;
            }
        }

        private sealed class ClientCredentialsHandler : AuthorizationHandler<ClientCredentialsRequirement> {
            protected override Task HandleRequirementAsync(AuthorizationHandlerContext context,
                                                           ClientCredentialsRequirement requirement) {
                // A client credentials token *does not* include a NameIdentifier (user) claim.
                bool isUserToken = context.User.HasClaim(c => c.Type == ClaimTypes.NameIdentifier);

                if (!isUserToken) {
                    context.Succeed(requirement); // Valid client credentials token
                }

                return Task.CompletedTask;
            }
        }

    }
}
