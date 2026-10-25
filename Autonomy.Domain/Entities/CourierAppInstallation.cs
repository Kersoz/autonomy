using Autonomy.Domain.Common;

namespace Autonomy.Domain.Entities;

public class CourierAppInstallation : BaseEntity
{
    public Guid FleetId { get; set; }

    public Guid CourierId { get; set; }

    public string AppIdentity { get; set; } = null!;

    public string Platform { get; set; } = null!;

    public string PackageIdentifier { get; set; } = null!;

    public string Version { get; set; } = null!;

    public string BuildNumber { get; set; } = null!;

    public string DeviceId { get; set; } = null!;

    public bool IsRegistered { get; set; }

    public string? PushToken { get; set; }

    public DateTime? LastActivityAt { get; set; }

    public string? StoreIdentifier { get; set; }
}