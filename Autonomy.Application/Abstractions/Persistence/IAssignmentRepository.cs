namespace Autonomy.Application.Abstractions.Persistence;

public interface IAssignmentRepository
{
    Task<T> ExecuteAtomicallyAsync<T>(
        Guid deliveryId,
        Guid? targetCourierId,
        Func<AssignmentTransaction, T> operation,
        CancellationToken cancellationToken = default);
}
