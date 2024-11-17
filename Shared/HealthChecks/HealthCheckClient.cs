using System.Collections.Generic;
using System.Net;

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

    public async Task<String> PerformHealthCheck(String scheme, 
                                                            String uri,
                                                            Int32 port,
                                                            CancellationToken cancellationToken) {
        String requestUri = this.BuildRequestUri(scheme, uri, port);

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUri);

        HttpResponseMessage responseMessage = await this.HttpClient.SendAsync(request);

        String responseData = await this.HandleResponse(responseMessage, cancellationToken);

        return responseData;
    }

    protected virtual async Task<String> HandleResponse(HttpResponseMessage responseMessage,
                                                        CancellationToken cancellationToken)
    {
        String result = String.Empty;

        // Read the content from the response
        String content = await responseMessage.Content.ReadAsStringAsync(cancellationToken);

        // Check the response code
        if (!responseMessage.IsSuccessStatusCode)
        {
            // throw a specific  exception to inherited class
            switch (responseMessage.StatusCode)
            {
                case HttpStatusCode.BadRequest:
                    throw new InvalidOperationException(content);
                case HttpStatusCode.Unauthorized:
                case HttpStatusCode.Forbidden:
                    throw new UnauthorizedAccessException(content);
                case HttpStatusCode.NotFound:
                    throw new KeyNotFoundException(content);
                case HttpStatusCode.InternalServerError:
                    throw new Exception("An internal error has occurred");
                default:
                    throw new Exception($"An internal error has occurred ({responseMessage.StatusCode})");
            }
        }

        // Set the result
        result = content;

        return result;
    }

    private String BuildRequestUri(String scheme, 
                                   String uri,
                                   Int32 port) {
        return $"{scheme}://{uri}:{port}/health";
    }

    #endregion
}