using System.Security.Claims;
using Autonomy.Persistence.Identity;

namespace Autonomy.Api.Authorization;

public static class AuthorizationPolicies
{
    public const string PlatformAdmin = nameof(PlatformAdmin);
    public const string FleetAdmin = nameof(FleetAdmin);
    public const string UserProvisioning = nameof(UserProvisioning);
    public const string FleetOperations = nameof(FleetOperations);
    public const string MerchantOperations = nameof(MerchantOperations);
    public const string CourierOperations = nameof(CourierOperations);
    public const string DeliveryOperations = nameof(DeliveryOperations);
    public const string DeliveryManagement = nameof(DeliveryManagement);
    public const string DeliveryDispatch = nameof(DeliveryDispatch);

    public const string TenantIdClaim = "tenant_id";
    public const string MerchantIdClaim = "merchant_id";

    public static bool HasTenant(ClaimsPrincipal user)
    {
        return Guid.TryParse(user.FindFirstValue(TenantIdClaim), out _);
    }

    public static bool HasMerchant(ClaimsPrincipal user)
    {
        return HasTenant(user)
            && Guid.TryParse(user.FindFirstValue(MerchantIdClaim), out _);
    }

    public static bool IsPlatformAdmin(ClaimsPrincipal user)
    {
        return user.IsInRole(AuthorizationRoles.PlatformAdmin);
    }

    public static bool IsFleetAdmin(ClaimsPrincipal user)
    {
        return user.IsInRole(AuthorizationRoles.FleetAdmin)
            && HasTenant(user);
    }

    public static bool IsRoleAllowedForTenant(string role)
    {
        return role is AuthorizationRoles.FleetAdmin
            or AuthorizationRoles.Dispatcher
            or AuthorizationRoles.MerchantUser
            or AuthorizationRoles.Courier;
    }
}
