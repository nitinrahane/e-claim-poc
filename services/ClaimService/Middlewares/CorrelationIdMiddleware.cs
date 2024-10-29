// Middleware to handle Correlation ID
using Microsoft.AspNetCore.Http;
using Serilog;
using Serilog.Context;
using System;
using System.Threading.Tasks;
public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private const string CorrelationIdHeader = "X-Correlation-ID";

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Check if the correlation ID is already present in the request
        if (!context.Request.Headers.TryGetValue(CorrelationIdHeader, out var correlationId))
        {
            correlationId = Guid.NewGuid().ToString(); // Generate a new correlation ID
            context.Request.Headers[CorrelationIdHeader] = correlationId; // Add it to the headers
        }

        // Add the correlation ID to the response headers
        context.Response.Headers[CorrelationIdHeader] = correlationId;

        // Log the correlation ID for tracking
        using (LogContext.PushProperty(CorrelationIdHeader, correlationId))
        {
            Log.Information("Request started for {Method} {Path} with Correlation ID {CorrelationId}",
                        context.Request.Method, context.Request.Path, correlationId);

            await _next(context);

            Log.Information("Request finished for {Method} {Path} with Correlation ID {CorrelationId}",
                            context.Request.Method, context.Request.Path, correlationId);


        }
    }
}
