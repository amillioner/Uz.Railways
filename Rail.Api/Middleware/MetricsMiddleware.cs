using Rail.Authentication;
using System.Diagnostics;

namespace Rail.Api.Middleware;

public class MetricsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMetricsService _metricsService;

    public MetricsMiddleware(RequestDelegate next, IMetricsService metricsService)
    {
        _next = next;
        _metricsService = metricsService;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();

            var endpoint = context.Request.Path.Value ?? "unknown";
            var method = context.Request.Method;
            var statusCode = context.Response.StatusCode;

            _metricsService.IncrementApiRequest(endpoint, method, statusCode);
            _metricsService.RecordApiResponseTime(endpoint, method, stopwatch.Elapsed.TotalMilliseconds);
        }
    }
}