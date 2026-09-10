using Microsoft.EntityFrameworkCore;
using ReconFlow.Application.Exceptions;

namespace ReconFlow.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (AppException exception)
        {
            await WriteErrorAsync(context, exception.StatusCode, exception.Code, exception.Message);
        }
        catch (DbUpdateException exception)
        {
            logger.LogWarning(exception, "A database constraint rejected the request");
            await WriteErrorAsync(context, 409, "DATA_CONFLICT", "The request conflicts with existing data.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unhandled request failure for {Method} {Path}", context.Request.Method, context.Request.Path);
            await WriteErrorAsync(context, 500, "INTERNAL_ERROR", "An unexpected error occurred.");
        }
    }

    private static async Task WriteErrorAsync(HttpContext context, int status, string code, string message)
    {
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new { code, message, traceId = context.TraceIdentifier });
    }
}
