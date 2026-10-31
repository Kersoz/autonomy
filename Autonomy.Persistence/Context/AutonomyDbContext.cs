using System.Linq.Expressions;
using System.Text.Json;
using Autonomy.Application.Interfaces;
using Autonomy.Persistence.Audit;
using Autonomy.Domain.Common;
using Autonomy.Domain.Entities;
using Autonomy.Persistence.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Autonomy.Persistence.Context;

public class AutonomyDbContext
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    private readonly ITenantContext _tenantContext;

    public AutonomyDbContext(
        DbContextOptions<AutonomyDbContext> options,
        ITenantContext tenantContext)
        : base(options)
    {
        _tenantContext = tenantContext;
    }

    public Guid CurrentTenantId => _tenantContext.TenantId ?? Guid.Empty;

    public bool IsPlatformAdmin => _tenantContext.IsPlatformAdmin;

    public DbSet<Assignment> Assignments => Set<Assignment>();
    public DbSet<Courier> Couriers => Set<Courier>();
    public DbSet<CourierAppInstallation> CourierAppInstallations => Set<CourierAppInstallation>();
    public DbSet<CourierLocation> CourierLocations => Set<CourierLocation>();
    public DbSet<Delivery> Deliveries => Set<Delivery>();
    public DbSet<DeliveryStatusHistory> DeliveryStatusHistories => Set<DeliveryStatusHistory>();
    public DbSet<Fleet> Fleets => Set<Fleet>();
    public DbSet<FleetPolicy> FleetPolicies => Set<FleetPolicy>();
    public DbSet<JobOffer> JobOffers => Set<JobOffer>();
    public DbSet<Merchant> Merchants => Set<Merchant>();
    public DbSet<TenantRoleAssignment> TenantRoleAssignments => Set<TenantRoleAssignment>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public override int SaveChanges()
    {
        PrepareChanges();
        return base.SaveChanges();
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        PrepareChanges();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        PrepareChanges();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        PrepareChanges();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ApplyTenantQueryFilters(modelBuilder);

        modelBuilder.Entity<Assignment>()
            .HasOne(assignment => assignment.Delivery)
            .WithMany()
            .HasForeignKey(assignment => assignment.DeliveryId);

        modelBuilder.Entity<Delivery>()
            .HasOne(delivery => delivery.ActiveAssignment)
            .WithOne()
            .HasForeignKey<Delivery>(delivery => delivery.ActiveAssignmentId);

        modelBuilder.Entity<TenantRoleAssignment>(entity =>
        {
            entity.HasKey(assignment => assignment.Id);
            entity.Property(assignment => assignment.RoleName)
                .HasMaxLength(64)
                .IsRequired();
            entity.HasIndex(assignment => new
            {
                assignment.UserId,
                assignment.TenantId,
                assignment.RoleName
            })
                .IsUnique()
                .HasFilter("\"MerchantId\" IS NULL");
            entity.HasIndex(assignment => new
            {
                assignment.UserId,
                assignment.TenantId,
                assignment.MerchantId,
                assignment.RoleName
            })
                .IsUnique()
                .HasFilter("\"MerchantId\" IS NOT NULL");
            entity.HasOne(assignment => assignment.User)
                .WithMany()
                .HasForeignKey(assignment => assignment.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(token => token.Id);
            entity.Property(token => token.TokenHash)
                .HasMaxLength(64)
                .IsRequired();
            entity.HasIndex(token => token.TokenHash)
                .IsUnique();
            entity.HasIndex(token => new { token.UserId, token.ExpiresAtUtc });
            entity.HasOne(token => token.User)
                .WithMany()
                .HasForeignKey(token => token.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        var roleIds = new[]
        {
            Guid.Parse("d23be2ec-37c4-48b0-80a6-b0d7b226d101"),
            Guid.Parse("d23be2ec-37c4-48b0-80a6-b0d7b226d102"),
            Guid.Parse("d23be2ec-37c4-48b0-80a6-b0d7b226d103"),
            Guid.Parse("d23be2ec-37c4-48b0-80a6-b0d7b226d104"),
            Guid.Parse("d23be2ec-37c4-48b0-80a6-b0d7b226d105")
        };
        modelBuilder.Entity<IdentityRole<Guid>>().HasData(
            AuthorizationRoles.All.Select((role, index) => new IdentityRole<Guid>
            {
                Id = roleIds[index],
                Name = role,
                NormalizedName = role.ToUpperInvariant(),
                ConcurrencyStamp = roleIds[index].ToString()
            }));

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(log => log.Id);
            entity.Property(log => log.EntityName).HasMaxLength(256).IsRequired();
            entity.Property(log => log.EntityId).HasMaxLength(128).IsRequired();
            entity.Property(log => log.Action).HasMaxLength(32).IsRequired();
            entity.Property(log => log.ChangesJson).IsRequired();
            entity.Property(log => log.CorrelationId).HasMaxLength(128);
            entity.HasIndex(log => new { log.TenantId, log.OccurredAtUtc });
            entity.HasIndex(log => new { log.EntityName, log.EntityId });
        });
    }

    private void ApplyTenantQueryFilters(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                     .Where(type => typeof(ITenantEntity).IsAssignableFrom(type.ClrType)))
        {
            var parameter = Expression.Parameter(entityType.ClrType, "entity");
            var tenantEntity = Expression.Convert(parameter, typeof(ITenantEntity));
            var entityTenantId = Expression.Property(
                tenantEntity,
                nameof(ITenantEntity.TenantId));

            var context = Expression.Constant(this);
            var isPlatformAdmin = Expression.Property(context, nameof(IsPlatformAdmin));
            var currentTenantId = Expression.Property(context, nameof(CurrentTenantId));
            var tenantMatches = Expression.Equal(entityTenantId, currentTenantId);
            var filterBody = Expression.OrElse(isPlatformAdmin, tenantMatches);
            var filter = Expression.Lambda(filterBody, parameter);

            var entity = modelBuilder.Entity(entityType.ClrType);
            entity.HasQueryFilter(filter);
            entity.HasIndex(nameof(ITenantEntity.TenantId));
        }
    }

    private void PrepareChanges()
    {
        ChangeTracker.DetectChanges();
        var auditEntries = new List<AuditLog>();

        foreach (var entry in ChangeTracker.Entries()
                     .Where(entry => entry.Entity is ITenantEntity
                         && entry.Entity is not AuditLog
                         && entry.State is EntityState.Added
                             or EntityState.Modified
                             or EntityState.Deleted))
        {
            var tenantEntity = (ITenantEntity)entry.Entity;
            var now = DateTime.UtcNow;

            if (entry.State == EntityState.Added)
            {
                if (entry.Entity is BaseEntity baseEntity
                    && baseEntity.Id == Guid.Empty)
                {
                    baseEntity.Id = Guid.NewGuid();
                }
                else if (entry.Metadata.FindProperty("Id") is not null
                    && entry.Property("Id").CurrentValue is Guid id
                    && id == Guid.Empty)
                {
                    entry.Property("Id").CurrentValue = Guid.NewGuid();
                }

                if (entry.Entity is Fleet)
                {
                    if (tenantEntity.TenantId != Guid.Empty
                        && tenantEntity.TenantId != ((BaseEntity)entry.Entity).Id)
                    {
                        throw new InvalidOperationException(
                            "A fleet's TenantId must equal its Id.");
                    }

                    tenantEntity.TenantId = ((BaseEntity)entry.Entity).Id;
                }
                else if (TryGetFleetId(entry, out var fleetId))
                {
                    if (tenantEntity.TenantId != Guid.Empty
                        && tenantEntity.TenantId != fleetId)
                    {
                        throw new InvalidOperationException(
                            $"TenantId must match FleetId for {entry.Metadata.ClrType.Name}.");
                    }

                    tenantEntity.TenantId = fleetId;
                }
                else if (tenantEntity.TenantId == Guid.Empty
                    && !_tenantContext.IsPlatformAdmin
                    && _tenantContext.TenantId.HasValue)
                {
                    tenantEntity.TenantId = _tenantContext.TenantId.Value;
                }

                if (tenantEntity.TenantId == Guid.Empty)
                {
                    throw new InvalidOperationException(
                        $"A tenant ID is required for {entry.Metadata.ClrType.Name}.");
                }

                EnsureTenantAccess(tenantEntity.TenantId, entry.Metadata.ClrType.Name);

                if (entry.Entity is BaseEntity createdEntity)
                {
                    createdEntity.CreatedAt = now;
                    createdEntity.CreatedBy = _tenantContext.UserId;
                    createdEntity.UpdatedAt = now;
                    createdEntity.UpdatedBy = _tenantContext.UserId;
                }
            }
            else
            {
                EnsureTenantAccess(tenantEntity.TenantId, entry.Metadata.ClrType.Name);
                if (entry.Entity is Fleet fleet
                    && tenantEntity.TenantId != fleet.Id)
                {
                    throw new InvalidOperationException(
                        "A fleet's TenantId must equal its Id.");
                }

                if (TryGetFleetId(entry, out var fleetId)
                    && tenantEntity.TenantId != fleetId)
                {
                    throw new InvalidOperationException(
                        $"TenantId must match FleetId for {entry.Metadata.ClrType.Name}.");
                }

                if (entry.Property(nameof(ITenantEntity.TenantId)).IsModified)
                {
                    throw new InvalidOperationException("TenantId cannot be changed after creation.");
                }

                if (entry.Entity is BaseEntity updatedEntity)
                {
                    updatedEntity.UpdatedAt = now;
                    updatedEntity.UpdatedBy = _tenantContext.UserId;
                    entry.Property(nameof(BaseEntity.UpdatedAt)).IsModified = true;
                    entry.Property(nameof(BaseEntity.UpdatedBy)).IsModified = true;
                }
            }

            auditEntries.Add(CreateAuditLog(entry, tenantEntity.TenantId, now));
        }

        if (auditEntries.Count > 0)
        {
            AuditLogs.AddRange(auditEntries);
        }
    }

    private void EnsureTenantAccess(Guid entityTenantId, string entityName)
    {
        if (_tenantContext.IsPlatformAdmin)
        {
            return;
        }

        if (_tenantContext.TenantId is not Guid currentTenantId
            || currentTenantId != entityTenantId)
        {
            throw new UnauthorizedAccessException(
                $"The active tenant cannot modify {entityName} in another tenant.");
        }
    }

    private static bool TryGetFleetId(
        Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry,
        out Guid fleetId)
    {
        var property = entry.Metadata.FindProperty("FleetId");
        if (property is not null
            && entry.Property("FleetId").CurrentValue is Guid value
            && value != Guid.Empty)
        {
            fleetId = value;
            return true;
        }

        fleetId = Guid.Empty;
        return false;
    }

    private AuditLog CreateAuditLog(
        Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry,
        Guid tenantId,
        DateTime occurredAtUtc)
    {
        var changes = entry.Properties
            .Where(property => entry.State != EntityState.Modified || property.IsModified)
            .Where(property => property.Metadata.Name != nameof(ITenantEntity.TenantId))
            .Select(property => new
            {
                Property = property.Metadata.Name,
                OldValue = IsSensitive(property.Metadata.Name)
                    ? "[REDACTED]"
                    : property.OriginalValue,
                NewValue = IsSensitive(property.Metadata.Name)
                    ? "[REDACTED]"
                    : property.CurrentValue
            });

        return new AuditLog
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EntityName = entry.Metadata.ClrType.Name,
            EntityId = entry.Property("Id").CurrentValue?.ToString()
                ?? string.Empty,
            Action = entry.State.ToString(),
            ChangesJson = JsonSerializer.Serialize(changes),
            ActorUserId = _tenantContext.UserId,
            OccurredAtUtc = occurredAtUtc,
            CorrelationId = _tenantContext.CorrelationId
        };
    }

    private static bool IsSensitive(string propertyName)
    {
        return propertyName.Contains("password", StringComparison.OrdinalIgnoreCase)
            || propertyName.Contains("phone", StringComparison.OrdinalIgnoreCase)
            || propertyName.Contains("address", StringComparison.OrdinalIgnoreCase)
            || propertyName.Contains("email", StringComparison.OrdinalIgnoreCase)
            || propertyName.Contains("token", StringComparison.OrdinalIgnoreCase)
            || propertyName.Contains("name", StringComparison.OrdinalIgnoreCase)
            || propertyName.Contains("notes", StringComparison.OrdinalIgnoreCase)
            || propertyName.Contains("instructions", StringComparison.OrdinalIgnoreCase);
    }
}