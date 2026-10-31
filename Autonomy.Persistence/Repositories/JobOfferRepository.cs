using Autonomy.Application.Abstractions.Persistence;
using Autonomy.Domain.Entities;
using Autonomy.Domain.Enums;
using Autonomy.Domain.Exceptions;
using Autonomy.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace Autonomy.Persistence.Repositories;

public sealed class JobOfferRepository(AutonomyDbContext context) : IJobOfferRepository
{
    public async Task<T> ExecuteAtomicallyAsync<T>(
        Guid? offerId,
        Guid? deliveryId,
        Guid? courierId,
        Func<JobOfferTransaction, T> operation,
        CancellationToken cancellationToken = default)
    {
        await using var dbTransaction =
            await context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            JobOffer? offer = null;
            if (offerId is Guid requestedOfferId)
            {
                await LockRowAsync("JobOffers", requestedOfferId, cancellationToken);
                offer = await context.JobOffers.SingleOrDefaultAsync(
                    item => item.Id == requestedOfferId,
                    cancellationToken);
                if (offer is null)
                {
                    var missingOfferResult = operation(
                        new JobOfferTransaction(null, null, null, null));
                    await dbTransaction.CommitAsync(cancellationToken);
                    return missingOfferResult;
                }

                deliveryId = offer.DeliveryId;
                courierId = offer.CourierId;
            }

            Delivery? delivery = null;
            Assignment? activeAssignment = null;
            Courier? courier = null;

            if (deliveryId is Guid requestedDeliveryId)
            {
                await LockRowAsync("Deliveries", requestedDeliveryId, cancellationToken);
                delivery = await context.Deliveries.SingleOrDefaultAsync(
                    item => item.Id == requestedDeliveryId,
                    cancellationToken);

                if (delivery is not null)
                {
                    activeAssignment = await context.Assignments.SingleOrDefaultAsync(
                        assignment => assignment.DeliveryId == requestedDeliveryId
                            && assignment.Status == AssignmentStatus.Active,
                        cancellationToken);
                }
            }

            if (courierId is Guid requestedCourierId)
            {
                await LockRowAsync("Couriers", requestedCourierId, cancellationToken);
                courier = await context.Couriers.SingleOrDefaultAsync(
                    item => item.Id == requestedCourierId,
                    cancellationToken);
            }

            var result = operation(new JobOfferTransaction(
                offer,
                delivery,
                activeAssignment,
                courier));
            if (result is JobOffer createdOffer
                && context.Entry(createdOffer).State == EntityState.Detached)
            {
                context.JobOffers.Add(createdOffer);
            }

            await context.SaveChangesAsync(cancellationToken);
            await dbTransaction.CommitAsync(cancellationToken);
            return result;
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException postgresException
                && postgresException.SqlState is PostgresErrorCodes.UniqueViolation
                    or PostgresErrorCodes.CheckViolation)
        {
            throw new AssignmentConflictException(
                "The delivery or courier no longer satisfies assignment constraints.");
        }
        catch (PostgresException exception)
            when (exception.SqlState is PostgresErrorCodes.UniqueViolation
                or PostgresErrorCodes.CheckViolation)
        {
            throw new AssignmentConflictException(
                "The delivery or courier no longer satisfies assignment constraints.");
        }
    }

    public async Task<int> ExpirePendingOffersAsync(
        DateTime now,
        CancellationToken cancellationToken = default)
    {
        const int batchSize = 500;
        var totalExpired = 0;

        while (true)
        {
            await using var dbTransaction =
                await context.Database.BeginTransactionAsync(cancellationToken);
            var offerIds = await context.JobOffers
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(offer => offer.Status == OfferStatus.Pending && offer.ExpiresAt <= now)
                .OrderBy(offer => offer.ExpiresAt)
                .Select(offer => offer.Id)
                .Take(batchSize)
                .ToListAsync(cancellationToken);

            if (offerIds.Count == 0)
            {
                await dbTransaction.CommitAsync(cancellationToken);
                return totalExpired;
            }

            var expiredInBatch = 0;
            foreach (var offerId in offerIds)
            {
                await LockRowAsync("JobOffers", offerId, cancellationToken);
                var offer = await context.JobOffers
                    .IgnoreQueryFilters()
                    .SingleOrDefaultAsync(
                        item => item.Id == offerId
                            && item.Status == OfferStatus.Pending
                            && item.ExpiresAt <= now,
                        cancellationToken);
                if (offer is null)
                {
                    continue;
                }

                offer.Expire(now);
                expiredInBatch++;
            }

            await context.SaveChangesAsync(cancellationToken);
            await dbTransaction.CommitAsync(cancellationToken);
            totalExpired += expiredInBatch;

            if (offerIds.Count < batchSize)
            {
                return totalExpired;
            }
        }
    }

    private async Task LockRowAsync(
        string tableName,
        Guid entityId,
        CancellationToken cancellationToken)
    {
        var quotedTable = tableName switch
        {
            "JobOffers" => "\"JobOffers\"",
            "Deliveries" => "\"Deliveries\"",
            "Couriers" => "\"Couriers\"",
            _ => throw new ArgumentOutOfRangeException(nameof(tableName))
        };
        await using var command = context.Database.GetDbConnection().CreateCommand();
        command.Transaction = context.Database.CurrentTransaction?.GetDbTransaction()
            ?? throw new InvalidOperationException("The job offer transaction is not active.");
        command.CommandText = $"SELECT \"Id\" FROM {quotedTable} WHERE \"Id\" = @id FOR UPDATE";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "id";
        parameter.Value = entityId;
        command.Parameters.Add(parameter);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        await reader.ReadAsync(cancellationToken);
    }
}
