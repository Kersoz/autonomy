using Autonomy.Application.Abstractions.Persistence;
using Autonomy.Domain.Entities;
using Autonomy.Domain.Enums;
using Autonomy.Domain.Exceptions;
using Autonomy.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace Autonomy.Persistence.Repositories;

public sealed class AssignmentRepository(AutonomyDbContext context) : IAssignmentRepository
{
    public async Task<T> ExecuteAtomicallyAsync<T>(
        Guid deliveryId,
        Guid? targetCourierId,
        Func<AssignmentTransaction, T> operation,
        CancellationToken cancellationToken = default)
    {
        await using var dbTransaction =
            await context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            await LockRowAsync("Deliveries", deliveryId, cancellationToken);
            var delivery = await context.Deliveries.SingleOrDefaultAsync(
                item => item.Id == deliveryId,
                cancellationToken);
            if (delivery is null)
            {
                var missingResult = operation(new AssignmentTransaction(
                    null,
                    null,
                    new Dictionary<Guid, Courier>()));
                await dbTransaction.CommitAsync(cancellationToken);
                return missingResult;
            }

            var activeAssignment = await context.Assignments.SingleOrDefaultAsync(
                assignment => assignment.DeliveryId == deliveryId
                    && assignment.Status == AssignmentStatus.Active,
                cancellationToken);
            var courierIds = new HashSet<Guid>();
            if (targetCourierId is Guid targetId)
            {
                courierIds.Add(targetId);
            }

            if (delivery.AssignedCourierId is Guid assignedCourierId)
            {
                courierIds.Add(assignedCourierId);
            }

            if (activeAssignment is not null)
            {
                courierIds.Add(activeAssignment.CourierId);
            }

            foreach (var courierId in courierIds.Order())
            {
                await LockRowAsync("Couriers", courierId, cancellationToken);
            }

            var couriers = await context.Couriers
                .Where(courier => courierIds.Contains(courier.Id))
                .ToDictionaryAsync(courier => courier.Id, cancellationToken);
            var result = operation(new AssignmentTransaction(
                delivery,
                activeAssignment,
                couriers));
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

    private async Task LockRowAsync(
        string tableName,
        Guid entityId,
        CancellationToken cancellationToken)
    {
        var quotedTable = tableName switch
        {
            "Deliveries" => "\"Deliveries\"",
            "Couriers" => "\"Couriers\"",
            _ => throw new ArgumentOutOfRangeException(nameof(tableName))
        };
        await using var command = context.Database.GetDbConnection().CreateCommand();
        command.Transaction = context.Database.CurrentTransaction?.GetDbTransaction()
            ?? throw new InvalidOperationException("The assignment transaction is not active.");
        command.CommandText = $"SELECT \"Id\" FROM {quotedTable} WHERE \"Id\" = @id FOR UPDATE";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "id";
        parameter.Value = entityId;
        command.Parameters.Add(parameter);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        await reader.ReadAsync(cancellationToken);
    }
}