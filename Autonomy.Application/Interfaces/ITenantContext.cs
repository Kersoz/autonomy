namespace Autonomy.Application.Interfaces;

public interface ITenantContext
{
    Guid? TenantId { get; }

    Guid? MerchantId { get; }

    Guid? UserId { get; }

    bool IsPlatformAdmin { get; }

    string? CorrelationId { get; }
}
