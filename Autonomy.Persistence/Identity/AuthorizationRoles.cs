namespace Autonomy.Persistence.Identity;

public static class AuthorizationRoles
{
    public const string PlatformAdmin = "PlatformAdmin";
    public const string FleetAdmin = "FleetAdmin";
    public const string Dispatcher = "Dispatcher";
    public const string MerchantUser = "MerchantUser";
    public const string Courier = "Courier";

    public static readonly string[] All =
    [
        PlatformAdmin,
        FleetAdmin,
        Dispatcher,
        MerchantUser,
        Courier
    ];
}
