using Autonomy.Api.Authorization;
using Autonomy.Api.Contracts.Auth;
using Autonomy.Persistence.Identity;
using FluentValidation;

namespace Autonomy.Api.Validation;

public sealed class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(request => request.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);
        RuleFor(request => request.Password)
            .NotEmpty()
            .MinimumLength(10)
            .Matches("[A-Z]").WithMessage("Password must contain an uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain a lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain a digit.");
        RuleFor(request => request.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(request => request.LastName).NotEmpty().MaximumLength(100);
        RuleFor(request => request.RoleName)
            .Must(role => AuthorizationRoles.All.Contains(role, StringComparer.Ordinal))
            .WithMessage("RoleName is not a supported role.");
        RuleFor(request => request.TenantId)
            .NotEmpty()
            .When(request => request.RoleName != AuthorizationRoles.PlatformAdmin);
        RuleFor(request => request.MerchantId)
            .NotEmpty()
            .When(request => request.RoleName == AuthorizationRoles.MerchantUser);
        RuleFor(request => request.MerchantId)
            .Null()
            .When(request => request.RoleName is AuthorizationRoles.PlatformAdmin
                or AuthorizationRoles.FleetAdmin
                or AuthorizationRoles.Dispatcher
                or AuthorizationRoles.Courier);
    }
}
