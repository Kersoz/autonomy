using Autonomy.Domain.Entities;

namespace Autonomy.Application.Abstractions.Persistence;

public sealed record AssignmentTransaction(
    Delivery? Delivery,
    Assignment? ActiveAssignment,
    IReadOnlyDictionary<Guid, Courier> Couriers);
