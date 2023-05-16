using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Tests
{
    using System.Net.Http;
    using System.Threading;
    using HealthChecks;
    using RichardSzalay.MockHttp;
    using Shouldly;
    using Xunit;

    public partial class SharedTests
    {
        [Fact]
        public async Task HealthCheckClient_PerformHealthCheck_HealthCheckIsReturned(){
            MockHttpMessageHandler mockHttp = new MockHttpMessageHandler();

            String expectedResponse = "{'name' : 'Test'}";
            mockHttp.When(HttpMethod.Get, "http://127.0.0.1:5000/health").Respond("application/json", expectedResponse);

            HealthCheckClient healthCheckClient = new HealthCheckClient(mockHttp.ToHttpClient());

            String response = await healthCheckClient.PerformHealthCheck("http", "127.0.0.1", 5000, CancellationToken.None);

            response.ShouldBe(expectedResponse);

        }

    }
}
