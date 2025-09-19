using System.Collections.Generic;
using System.Net;
using SimpleResults;

namespace Shared.HealthChecks;

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

public class HealthCheckClient : IHealthCheckClient
{
    private readonly HttpClient HttpClient;

    #region Constructors

    public HealthCheckClient(HttpClient httpClient) {
        this.HttpClient = httpClient;
    }

    #endregion

    #region Methods

    public async Task<Result<String>> PerformHealthCheck(String scheme, 
                                                         String uri,
                                                         Int32 port,
                                                         CancellationToken cancellationToken) {
        String requestUri = this.BuildRequestUri(scheme, uri, port);

        HttpRequestMessage request = new(HttpMethod.Get, requestUri);

        HttpResponseMessage responseMessage = await this.HttpClient.SendAsync(request);

        Result<String> responseData = await this.HandleResponse(responseMessage, cancellationToken);

        return responseData;
    }

    protected virtual async Task<Result<String>> HandleResponse(HttpResponseMessage responseMessage,
                                                        CancellationToken cancellationToken)
    {
        // Read the content from the response
        String content = await responseMessage.Content.ReadAsStringAsync(cancellationToken);

        // Check the response code
        Result<String> result = (responseMessage.IsSuccessStatusCode, responseMessage.StatusCode) switch {
            (true, _) => Result.Success<String>(content),
            (_, HttpStatusCode.BadRequest) => Result.Invalid(content),
            (_, HttpStatusCode.Forbidden) => Result.Forbidden(content),
            (_, HttpStatusCode.NotFound) => Result.NotFound(content),
            (_, HttpStatusCode.Unauthorized) => Result.Unauthorized(content),
            (_, HttpStatusCode.InternalServerError) => Result.CriticalError(content),
            _ => Result.Failure(content)

        };
        return result;
    }

    private String BuildRequestUri(String scheme, 
                                   String uri,
                                   Int32 port) {
        return $"{scheme}://{uri}:{port}/health";
    }

    #endregion
}