using Autonomy.Domain.Entities;

namespace Autonomy.Application.Abstractions.Persistence;

public sealed record JobOfferTransaction(
    JobOffer? Offer,
    Delivery? Delivery,
    Assignment? ActiveAssignment,
    Courier? Courier);
