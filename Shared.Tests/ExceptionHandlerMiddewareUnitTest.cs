using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using Shared.Exceptions;
using Shared.General;
using Shared.Middleware;
using Shouldly;
using Xunit;
using NullLogger = Shared.Logger.NullLogger;

namespace Shared.Tests
{
    public class ExceptionHandlerMiddewareUnitTest
    {
        private const String ExceptionMessage = "Test Exception Message";

        [Fact]
        public async void ExceptionHandlerMiddleware_ArgumentNullExceptionThrown_BadRequestHttpStatusCodeReturned()
        {
            Logger.Logger.Initialise(NullLogger.Instance);
            
            ExceptionHandlerMiddleware middleware = new ExceptionHandlerMiddleware((innerHttpContext) =>
                throw new ArgumentNullException("TestParam",ExceptionHandlerMiddewareUnitTest.ExceptionMessage));

            DefaultHttpContext context = ExceptionHandlerMiddewareUnitTest.CreateContext();

            await middleware.Invoke(context);

            ErrorResponse responseData = ExceptionHandlerMiddewareUnitTest.GetErrorResponse(context);

            context.Response.StatusCode.ShouldBe((Int32) HttpStatusCode.BadRequest);
            responseData.ShouldNotBeNull();
            responseData.Message.ShouldNotBeNullOrEmpty();
        }

        [Fact]
        public async void ExceptionHandlerMiddleware_InvalidDataExceptionThrown_BadRequestHttpStatusCodeReturned()
        {
            Logger.Logger.Initialise(NullLogger.Instance);
            
            ExceptionHandlerMiddleware middleware = new ExceptionHandlerMiddleware((innerHttpContext) =>
                throw new InvalidDataException(ExceptionHandlerMiddewareUnitTest.ExceptionMessage));

            DefaultHttpContext context = ExceptionHandlerMiddewareUnitTest.CreateContext();

            await middleware.Invoke(context);

            ErrorResponse responseData = ExceptionHandlerMiddewareUnitTest.GetErrorResponse(context);

            context.Response.StatusCode.ShouldBe((Int32) HttpStatusCode.BadRequest);
            responseData.ShouldNotBeNull();
            responseData.Message.ShouldNotBeNullOrEmpty();
        }

        [Fact]
        public async void ExceptionHandlerMiddleware_InvalidOperationExceptionThrown_BadRequestHttpStatusCodeReturned()
        {
            Logger.Logger.Initialise(NullLogger.Instance);
            
            ExceptionHandlerMiddleware middleware = new ExceptionHandlerMiddleware((innerHttpContext) =>
                throw new InvalidOperationException(ExceptionHandlerMiddewareUnitTest.ExceptionMessage));

            DefaultHttpContext context = ExceptionHandlerMiddewareUnitTest.CreateContext();

            await middleware.Invoke(context);

            ErrorResponse responseData = ExceptionHandlerMiddewareUnitTest.GetErrorResponse(context);

            context.Response.StatusCode.ShouldBe((Int32) HttpStatusCode.BadRequest);
            responseData.ShouldNotBeNull();
            responseData.Message.ShouldNotBeNullOrEmpty();
        }

        [Fact]
        public async void ExceptionHandlerMiddleware_FormatExceptionThrown_BadRequestHttpStatusCodeReturned()
        {
            Logger.Logger.Initialise(NullLogger.Instance);
            
            ExceptionHandlerMiddleware middleware = new ExceptionHandlerMiddleware((innerHttpContext) =>
                throw new FormatException(ExceptionHandlerMiddewareUnitTest.ExceptionMessage));

            DefaultHttpContext context = ExceptionHandlerMiddewareUnitTest.CreateContext();

            await middleware.Invoke(context);

            ErrorResponse responseData = ExceptionHandlerMiddewareUnitTest.GetErrorResponse(context);

            context.Response.StatusCode.ShouldBe((Int32) HttpStatusCode.BadRequest);
            responseData.ShouldNotBeNull();
            responseData.Message.ShouldNotBeNullOrEmpty();
        }

