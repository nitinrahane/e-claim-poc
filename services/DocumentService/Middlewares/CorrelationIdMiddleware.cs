// Middlewares/CorrelationIdMiddleware.cs
using Microsoft.AspNetCore.Http;
using Serilog.Context;
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
        if (!context.Request.Headers.TryGetValue(CorrelationIdHeader, out var correlationId))
        {
            correlationId = Guid.NewGuid().ToString();
            context.Request.Headers[CorrelationIdHeader] = correlationId;
        }

        context.Response.Headers[CorrelationIdHeader] = correlationId;

        using (LogContext.PushProperty(CorrelationIdHeader, correlationId))
        {
            await _next(context);
        }
    }
}
