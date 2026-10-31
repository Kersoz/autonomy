namespace Autonomy.Api.Middleware;

public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Correlation-ID";
    public const string ItemKey = "CorrelationId";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = Guid.TryParse(
            context.Request.Headers[HeaderName].FirstOrDefault(),
            out var parsedCorrelationId)
            ? parsedCorrelationId.ToString()
            : Guid.NewGuid().ToString();

        context.Items[ItemKey] = correlationId;
        context.Response.Headers[HeaderName] = correlationId;
        await next(context);
    }
}
