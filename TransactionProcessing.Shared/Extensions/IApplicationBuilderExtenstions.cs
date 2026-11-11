using TransactionProcessing.Shared.Middleware;
using Microsoft.AspNetCore.Builder;

namespace TransactionProcessing.Shared.Extensions;

public static class IApplicationBuilderExtensions
{
    public static void AddExceptionHandler(this IApplicationBuilder applicationBuilder) => applicationBuilder.UseMiddleware<ExceptionHandlerMiddleware>();
    
    public static void AddRequestResponseLogging(this IApplicationBuilder applicationBuilder) => applicationBuilder.UseMiddleware<RequestResponseLoggingMiddleware>();
}