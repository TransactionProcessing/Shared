namespace Shared.HealthChecks;

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ClientProxyBase;
using Newtonsoft.Json;

public class HealthCheckClient : ClientProxyBase, IHealthCheckClient
{
    #region Constructors

    public HealthCheckClient(HttpClient client) : base(client) {
    }

    #endregion

    #region Methods

    public async Task<HealthCheckResult> PerformHealthCheck(String uri,
                                                            Int32 port,
                                                            CancellationToken cancellationToken) {
        String requestUri = this.BuildRequestUri(uri, port);

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUri);

        HttpResponseMessage responseMessage = await this.HttpClient.SendAsync(request);

        String responseData = await this.HandleResponse(responseMessage, cancellationToken);

        return JsonConvert.DeserializeObject<HealthCheckResult>(responseData);
    }

    private String BuildRequestUri(String uri,
                                   Int32 port) {
        return $"http://{uri}:{port}/health";
    }

    #endregion
}