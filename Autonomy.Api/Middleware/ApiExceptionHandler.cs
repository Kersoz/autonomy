using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Autonomy.Domain.Exceptions;

namespace Autonomy.Api.Middleware;

public sealed class ApiExceptionHandler(
    ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title) = exception switch
        {
            InvalidDeliveryTransitionException =>
                (StatusCodes.Status409Conflict, "Invalid delivery transition"),
            AssignmentConflictException =>
                (StatusCodes.Status409Conflict, "Assignment conflict"),
            UnauthorizedAccessException =>
                (StatusCodes.Status403Forbidden, "Forbidden"),
            KeyNotFoundException =>
                (StatusCodes.Status404NotFound, "Not Found"),
            ArgumentException =>
                (StatusCodes.Status400BadRequest, "Invalid Request"),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred")
        };

        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception processing API request.");
        }
        else
        {
            logger.LogWarning(exception, "API request failed with status {StatusCode}.", statusCode);
        }

        var correlationId = httpContext.Items[CorrelationIdMiddleware.ItemKey]?.ToString()
            ?? httpContext.TraceIdentifier;
        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = statusCode switch
            {
                StatusCodes.Status400BadRequest => "The request was invalid.",
                StatusCodes.Status403Forbidden => "The request is not allowed.",
                StatusCodes.Status404NotFound => "The requested resource was not found.",
                StatusCodes.Status409Conflict when exception is AssignmentConflictException =>
                    "The delivery or courier no longer satisfies assignment constraints.",
                StatusCodes.Status409Conflict => "The delivery cannot transition from its current state.",
                _ => "The server could not complete the request."
            },
            Instance = httpContext.Request.Path
        };
        problem.Extensions["correlationId"] = correlationId;
        problem.Extensions["traceId"] = httpContext.TraceIdentifier;

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/problem+json";
        await httpContext.Response.WriteAsJsonAsync(
            problem,
            cancellationToken: cancellationToken);
        return true;
    }
}
