using Autonomy.Api.Contracts.Deliveries;
using FluentValidation;

namespace Autonomy.Api.Validation;

public sealed class CancelDeliveryRequestValidator
    : AbstractValidator<CancelDeliveryRequest>
{
    public CancelDeliveryRequestValidator()
    {
        RuleFor(request => request.Reason).NotEmpty().MaximumLength(500);
    }
}
