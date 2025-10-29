using ActivoosCRM.Application.Common.Interfaces;
using ActivoosCRM.Domain.Common;
using ActivoosCRM.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace ActivoosCRM.Infrastructure.Persistence;

/// <summary>
/// Application database context
/// </summary>
public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    private readonly ICurrentUserService? _currentUserService;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ICurrentUserService currentUserService)
        : base(options)
    {
        _currentUserService = currentUserService;
    }

    // DbSets for all entities
    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<LocationRequest> LocationRequests => Set<LocationRequest>();
    public DbSet<ActivityProvider> ActivityProviders => Set<ActivityProvider>();
    public DbSet<CustomerProfile> CustomerProfiles => Set<CustomerProfile>();
    public DbSet<Activity> Activities => Set<Activity>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Wishlist> Wishlists => Set<Wishlist>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Coupon> Coupons => Set<Coupon>();
    public DbSet<CouponUsage> CouponUsages => Set<CouponUsage>();
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();
    public DbSet<ActivityImage> ActivityImages => Set<ActivityImage>();
    public DbSet<ActivitySchedule> ActivitySchedules => Set<ActivitySchedule>();
    public DbSet<ActivityTag> ActivityTags => Set<ActivityTag>();
    public DbSet<BookingParticipant> BookingParticipants => Set<BookingParticipant>();
    public DbSet<ProviderContact> ProviderContacts => Set<ProviderContact>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Ignore DomainEvent (it's not a table)
        modelBuilder.Ignore<DomainEvent>();

        // Apply all entity configurations from current assembly
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var currentUserId = _currentUserService?.GetCurrentUserId();

        // Update audit timestamps for all BaseEntity types
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = DateTime.UtcNow;
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
        }

        // Update user tracking for AuditableEntity types
        // Note: This requires entities to inherit from AuditableEntity
        var auditableEntries = ChangeTracker.Entries()
            .Where(e => e.Entity is AuditableEntity)
            .ToList();

        foreach (var entry in auditableEntries)
        {
            var auditableEntity = (AuditableEntity)entry.Entity;

            if (entry.State == EntityState.Added && currentUserId.HasValue)
            {
                auditableEntity.CreatedBy = currentUserId.Value;
            }
            else if (entry.State == EntityState.Modified && currentUserId.HasValue)
            {
                auditableEntity.UpdatedBy = currentUserId.Value;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
