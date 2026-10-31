using Autonomy.Domain.Enums;

namespace Autonomy.Domain.Entities;

public sealed record DeliveryStatusTransition(
    Guid Id,
    DeliveryStatus PreviousStatus,
    DeliveryStatus NewStatus,
    DateTime OccurredAtUtc);
