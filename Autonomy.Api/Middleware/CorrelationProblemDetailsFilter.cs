using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Autonomy.Api.Middleware;

public sealed class CorrelationProblemDetailsFilter(
    IHttpContextAccessor httpContextAccessor) : IResultFilter
{
    public void OnResultExecuting(ResultExecutingContext context)
    {
        if (context.Result is not ObjectResult { Value: ProblemDetails problemDetails })
        {
            return;
        }

        var httpContext = httpContextAccessor.HttpContext;
        problemDetails.Extensions["correlationId"] =
            httpContext?.Items[CorrelationIdMiddleware.ItemKey]?.ToString()
            ?? httpContext?.TraceIdentifier;
        problemDetails.Extensions["traceId"] = httpContext?.TraceIdentifier;
    }

    public void OnResultExecuted(ResultExecutedContext context)
    {
    }
}
