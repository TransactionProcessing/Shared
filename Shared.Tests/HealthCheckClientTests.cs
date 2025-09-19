using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Routing;
using SimpleResults;

namespace Shared.Tests;

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
        MockHttpMessageHandler mockHttp = new();

        String expectedResponse = "{'name' : 'Test'}";
        mockHttp.When(HttpMethod.Get, "http://127.0.0.1:5000/health").Respond("application/json", expectedResponse);

        HealthCheckClient healthCheckClient = new(mockHttp.ToHttpClient());

        var result = await healthCheckClient.PerformHealthCheck("http", "127.0.0.1", 5000, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Data.ShouldBe(expectedResponse);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, ResultStatus.Invalid)]
    [InlineData(HttpStatusCode.Forbidden, ResultStatus.Forbidden)]
    [InlineData(HttpStatusCode.NotFound, ResultStatus.NotFound)]
    [InlineData(HttpStatusCode.Unauthorized, ResultStatus.Unauthorized)]
    [InlineData(HttpStatusCode.InternalServerError, ResultStatus.CriticalError)]
    [InlineData(HttpStatusCode.BadGateway, ResultStatus.Failure)]
    public async Task HealthCheckClient_PerformHealthCheck_FailedResponses_HealthCheckIsReturned(HttpStatusCode statusCode, ResultStatus resultStatus)
    {
        MockHttpMessageHandler mockHttp = new();

        //String expectedResponse = "{'name' : 'Test'}";
        mockHttp.When(HttpMethod.Get, "http://127.0.0.1:5000/health").Respond(req => new HttpResponseMessage(statusCode));

        HealthCheckClient healthCheckClient = new(mockHttp.ToHttpClient());

        Result<String> result = await healthCheckClient.PerformHealthCheck("http", "127.0.0.1", 5000, CancellationToken.None);

        result.IsFailed.ShouldBeTrue();
        result.Status.ShouldBe(resultStatus);
    }

}