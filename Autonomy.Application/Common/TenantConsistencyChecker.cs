using Autonomy.Domain.Common;

namespace Autonomy.Application.Common;

public static class TenantConsistencyChecker
{
    public static void EnsureSameTenant(ITenantEntity first, ITenantEntity second)
    {
        EnsureSameTenant(first.TenantId, second.TenantId);
    }

    public static void EnsureSameTenant(Guid firstTenantId, Guid secondTenantId)
    {
        if (firstTenantId == Guid.Empty
            || secondTenantId == Guid.Empty
            || firstTenantId != secondTenantId)
        {
            throw new UnauthorizedAccessException(
                "Entities must belong to the same tenant.");
        }
    }
}
