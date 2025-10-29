using ActivoosCRM.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ActivoosCRM.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity configuration for ActivityProvider
/// Responsible for: ActivityProvider table schema, user relationship
/// </summary>
public class ActivityProviderConfiguration : IEntityTypeConfiguration<ActivityProvider>
{
    public void Configure(EntityTypeBuilder<ActivityProvider> builder)
    {
        builder.ToTable("activity_providers");

        // Primary key
        builder.HasKey(ap => ap.Id);

        // Properties
        builder.Property(ap => ap.BusinessName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(ap => ap.BusinessRegistrationNumber)
            .HasMaxLength(100);

        builder.Property(ap => ap.TaxIdentificationNumber)
            .HasMaxLength(100);

        builder.Property(ap => ap.BusinessEmail)
            .HasMaxLength(255);

        builder.Property(ap => ap.BusinessPhone)
            .HasMaxLength(20);

        builder.Property(ap => ap.Website)
            .HasMaxLength(500);
            
        builder.Property(ap => ap.InstagramHandle)
            .HasMaxLength(100);

        builder.Property(ap => ap.FacebookUrl)
            .HasMaxLength(500);

        builder.Property(ap => ap.Description)
            .HasMaxLength(2000);

        builder.Property(ap => ap.LogoUrl)
            .HasMaxLength(500);

        builder.Property(ap => ap.AddressLine1)
            .HasMaxLength(255);

        builder.Property(ap => ap.AddressLine2)
            .HasMaxLength(255);

        builder.Property(ap => ap.City)
            .HasMaxLength(100);

        builder.Property(ap => ap.State)
            .HasMaxLength(100);

        builder.Property(ap => ap.Country)
            .HasMaxLength(100);

        builder.Property(ap => ap.PostalCode)
            .HasMaxLength(20);

        builder.Property(ap => ap.BankAccountName)
            .HasMaxLength(200);

        builder.Property(ap => ap.BankAccountNumber)
            .HasMaxLength(100);

        builder.Property(ap => ap.BankName)
            .HasMaxLength(200);

        builder.Property(ap => ap.BankBranchCode)
            .HasMaxLength(50);

        builder.Property(ap => ap.PaymentGatewayId)
            .HasMaxLength(200);

        builder.Property(ap => ap.RejectionReason)
            .HasMaxLength(500);

        builder.Property(ap => ap.AverageRating)
            .HasPrecision(3, 2);

        // Audit fields
        builder.Property(ap => ap.CreatedAt)
            .IsRequired();

        builder.Property(ap => ap.UpdatedAt)
            .IsRequired();

        // Relationships
        builder.HasOne(ap => ap.User)
            .WithOne(u => u.ActivityProvider)
            .HasForeignKey<ActivityProvider>(ap => ap.UserId)
            .OnDelete(DeleteBehavior.Restrict);
            
        builder.HasOne(ap => ap.Location)
            .WithMany()
            .HasForeignKey(ap => ap.LocationId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        // Indexes
        builder.HasIndex(ap => ap.UserId)
            .IsUnique()
            .HasDatabaseName("ix_activity_providers_user_id");

        builder.HasIndex(ap => ap.BusinessName)
            .HasDatabaseName("ix_activity_providers_business_name");

        builder.HasIndex(ap => ap.IsVerified)
            .HasDatabaseName("ix_activity_providers_is_verified");

        builder.HasIndex(ap => ap.IsActive)
            .HasDatabaseName("ix_activity_providers_is_active");

        builder.HasIndex(ap => ap.AverageRating)
            .HasDatabaseName("ix_activity_providers_average_rating");

        // Ignore computed properties
        builder.Ignore(ap => ap.DomainEvents);
    }
}
