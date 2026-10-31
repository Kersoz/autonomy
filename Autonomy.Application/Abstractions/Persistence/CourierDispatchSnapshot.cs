using Autonomy.Domain.Entities;

namespace Autonomy.Application.Abstractions.Persistence;

public sealed record CourierDispatchSnapshot(
    Courier Courier,
    CourierLocation? LatestLocation);
