using Autonomy.Api.Contracts.Deliveries;
using FluentValidation;

namespace Autonomy.Api.Validation;

public sealed class AssignDeliveryRequestValidator
    : AbstractValidator<AssignDeliveryRequest>
{
    public AssignDeliveryRequestValidator()
    {
        RuleFor(request => request.CourierId).NotEmpty();
    }
}
