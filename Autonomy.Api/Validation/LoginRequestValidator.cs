using Autonomy.Api.Contracts.Auth;
using FluentValidation;

namespace Autonomy.Api.Validation;

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(request => request.Email).NotEmpty().EmailAddress();
        RuleFor(request => request.Password).NotEmpty();
        RuleFor(request => request.MerchantId)
            .Null()
            .When(request => request.TenantId is null);
    }
}
