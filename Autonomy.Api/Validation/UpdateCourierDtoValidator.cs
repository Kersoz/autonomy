using Autonomy.Application.DTOs.Couriers;
using FluentValidation;

namespace Autonomy.Api.Validation;

public sealed class UpdateCourierDtoValidator : AbstractValidator<UpdateCourierDto>
{
    public UpdateCourierDtoValidator()
    {
        RuleFor(request => request.Name).NotEmpty().MaximumLength(200);
        RuleFor(request => request.ContactPhone).NotEmpty().MaximumLength(32);
        RuleFor(request => request.VehicleType).MaximumLength(100);
        RuleFor(request => request.VehiclePlate).MaximumLength(32);
        RuleFor(request => request.MaxSimultaneousDeliveries)
            .InclusiveBetween(1, 10);
    }
}
