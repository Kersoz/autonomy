using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Autonomy.Api.Middleware;

public sealed class FluentValidationActionFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        var failures = new List<ValidationFailure>();
        foreach (var argument in context.ActionArguments.Values.Where(value => value is not null))
        {
            var validatorType = typeof(IValidator<>).MakeGenericType(argument!.GetType());
            var validators = context.HttpContext.RequestServices
                .GetServices(validatorType)
                .Cast<IValidator>();
            foreach (var validator in validators)
            {
                var validationContextType = typeof(ValidationContext<>)
                    .MakeGenericType(argument.GetType());
                var validationContext = (IValidationContext?)Activator.CreateInstance(
                    validationContextType,
                    argument)
                    ?? throw new InvalidOperationException(
                        $"Could not create a validation context for {argument.GetType().Name}.");
                var validationResult = await validator.ValidateAsync(
                    validationContext,
                    context.HttpContext.RequestAborted);
                failures.AddRange(validationResult.Errors);
            }
        }

        if (failures.Count > 0)
        {
            var errors = failures
                .GroupBy(failure => failure.PropertyName)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(failure => failure.ErrorMessage).ToArray());
            context.Result = new BadRequestObjectResult(new ValidationProblemDetails(errors)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "One or more validation errors occurred.",
                Instance = context.HttpContext.Request.Path,
                Extensions =
                {
                    ["correlationId"] =
                        context.HttpContext.Items[CorrelationIdMiddleware.ItemKey]?.ToString()
                }
            });
            return;
        }

        await next();
    }
}