        [Fact]
        public async void ExceptionHandlerMiddleware_NotSupportedExceptionThrown_BadRequestHttpStatusCodeReturned()
        {
            Logger.Logger.Initialise(NullLogger.Instance);
            
            ExceptionHandlerMiddleware middleware = new ExceptionHandlerMiddleware((innerHttpContext) =>
                throw new NotSupportedException(ExceptionHandlerMiddewareUnitTest.ExceptionMessage));

            DefaultHttpContext context = ExceptionHandlerMiddewareUnitTest.CreateContext();

            await middleware.Invoke(context);

            ErrorResponse responseData = ExceptionHandlerMiddewareUnitTest.GetErrorResponse(context);

            context.Response.StatusCode.ShouldBe((Int32) HttpStatusCode.BadRequest);
            responseData.ShouldNotBeNull();
            responseData.Message.ShouldNotBeNullOrEmpty();
        }

        [Fact]
        public async void ExceptionHandlerMiddleware_NotFoundExceptionThrown_NotFoundHttpStatusCodeReturned()
        {
            Logger.Logger.Initialise(NullLogger.Instance);
            
            ExceptionHandlerMiddleware middleware = new ExceptionHandlerMiddleware((innerHttpContext) =>
                throw new NotFoundException(ExceptionHandlerMiddewareUnitTest.ExceptionMessage));

            DefaultHttpContext context = ExceptionHandlerMiddewareUnitTest.CreateContext();

            await middleware.Invoke(context);

            ErrorResponse responseData = ExceptionHandlerMiddewareUnitTest.GetErrorResponse(context);

            context.Response.StatusCode.ShouldBe((Int32) HttpStatusCode.NotFound);
            responseData.ShouldNotBeNull();
            responseData.Message.ShouldNotBeNullOrEmpty();
        }

        [Fact]
        public async void ExceptionHandlerMiddleware_NotImplementedExceptionThrown_NotImplementedHttpStatusCodeReturned()
        {
            Logger.Logger.Initialise(NullLogger.Instance);
            
            ExceptionHandlerMiddleware middleware = new ExceptionHandlerMiddleware((innerHttpContext) =>
                throw new NotImplementedException(ExceptionHandlerMiddewareUnitTest.ExceptionMessage));

            DefaultHttpContext context = ExceptionHandlerMiddewareUnitTest.CreateContext();

            await middleware.Invoke(context);

            ErrorResponse responseData = ExceptionHandlerMiddewareUnitTest.GetErrorResponse(context);

            context.Response.StatusCode.ShouldBe((Int32) HttpStatusCode.NotImplemented);
            responseData.ShouldNotBeNull();
            responseData.Message.ShouldNotBeNullOrEmpty();
        }

        [Fact]
        public async void ExceptionHandlerMiddleware_OtherExceptionThrown_InternalServerErrorHttpStatusCodeReturned()
        {
            Logger.Logger.Initialise(NullLogger.Instance);
            
            ExceptionHandlerMiddleware middleware = new ExceptionHandlerMiddleware((innerHttpContext) =>
                throw new Exception(ExceptionHandlerMiddewareUnitTest.ExceptionMessage));

            DefaultHttpContext context = ExceptionHandlerMiddewareUnitTest.CreateContext();

            await middleware.Invoke(context);

            ErrorResponse responseData = ExceptionHandlerMiddewareUnitTest.GetErrorResponse(context);

            context.Response.StatusCode.ShouldBe((Int32) HttpStatusCode.InternalServerError);
            responseData.ShouldNotBeNull();
            responseData.Message.ShouldNotBeNullOrEmpty();
        }

        private static ErrorResponse GetErrorResponse(DefaultHttpContext context)
        {
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            StreamReader reader = new StreamReader(context.Response.Body);
            String streamText = reader.ReadToEnd();
            ErrorResponse responseData = JsonConvert.DeserializeObject<ErrorResponse>(streamText);
            return responseData;
        }

        private static DefaultHttpContext CreateContext()
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
    }
}
