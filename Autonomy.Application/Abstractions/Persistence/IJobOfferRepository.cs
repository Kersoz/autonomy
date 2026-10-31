using Autonomy.Application.Abstractions.Persistence;

namespace Autonomy.Application.Abstractions.Persistence;

public interface IJobOfferRepository
{
    Task<T> ExecuteAtomicallyAsync<T>(
        Guid? offerId,
        Guid? deliveryId,
        Guid? courierId,
        Func<JobOfferTransaction, T> operation,
        CancellationToken cancellationToken = default);

    Task<int> ExpirePendingOffersAsync(
        DateTime now,
        CancellationToken cancellationToken = default);
}
