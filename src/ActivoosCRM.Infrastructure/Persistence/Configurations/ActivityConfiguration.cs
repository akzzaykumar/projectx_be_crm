using ActivoosCRM.Domain.Entities;
using ActivoosCRM.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ActivoosCRM.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity configuration for Activity
/// Responsible for: Activity table schema, provider/category/location relationships
/// </summary>
public class ActivityConfiguration : IEntityTypeConfiguration<Activity>
{
    public void Configure(EntityTypeBuilder<Activity> builder)
    {
        builder.ToTable("activities");

        // Primary key
        builder.HasKey(a => a.Id);

        // Properties
        builder.Property(a => a.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(a => a.Slug)
            .IsRequired()
            .HasMaxLength(250);

        builder.Property(a => a.Description)
            .IsRequired()
            .HasMaxLength(5000);

        builder.Property(a => a.ShortDescription)
            .HasMaxLength(500);

        builder.Property(a => a.CoverImageUrl)
            .HasMaxLength(500);

        builder.Property(a => a.Price)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(a => a.Currency)
            .IsRequired()
            .HasMaxLength(3);

        builder.Property(a => a.DiscountedPrice)
            .HasPrecision(18, 2);

        builder.Property(a => a.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(a => a.SkillLevel)
            .HasMaxLength(50);

        builder.Property(a => a.RequiredEquipment)
            .HasMaxLength(1000);

        builder.Property(a => a.ProvidedEquipment)
            .HasMaxLength(1000);

        builder.Property(a => a.SafetyInstructions)
            .HasMaxLength(2000);

        builder.Property(a => a.CancellationPolicy)
            .HasMaxLength(1000);

        builder.Property(a => a.RefundPolicy)
            .HasMaxLength(1000);

        builder.Property(a => a.AverageRating)
            .HasPrecision(3, 2);

        // Audit fields
        builder.Property(a => a.CreatedAt)
            .IsRequired();

        builder.Property(a => a.UpdatedAt)
            .IsRequired();

        // Relationships
        builder.HasOne(a => a.Provider)
            .WithMany(p => p.Activities)
            .HasForeignKey(a => a.ProviderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Category)
            .WithMany(c => c.Activities)
            .HasForeignKey(a => a.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Location)
            .WithMany(l => l.Activities)
            .HasForeignKey(a => a.LocationId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(a => a.Slug)
            .IsUnique()
            .HasDatabaseName("ix_activities_slug");

        builder.HasIndex(a => a.ProviderId)
            .HasDatabaseName("ix_activities_provider_id");

        builder.HasIndex(a => a.CategoryId)
            .HasDatabaseName("ix_activities_category_id");

        builder.HasIndex(a => a.LocationId)
            .HasDatabaseName("ix_activities_location_id");

        builder.HasIndex(a => a.Status)
            .HasDatabaseName("ix_activities_status");

        builder.HasIndex(a => a.IsActive)
            .HasDatabaseName("ix_activities_is_active");

        builder.HasIndex(a => a.IsFeatured)
            .HasDatabaseName("ix_activities_is_featured");

        builder.HasIndex(a => a.Price)
            .HasDatabaseName("ix_activities_price");

        builder.HasIndex(a => a.AverageRating)
            .HasDatabaseName("ix_activities_average_rating");

        builder.HasIndex(a => a.PublishedAt)
            .HasDatabaseName("ix_activities_published_at");

        // Ignore computed properties
        builder.Ignore(a => a.EffectivePrice);
        builder.Ignore(a => a.HasActiveDiscount);
        builder.Ignore(a => a.DomainEvents);
    }
}
