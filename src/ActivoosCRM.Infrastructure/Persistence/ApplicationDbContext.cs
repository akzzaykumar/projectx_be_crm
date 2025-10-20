using ActivoosCRM.Application.Common.Interfaces;
using ActivoosCRM.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace ActivoosCRM.Infrastructure.Persistence;

/// <summary>
/// Application database context
/// </summary>
public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Activity> Activities => Set<Activity>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<TimeSlot> TimeSlots => Set<TimeSlot>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all configurations from current assembly
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // Configure table names
        modelBuilder.Entity<User>().ToTable("Users");
        modelBuilder.Entity<Customer>().ToTable("Customers");
        modelBuilder.Entity<Activity>().ToTable("Activities");
        modelBuilder.Entity<Booking>().ToTable("Bookings");
        modelBuilder.Entity<TimeSlot>().ToTable("TimeSlots");

        // Configure indexes
        ConfigureIndexes(modelBuilder);

        // Configure relationships
        ConfigureRelationships(modelBuilder);
    }

    private void ConfigureIndexes(ModelBuilder modelBuilder)
    {
        // Users indexes
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        // Customers indexes
        modelBuilder.Entity<Customer>()
            .HasIndex(c => c.Email);

        modelBuilder.Entity<Customer>()
            .HasIndex(c => c.Status);

        modelBuilder.Entity<Customer>()
            .HasIndex(c => c.CreatedAt);

        // Activities indexes
        modelBuilder.Entity<Activity>()
            .HasIndex(a => a.Category);

        modelBuilder.Entity<Activity>()
            .HasIndex(a => a.Price);

        // Bookings indexes
        modelBuilder.Entity<Booking>()
            .HasIndex(b => b.CustomerId);

        modelBuilder.Entity<Booking>()
            .HasIndex(b => b.ActivityId);

        modelBuilder.Entity<Booking>()
            .HasIndex(b => b.Date);

        modelBuilder.Entity<Booking>()
            .HasIndex(b => b.Status);

        modelBuilder.Entity<Booking>()
            .HasIndex(b => b.CreatedAt);
    }

    private void ConfigureRelationships(ModelBuilder modelBuilder)
    {
        // Customer relationships
        modelBuilder.Entity<Customer>()
            .HasOne(c => c.CreatedBy)
            .WithMany(u => u.CreatedCustomers)
            .HasForeignKey(c => c.CreatedById)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Customer>()
            .HasMany(c => c.Bookings)
            .WithOne(b => b.Customer)
            .HasForeignKey(b => b.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        // Activity relationships
        modelBuilder.Entity<Activity>()
            .HasOne(a => a.CreatedBy)
            .WithMany(u => u.CreatedActivities)
            .HasForeignKey(a => a.CreatedById)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Activity>()
            .HasMany(a => a.TimeSlots)
            .WithOne(ts => ts.Activity)
            .HasForeignKey(ts => ts.ActivityId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Activity>()
            .HasMany(a => a.Bookings)
            .WithOne(b => b.Activity)
            .HasForeignKey(b => b.ActivityId)
            .OnDelete(DeleteBehavior.Restrict);

        // Booking relationships
        modelBuilder.Entity<Booking>()
            .HasOne(b => b.CreatedBy)
            .WithMany(u => u.CreatedBookings)
            .HasForeignKey(b => b.CreatedById)
            .OnDelete(DeleteBehavior.SetNull);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Update timestamps
        foreach (var entry in ChangeTracker.Entries<Domain.Common.BaseEntity>())
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

        return base.SaveChangesAsync(cancellationToken);
    }
}
