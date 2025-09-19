using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.VisualBasic;
using Newtonsoft.Json.Linq;
using Shared.General;
using Shared.Logger.TennantContext;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using ClaimsPrincipal = System.Security.Claims.ClaimsPrincipal;

namespace Shared.Middleware;

    public class TenantMiddleware
    {
        #region Fields

        private readonly RequestDelegate Next;

        #endregion

        #region Constructors

        public TenantMiddleware(RequestDelegate next) {
            this.Next = next;
        }

        public const String KeyNameCorrelationId = "correlationId";

        #endregion

        public async Task InvokeAsync(HttpContext context, TenantContext tenantContext)
        {
            Stopwatch watch = Stopwatch.StartNew();

            // Detect the tenant from the incoming request
            TenantIdentifiers tenantIdentifiers = await this.GetIdentifiersFromContext(context);

            Boolean.TryParse(ConfigurationReader.GetValueOrDefault("AppSettings","LogsPerTenantEnabled", "false"), out Boolean logPerTenantEnabled);

            // Check the headers for a correlationId
            context.Request.Headers.TryGetValue(KeyNameCorrelationId, out StringValues correlationIdHeader);
            Guid.TryParse(correlationIdHeader, out Guid correlationId);

            if (correlationId != Guid.Empty)
            {
                tenantContext.SetCorrelationId(correlationId);
                context.Items[KeyNameCorrelationId] = correlationId.ToString(); // make it accessible to HttpClient handlers
            }

            tenantContext.Initialise(tenantIdentifiers, logPerTenantEnabled);

            // Set the current tenant in the TenantContext
            TenantContext.CurrentTenant = tenantContext;

            String clientIp = context.Connection.RemoteIpAddress?.ToString();

            //Makes sense to start our correlation audit trace here
            String logMessage = $"Receiving from {clientIp} => {context.Request.Method} {context.Request.Host}{context.Request.Path}";

            Logger.Logger.LogInformation(logMessage);

            // Call the next middleware
            await this.Next(context);

            watch.Stop();
            String afterMessage = $"{context.Response.StatusCode} {logMessage} Duration: {watch.ElapsedMilliseconds}ms";
            Logger.Logger.LogInformation(afterMessage);
        }

        private async Task<TenantIdentifiers> GetIdentifiersFromContext(HttpContext context) =>
            context switch
            {
                _ when context.GetIdentifiersFromToken() is var identifiersFromToken && identifiersFromToken != TenantIdentifiers.Default() => identifiersFromToken,
                _ when context.GetIdentifiersFromHeaders() is var identifiersFromHeaders && identifiersFromHeaders != TenantIdentifiers.Default() => identifiersFromHeaders,
                _ when context.GetIdentifiersFromRoute() is var identifiersFromHeaders && identifiersFromHeaders != TenantIdentifiers.Default() => identifiersFromHeaders,
                //_ when await context.GetIdentifiersFromPayload() is var identifiersFromPayload && identifiersFromPayload != TenantIdentifiers.Default() =>
                    //identifiersFromPayload,
                _ => TenantIdentifiers.Default(),
            };
    }


public static class ClaimsPrincipalExtensions
{
    public static Boolean IsAuthenticated(this ClaimsPrincipal principal)
    {
        return principal?.Identity?.IsAuthenticated ?? false;
    }
}

public static class HttpContextExtensionMethods {
    public static TenantIdentifiers GetIdentifiersFromToken(this HttpContext context) {
        if (!context.User.IsAuthenticated()) {
            return TenantIdentifiers.Default();
        }

        Claim claimEstateId = context.User.Claims.SingleOrDefault(u => u.Type == "estateId");
        Claim claimMerchantId = context.User.Claims.SingleOrDefault(u => u.Type == "merchantId");

        Guid.TryParse(claimEstateId?.Value, out Guid estateId);
        Guid.TryParse(claimMerchantId?.Value, out Guid merchantId);

        return estateId == Guid.Empty ? TenantIdentifiers.Default() : new TenantIdentifiers(estateId, merchantId);
    }

    public static TenantIdentifiers GetIdentifiersFromHeaders(this HttpContext context) {
        // Get the org Id
        context.Request.Headers.TryGetValue("estateId", out StringValues estateIdHeader);
        Guid.TryParse(estateIdHeader, out Guid estateId);

        // Try and get the store Id
        context.Request.Headers.TryGetValue("merchantId", out StringValues merchantIdHeader);
        Guid.TryParse(merchantIdHeader, out Guid merchantId);

        return estateId == Guid.Empty ? TenantIdentifiers.Default() : new TenantIdentifiers(estateId, merchantId);
    }

    public static TenantIdentifiers GetIdentifiersFromRoute(this HttpContext context) {
        // Get the org Id

        context.Request.RouteValues.TryGetValue("estateId", out object estateIdRouteValue);
        Guid.TryParse(estateIdRouteValue?.ToString(), out Guid estateId);

        // Try and get the store Id
        context.Request.RouteValues.TryGetValue("merchantId", out object merchantIdRouteValue);
        Guid.TryParse(merchantIdRouteValue?.ToString(), out Guid merchantId);

        return estateId == Guid.Empty ? TenantIdentifiers.Default() : new TenantIdentifiers(estateId, merchantId);
    }

    /*public static async Task<TenantIdentifiers> GetIdentifiersFromPayload(this HttpContext context)
    {
        HttpRequest request = context.Request;
        String bodyAsText = null;

        try
        {
            request.EnableBuffering();
            bodyAsText = await new StreamReader(request.Body).ReadToEndAsync();
            request.Body.Position = 0;

            if (String.IsNullOrWhiteSpace(bodyAsText))
            {
                return TenantIdentifiers.Default();
            }

            JToken rootToken = JToken.Parse(bodyAsText);

            JToken organisationIdToken = rootToken.SelectTokens("..organisationId").FirstOrDefault();
            JToken storeIdToken = rootToken.SelectTokens("..storeId").FirstOrDefault();

            Guid.TryParse(organisationIdToken?.Value<String>(), out Guid organisationId);
            Guid.TryParse(storeIdToken?.Value<String>(), out Guid storeId);

            return organisationId == Guid.Empty ? TenantIdentifiers.Default() : new TenantIdentifiers(organisationId, storeId);
        }
        catch (Exception e)
        {
            EposityLogger.WriteWarning($"Unable to get organisationId from request body [{bodyAsText}]");
            EposityLogger.WriteException(e);
            return TenantIdentifiers.Default();
        }
    }*/
}
