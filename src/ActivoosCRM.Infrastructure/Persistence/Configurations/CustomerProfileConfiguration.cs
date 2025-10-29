using ActivoosCRM.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ActivoosCRM.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity configuration for CustomerProfile
/// Responsible for: CustomerProfile table schema, user relationship
/// </summary>
public class CustomerProfileConfiguration : IEntityTypeConfiguration<CustomerProfile>
{
    public void Configure(EntityTypeBuilder<CustomerProfile> builder)
    {
        builder.ToTable("customer_profiles");

        // Primary key
        builder.HasKey(cp => cp.Id);

        // Properties
        builder.Property(cp => cp.Gender)
            .HasMaxLength(20);

        builder.Property(cp => cp.ProfilePictureUrl)
            .HasMaxLength(500);

        builder.Property(cp => cp.Bio)
            .HasMaxLength(1000);

        builder.Property(cp => cp.DietaryRestrictions)
            .HasColumnType("text");

        builder.Property(cp => cp.MedicalConditions)
            .HasColumnType("text");

        builder.Property(cp => cp.AddressLine1)
            .HasMaxLength(255);

        builder.Property(cp => cp.AddressLine2)
            .HasMaxLength(255);

        builder.Property(cp => cp.City)
            .HasMaxLength(100);

        builder.Property(cp => cp.State)
            .HasMaxLength(100);

        builder.Property(cp => cp.Country)
            .HasMaxLength(100);

        builder.Property(cp => cp.PostalCode)
            .HasMaxLength(20);

        builder.Property(cp => cp.EmergencyContactName)
            .HasMaxLength(200);

        builder.Property(cp => cp.EmergencyContactPhone)
            .HasMaxLength(20);

        builder.Property(cp => cp.EmergencyContactRelationship)
            .HasMaxLength(100);

        builder.Property(cp => cp.PreferredLanguage)
            .HasMaxLength(10);

        builder.Property(cp => cp.PreferredCurrency)
            .HasMaxLength(3);

        builder.Property(cp => cp.TotalSpent)
            .HasPrecision(18, 2);

        // Audit fields
        builder.Property(cp => cp.CreatedAt)
            .IsRequired();

        builder.Property(cp => cp.UpdatedAt)
            .IsRequired();

        // Relationships
        builder.HasOne(cp => cp.User)
            .WithOne(u => u.CustomerProfile)
            .HasForeignKey<CustomerProfile>(cp => cp.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(cp => cp.UserId)
            .IsUnique()
            .HasDatabaseName("ix_customer_profiles_user_id");

        builder.HasIndex(cp => cp.TotalBookings)
            .HasDatabaseName("ix_customer_profiles_total_bookings");

        builder.HasIndex(cp => cp.LastBookingAt)
            .HasDatabaseName("ix_customer_profiles_last_booking_at");

        // Ignore computed properties
        builder.Ignore(cp => cp.DomainEvents);
    }
}
