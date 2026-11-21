using System.Net;
using TransactionProcessing.UnionResults;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace TransactionProcessing.Shared.HealthChecks;

public class HealthCheckClient : IHealthCheckClient
{
    private readonly HttpClient HttpClient;

    public HealthCheckClient(HttpClient httpClient) {
        this.HttpClient = httpClient;
    }

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
        String content = await responseMessage.Content.ReadAsStringAsync(cancellationToken);
        if (responseMessage.IsSuccessStatusCode) {
            return Results.Ok(content);
        }

        ErrorType errorType = responseMessage.StatusCode switch {
            HttpStatusCode.BadRequest => ErrorType.Validation,
            HttpStatusCode.Forbidden => ErrorType.Forbidden,
            HttpStatusCode.NotFound => ErrorType.NotFound,
            HttpStatusCode.Unauthorized => ErrorType.Unauthorized,
            HttpStatusCode.InternalServerError => ErrorType.Critical,
            _ => ErrorType.Failure
        };
        return new Error(errorType, content);
    }

    private String BuildRequestUri(String scheme, 
                                   String uri,
                                   Int32 port) {
        // The standard health endpoint is allowed to use a plain-text response.
        // Container startup deserialises the response, so use the JSON health
        // check UI endpoint consistently across the services.
        return $"{scheme}://{uri}:{port}/healthui";
    }
}
