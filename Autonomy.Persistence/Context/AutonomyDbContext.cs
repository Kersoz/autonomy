using Autonomy.Domain.Entities;
using Autonomy.Persistence.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
namespace Autonomy.Persistence.Context;
public class AutonomyDbContext
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public AutonomyDbContext(
        DbContextOptions<AutonomyDbContext> options)
        : base(options)
    {
    }
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
    public DbSet<ApplicationUser> Users => Set<ApplicationUser>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
    base.OnModelCreating(modelBuilder);

    modelBuilder.Entity<Assignment>()
        .HasOne(a => a.Delivery)
        .WithMany() // İstersen Delivery.Assignments koleksiyonunu ekleyip burada kullanabilirsin.
        .HasForeignKey(a => a.DeliveryId);

    modelBuilder.Entity<Delivery>()
        .HasOne(d => d.ActiveAssignment)
        .WithOne()
        .HasForeignKey<Delivery>(d => d.ActiveAssignmentId);
    }
}