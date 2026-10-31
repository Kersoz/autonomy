using Autonomy.Domain.Enums;

namespace Autonomy.Domain.Exceptions;

public sealed class InvalidDeliveryTransitionException(
    DeliveryStatus currentStatus,
    DeliveryStatus targetStatus)
    : InvalidOperationException(
        $"A delivery cannot transition from {currentStatus} to {targetStatus}.")
{
    public DeliveryStatus CurrentStatus { get; } = currentStatus;

    public DeliveryStatus TargetStatus { get; } = targetStatus;
}
