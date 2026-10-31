using Autonomy.Api.Contracts.Deliveries;
using Autonomy.Domain.Enums;
using FluentValidation;

namespace Autonomy.Api.Validation;

public sealed class CreateDeliveryRequestValidator
    : AbstractValidator<CreateDeliveryRequest>
{
    public CreateDeliveryRequestValidator()
    {
        RuleFor(request => request.FleetId).NotEmpty();
        RuleFor(request => request.MerchantId).NotEmpty();
        RuleFor(request => request.CustomerName).NotEmpty().MaximumLength(200);
        RuleFor(request => request.CustomerPhone).NotEmpty().MaximumLength(32);
        RuleFor(request => request.PickupAddress).NotEmpty().MaximumLength(500);
        RuleFor(request => request.DropoffAddress).NotEmpty().MaximumLength(500);
        RuleFor(request => request.PickupLatitude).InclusiveBetween(-90, 90);
        RuleFor(request => request.PickupLongitude).InclusiveBetween(-180, 180);
        RuleFor(request => request.DropoffLatitude).InclusiveBetween(-90, 90);
        RuleFor(request => request.DropoffLongitude).InclusiveBetween(-180, 180);
        RuleFor(request => request.OrderValue).GreaterThanOrEqualTo(0);
        RuleFor(request => request.PaymentMethod)
            .IsInEnum()
            .NotEqual(PaymentMethod.Unknown);
        RuleFor(request => request.Currency)
            .NotEmpty()
            .Length(3)
            .Matches("^[A-Za-z]{3}$");
    }
}
