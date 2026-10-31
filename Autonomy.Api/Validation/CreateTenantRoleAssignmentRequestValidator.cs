using Autonomy.Api.Contracts.Auth;
using Autonomy.Api.Authorization;
using Autonomy.Persistence.Identity;
using FluentValidation;

namespace Autonomy.Api.Validation;

public sealed class CreateTenantRoleAssignmentRequestValidator
    : AbstractValidator<CreateTenantRoleAssignmentRequest>
{
    public CreateTenantRoleAssignmentRequestValidator()
    {
        RuleFor(request => request.UserId).NotEmpty();
        RuleFor(request => request.TenantId).NotEmpty();
        RuleFor(request => request.RoleName)
            .Must(AuthorizationPolicies.IsRoleAllowedForTenant)
            .WithMessage("RoleName is not valid for a fleet tenant.");
        RuleFor(request => request.MerchantId)
            .NotEmpty()
            .When(request => request.RoleName == AuthorizationRoles.MerchantUser);
        RuleFor(request => request.MerchantId)
            .Null()
            .When(request => request.RoleName != AuthorizationRoles.MerchantUser);
    }
}
