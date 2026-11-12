using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Routing;
using TransactionProcessing.UnionResults;

namespace TransactionProcessing.Shared.Tests;

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
        mockHttp.When(HttpMethod.Get, "http://127.0.0.1:5000/healthui").Respond("application/json", expectedResponse);

        HealthCheckClient healthCheckClient = new(mockHttp.ToHttpClient());

        var result = await healthCheckClient.PerformHealthCheck("http", "127.0.0.1", 5000, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Data.ShouldBe(expectedResponse);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, ErrorType.Validation)]
    [InlineData(HttpStatusCode.Forbidden, ErrorType.Forbidden)]
    [InlineData(HttpStatusCode.NotFound, ErrorType.NotFound)]
    [InlineData(HttpStatusCode.Unauthorized, ErrorType.Unauthorized)]
    [InlineData(HttpStatusCode.InternalServerError, ErrorType.Critical)]
    [InlineData(HttpStatusCode.BadGateway, ErrorType.Failure)]
    public async Task HealthCheckClient_PerformHealthCheck_FailedResponses_HealthCheckIsReturned(HttpStatusCode statusCode, ErrorType resultStatus)
    {
        MockHttpMessageHandler mockHttp = new();

        //String expectedResponse = "{'name' : 'Test'}";
        mockHttp.When(HttpMethod.Get, "http://127.0.0.1:5000/healthui").Respond(req => new HttpResponseMessage(statusCode));

        HealthCheckClient healthCheckClient = new(mockHttp.ToHttpClient());

        Result<String> result = await healthCheckClient.PerformHealthCheck("http", "127.0.0.1", 5000, CancellationToken.None);

        result.IsFailed.ShouldBeTrue();
        result.Errors[0].ErrorType.ShouldBe(resultStatus);
    }

}
