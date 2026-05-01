using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Shared.Exceptions;
using Shared.Serialisation;

namespace Shared.Middleware;

public class ExceptionHandlerMiddleware {
    private readonly RequestDelegate next;

    public ExceptionHandlerMiddleware(RequestDelegate next) {
        this.next = next;
    }

    public async Task Invoke(HttpContext context) {
        try {
            await next(context);
        }
        catch (Exception ex) {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context,
                                            Exception exception) {
        Exception newException = new($"An unhandled exception has occurred while executing the request. Url: {context.Request.GetDisplayUrl()}", exception);
        Logger.Logger.LogError(newException);

        // Set some defaults
        HttpResponse response = context.Response;
        HttpStatusCode statusCode = HttpStatusCode.InternalServerError;
        String message = "Unexpected error";

        if (exception is ArgumentException || exception is InvalidOperationException || exception is InvalidDataException || exception is FormatException || exception is NotSupportedException) {
            statusCode = HttpStatusCode.BadRequest;
            message = exception.Message;
        }
        else if (exception is NotFoundException) {
            statusCode = HttpStatusCode.NotFound;
            message = exception.Message;
        }
        else if (exception is NotImplementedException) {
            statusCode = HttpStatusCode.NotImplemented;
            message = exception.Message;
        }

        response.ContentType = context.Request.ContentType;
        response.StatusCode = (Int32)statusCode;

        await response.WriteAsync(StringSerialiser.Serialise(new ErrorResponse(message)));
    }
}