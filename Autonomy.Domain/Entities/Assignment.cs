using Autonomy.Domain.Common;
using Autonomy.Domain.Enums;

namespace Autonomy.Domain.Entities;

public class Assignment : BaseEntity
{
    public Guid FleetId { get; set; }

    public Fleet Fleet { get; set; } = null!;

    public Guid DeliveryId { get; set; }

    public Delivery Delivery { get; set; } = null!;

    public Guid CourierId { get; set; }

    public Courier Courier { get; set; } = null!;

    public DateTime AssignedAt { get; set; }

    public AssignmentSource Source { get; set; }

    public AssignmentStatus Status { get; set; }

    public DateTime? EndedAt { get; set; }

    public Guid? RelatedOfferId { get; set; }

    public JobOffer? RelatedOffer { get; set; }

    public Guid? DispatcherUserId { get; set; }

    public string? ReleaseReason { get; set; }

    public Guid? PreviousAssignmentId { get; set; }

    public Assignment? PreviousAssignment { get; set; }

    public DispatchOrigin DispatchOrigin { get; set; }

    public string? SelectedBy { get; set; }

    public string? ReleasedBy { get; set; }

    public string? PolicyVersion { get; set; }
}