using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shared.Exceptions;
using Shared.Middleware;

namespace Shared.Tests;

using System.IO;
using System.Net;
using Extensions;
using General;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NLog.LayoutRenderers.Wrappers;
using Shouldly;
using Xunit;

[Collection("Sequential")]
public partial class SharedTests
{
    [Theory]
    [InlineData("http://localhost/api", false)]
    [InlineData("http://localhost/api/health", true)]
    public void MiddlewareHelpersTests_IsHealthCheckRequest_ResultIsExpected(String url, Boolean expectedResult){
        Boolean result = Helpers.IsHealthCheckRequest(url);
        result.ShouldBe(expectedResult);
    }

    [Theory]
    [InlineData("http://localhost/api", false)]
    [InlineData("http://localhost/api/health", true)]
    public void MiddlewareHelpersTests_LogMessage_IsHealthCheckLog(String url, Boolean healthCheckLog){
        StringBuilder message = new();
        message.Append("message");

        TestLogger logger = TestHelpers.InitialiseLogger();

        Helpers.LogMessage(url, message, LogLevel.Information);
            
        logger.GetLogEntries().Last().Contains("HEALTH_CHECK").ShouldBe(healthCheckLog);

    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("RequestBody", true)]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("RequestBody", false)]
    public async Task RequestLoggingMiddleware_RequestIsLogged(String requestBody, Boolean logRequests)
    {
        TestLogger logger = TestHelpers.InitialiseLogger();
        RequestResponseMiddlewareLoggingConfig configuration =
            new(LogLevel.Warning, logRequests, true);
            
        DefaultHttpContext defaultContext = new();
        if (requestBody != null){
            defaultContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(requestBody));
        }
        else{
            defaultContext.Request.Body = new MemoryStream();
        }

        defaultContext.Request.Path = "/";

        // Act
        RequestLoggingMiddleware middlewareInstance = new(next: (innerHttpContext) => {
            innerHttpContext.Response.WriteAsync("ResponseBody");
            return Task.CompletedTask;
        });

        await middlewareInstance.Invoke(defaultContext, configuration);

        defaultContext.Request.Body.Seek(0, SeekOrigin.Begin);
        String body = await new StreamReader(defaultContext.Request.Body).ReadToEndAsync();
        if (requestBody != null){
            Assert.Equal(requestBody, body);
        }
        else{
            Assert.Equal(String.Empty, body);
        }

        if (logRequests)
        {
            logger.GetLogEntries().Last().ShouldNotBeNullOrEmpty();
        }
        else
        {
            logger.GetLogEntries().Length.ShouldBe(0);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task  ResponseLoggingMiddleware_RequestIsLogged(Boolean logResponses){
        TestLogger logger = TestHelpers.InitialiseLogger();
        RequestResponseMiddlewareLoggingConfig configuration = new(LogLevel.Warning, true, logResponses);

        const string expectedOutput = "Request handed over to next request delegate";

        DefaultHttpContext defaultContext = new();
        defaultContext.Response.Body = new MemoryStream();
        defaultContext.Request.Path = "/";

        // Act
        ResponseLoggingMiddleware middlewareInstance = new(next: (innerHttpContext) => {
            innerHttpContext.Response.WriteAsync(expectedOutput);
            return Task.CompletedTask;
        });

        await middlewareInstance.Invoke(defaultContext, configuration);

        defaultContext.Response.Body.Seek(0, SeekOrigin.Begin);
        String body = new StreamReader(defaultContext.Response.Body).ReadToEnd();
        Assert.Equal(expectedOutput, body);

        if (logResponses)
        {
            logger.GetLogEntries().Last().ShouldNotBeNullOrEmpty();
        }
        else
        {
            logger.GetLogEntries().Length.ShouldBe(0);
        }
    }
        
    [Fact]
    public async Task ExceptionHandlerMiddleware_ArgumentNullExceptionThrown_BadRequestHttpStatusCodeReturned()
    {
        TestHelpers.InitialiseLogger();

        ExceptionHandlerMiddleware middleware = new((innerHttpContext) =>
            throw TestHelpers.CreateException<ArgumentNullException>());

        DefaultHttpContext context = TestHelpers.CreateHttpContext();

        await middleware.Invoke(context);

        ErrorResponse responseData = TestHelpers.GetErrorResponse(context);

        context.Response.StatusCode.ShouldBe((Int32)HttpStatusCode.BadRequest);
        responseData.ShouldNotBeNull();
        responseData.Message.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task ExceptionHandlerMiddleware_InvalidDataExceptionThrown_BadRequestHttpStatusCodeReturned()
    {
        TestHelpers.InitialiseLogger();

        ExceptionHandlerMiddleware middleware = new((innerHttpContext) =>
            throw TestHelpers.CreateException<InvalidDataException>());

        DefaultHttpContext context = TestHelpers.CreateHttpContext();

        await middleware.Invoke(context);

        ErrorResponse responseData = TestHelpers.GetErrorResponse(context);

        context.Response.StatusCode.ShouldBe((Int32)HttpStatusCode.BadRequest);
        responseData.ShouldNotBeNull();
        responseData.Message.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task ExceptionHandlerMiddleware_InvalidOperationExceptionThrown_BadRequestHttpStatusCodeReturned()
    {
        TestHelpers.InitialiseLogger();

        ExceptionHandlerMiddleware middleware = new((innerHttpContext) =>
            throw TestHelpers.CreateException<InvalidOperationException>());

        DefaultHttpContext context = TestHelpers.CreateHttpContext();

        await middleware.Invoke(context);

        ErrorResponse responseData = TestHelpers.GetErrorResponse(context);

        context.Response.StatusCode.ShouldBe((Int32)HttpStatusCode.BadRequest);
        responseData.ShouldNotBeNull();
        responseData.Message.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task ExceptionHandlerMiddleware_FormatExceptionThrown_BadRequestHttpStatusCodeReturned()
    {
        TestHelpers.InitialiseLogger();

        ExceptionHandlerMiddleware middleware = new((innerHttpContext) =>
            throw TestHelpers.CreateException<FormatException>());

        DefaultHttpContext context = TestHelpers.CreateHttpContext();

        await middleware.Invoke(context);

        ErrorResponse responseData = TestHelpers.GetErrorResponse(context);

        context.Response.StatusCode.ShouldBe((Int32)HttpStatusCode.BadRequest);
        responseData.ShouldNotBeNull();
        responseData.Message.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task ExceptionHandlerMiddleware_NotSupportedExceptionThrown_BadRequestHttpStatusCodeReturned()
    {
        TestHelpers.InitialiseLogger();

        ExceptionHandlerMiddleware middleware = new((innerHttpContext) =>
            throw TestHelpers.CreateException<NotSupportedException>());

        DefaultHttpContext context = TestHelpers.CreateHttpContext();

        await middleware.Invoke(context);

        ErrorResponse responseData = TestHelpers.GetErrorResponse(context);

        context.Response.StatusCode.ShouldBe((Int32)HttpStatusCode.BadRequest);
        responseData.ShouldNotBeNull();
        responseData.Message.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task ExceptionHandlerMiddleware_NotFoundExceptionThrown_NotFoundHttpStatusCodeReturned()
    {
        TestHelpers.InitialiseLogger();

        ExceptionHandlerMiddleware middleware = new((innerHttpContext) =>
            throw TestHelpers.CreateException<NotFoundException>());

        DefaultHttpContext context = TestHelpers.CreateHttpContext();

        await middleware.Invoke(context);

        ErrorResponse responseData = TestHelpers.GetErrorResponse(context);

        context.Response.StatusCode.ShouldBe((Int32)HttpStatusCode.NotFound);
        responseData.ShouldNotBeNull();
        responseData.Message.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task ExceptionHandlerMiddleware_NotImplementedExceptionThrown_NotImplementedHttpStatusCodeReturned()
    {
        TestHelpers.InitialiseLogger();

        ExceptionHandlerMiddleware middleware = new((innerHttpContext) =>
            throw TestHelpers.CreateException<NotImplementedException>());

        DefaultHttpContext context = TestHelpers.CreateHttpContext();

        await middleware.Invoke(context);

        ErrorResponse responseData = TestHelpers.GetErrorResponse(context);

        context.Response.StatusCode.ShouldBe((Int32)HttpStatusCode.NotImplemented);
        responseData.ShouldNotBeNull();
        responseData.Message.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task ExceptionHandlerMiddleware_OtherExceptionThrown_InternalServerErrorHttpStatusCodeReturned()
    {
        TestHelpers.InitialiseLogger();

        ExceptionHandlerMiddleware middleware = new((innerHttpContext) =>
            throw TestHelpers.CreateException<Exception>());

        DefaultHttpContext context = TestHelpers.CreateHttpContext();

        await middleware.Invoke(context);

        ErrorResponse responseData = TestHelpers.GetErrorResponse(context);

        context.Response.StatusCode.ShouldBe((Int32)HttpStatusCode.InternalServerError);
        responseData.ShouldNotBeNull();
        responseData.Message.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task ExceptionHandlerMiddleware_NoExceptionThrown_OKResponseReturned()
    {
        TestHelpers.InitialiseLogger();

        ExceptionHandlerMiddleware middleware = new((innerHttpContext) => Task.CompletedTask);

        DefaultHttpContext context = TestHelpers.CreateHttpContext();

        await middleware.Invoke(context);

        ErrorResponse responseData = TestHelpers.GetErrorResponse(context);

        context.Response.StatusCode.ShouldBe((Int32)HttpStatusCode.OK);
        responseData.ShouldBeNull();
    }

    [Fact]
    public void HealthChecksBuilderExtensions_AddFileProcessorService_ServiceAdded()
    {

        IConfigurationBuilder configurationBuilder = new ConfigurationBuilder().AddInMemoryCollection(TestHelpers.DefaultAppSettings).AddEnvironmentVariables();
        ConfigurationReader.Initialise(configurationBuilder.Build());

        TestHealthChecksBuilder builder = new ();
        IHealthChecksBuilder healthChecksBuilder = builder.AddFileProcessorService();
        ((TestHealthChecksBuilder)healthChecksBuilder).Registrations.Count.ShouldBe(1);
        ((TestHealthChecksBuilder)healthChecksBuilder).Registrations.First().Name.ShouldBe("File Processor Service");
    }

    [Fact]
    public void HealthChecksBuilderExtensions_AddMessagingService_ServiceAdded()
    {

        IConfigurationBuilder configurationBuilder = new ConfigurationBuilder().AddInMemoryCollection(TestHelpers.DefaultAppSettings).AddEnvironmentVariables();
        ConfigurationReader.Initialise(configurationBuilder.Build());

        TestHealthChecksBuilder builder = new();
        IHealthChecksBuilder healthChecksBuilder = builder.AddMessagingService();
        ((TestHealthChecksBuilder)healthChecksBuilder).Registrations.Count.ShouldBe(1);
        ((TestHealthChecksBuilder)healthChecksBuilder).Registrations.First().Name.ShouldBe("Messaging Service");
    }

    [Fact]
    public void HealthChecksBuilderExtensions_AddSecurityService_ServiceAdded()
    {

        IConfigurationBuilder configurationBuilder = new ConfigurationBuilder().AddInMemoryCollection(TestHelpers.DefaultAppSettings).AddEnvironmentVariables();
        ConfigurationReader.Initialise(configurationBuilder.Build());

        TestHealthChecksBuilder builder = new();
        IHealthChecksBuilder healthChecksBuilder = builder.AddSecurityService();
        ((TestHealthChecksBuilder)healthChecksBuilder).Registrations.Count.ShouldBe(1);
        ((TestHealthChecksBuilder)healthChecksBuilder).Registrations.First().Name.ShouldBe("Security Service");
    }

    [Fact]
    public void HealthChecksBuilderExtensions_AddTransactionProcessorService_ServiceAdded()
    {

        IConfigurationBuilder configurationBuilder = new ConfigurationBuilder().AddInMemoryCollection(TestHelpers.DefaultAppSettings).AddEnvironmentVariables();
        ConfigurationReader.Initialise(configurationBuilder.Build());

        TestHealthChecksBuilder builder = new();
        IHealthChecksBuilder healthChecksBuilder = builder.AddTransactionProcessorService();
        ((TestHealthChecksBuilder)healthChecksBuilder).Registrations.Count.ShouldBe(1);
        ((TestHealthChecksBuilder)healthChecksBuilder).Registrations.First().Name.ShouldBe("Transaction Processor Service");
    }

    [Fact]
    public void ApplicationBuilderExtensions_AddExceptionHandler_HandlerAdded(){
        IApplicationBuilder builder = new ApplicationBuilder(new TestServiceProvider());

        builder.AddExceptionHandler();
    }

    [Fact]
    public void ApplicationBuilderExtensions_AddRequestLogging_HandlerAdded()
    {
        IApplicationBuilder builder = new ApplicationBuilder(new TestServiceProvider());

        builder.AddRequestLogging();
    }

    [Fact]
    public void ApplicationBuilderExtensions_AddResponseLogging_HandlerAdded()
    {
        IApplicationBuilder builder = new ApplicationBuilder(new TestServiceProvider());

        builder.AddResponseLogging();
    }
}

public class TestServiceProvider : IServiceProvider{
    public Object GetService(Type serviceType){
        return null;
    }
}

public class TestHealthChecksBuilder : IHealthChecksBuilder{
    public List<HealthCheckRegistration> Registrations = new List<HealthCheckRegistration>();

    public TestHealthChecksBuilder(){
        this.Services = new ServiceCollection();
    }

    public IHealthChecksBuilder Add(HealthCheckRegistration registration){
        this.Registrations.Add(registration);
        return this;
    }

    public IServiceCollection Services{ get; }
}