namespace Shared.Tests;

using System;
using System.Collections.Generic;
using System.IO;
using Logger;
using Microsoft.AspNetCore.Http;
using Middleware;
using Newtonsoft.Json;

public static class TestHelpers{
    public static DefaultHttpContext CreateHttpContext()
    {
        DefaultHttpContext context = new DefaultHttpContext();
        context.Request.Scheme = "http";
        context.Request.Host = new HostString("localhost");
        context.Request.Path = new PathString("/test");
        context.Request.PathBase = new PathString("/");
        context.Request.Method = "GET";
        context.Request.Body = new MemoryStream();
        context.Request.QueryString = new QueryString("?param1=2");
        context.Response.Body = new MemoryStream();
        return context;
    }

    public static ErrorResponse GetErrorResponse(DefaultHttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        StreamReader reader = new StreamReader(context.Response.Body);
        String streamText = reader.ReadToEnd();
        ErrorResponse responseData = JsonConvert.DeserializeObject<ErrorResponse>(streamText);
        return responseData;
    }

    public static readonly String ExceptionMessage = "Test Exception Message";

    public static T CreateException<T>() where T : Exception{
        return (T)System.Activator.CreateInstance(typeof(T), TestHelpers.ExceptionMessage);
    }

    public static TestLogger InitialiseLogger(){
        TestLogger logger = new TestLogger();
        Logger.Initialise(logger);
        return logger;
    }

    public static IReadOnlyDictionary<String, String> DefaultAppSettings { get; } = new Dictionary<String, String>
                                                                                    {
                                                                                        ["AppSettings:Test"] = "",
                                                                                        ["AppSettings:ClientId"] = "clientId",
                                                                                        ["AppSettings:ClientSecret"] = "Secret1",
                                                                                        ["AppSettings:TestArray"] = "[\"A\", \"B\", \"C\"]",
        ["AppSettings:FileProcessorApi"] = "http://127.0.0.1:5009",
        ["AppSettings:MessagingServiceApi"] = "http://127.0.0.1:5006",
        ["SecurityConfiguration:Authority"] = "http://127.0.0.1:5001",
        ["AppSettings:TransactionProcessorApi"] = "http://127.0.0.1:5002",
        ["EventStoreSettings:ConnectionString"] = "https://192.168.1.133:2113",
                                                                                        ["ConnectionStrings:HealthCheck"] =
                                                                                            "server=192.168.1.133;database=master;user id=sa;password=Sc0tland",
                                                                                        ["AppSettings:EventHandlerConfiguration:ResponseReceivedFromEmailProviderEvent:0"] =
                                                                                            "MessagingService.BusinessLogic.EventHandling.EmailDomainEventHandler, MessagingService.BusinessLogic",
                                                                                        ["AppSettings:EventHandlerConfiguration:ResponseReceivedFromSMSProviderEvent:0"] =
                                                                                            "MessagingService.BusinessLogic.EventHandling.SMSDomainEventHandler, MessagingService.BusinessLogic"
                                                                                    };
}