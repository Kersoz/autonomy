using System.Diagnostics;

namespace Autonomy.Api.Middleware;

public sealed class RequestTrackingMiddleware(
    RequestDelegate next,
    ILogger<RequestTrackingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await next(context);
        }
        finally
        {
            stopwatch.Stop();
            logger.LogInformation(
                "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMilliseconds} ms. CorrelationId={CorrelationId} UserId={UserId}",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds,
                context.Items[CorrelationIdMiddleware.ItemKey],
                context.User.FindFirst("sub")?.Value);
        }
    }
}
