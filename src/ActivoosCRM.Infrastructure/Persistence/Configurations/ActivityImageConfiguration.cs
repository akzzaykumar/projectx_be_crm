using ActivoosCRM.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ActivoosCRM.Infrastructure.Persistence.Configurations;

public class ActivityImageConfiguration : IEntityTypeConfiguration<ActivityImage>
{
    public void Configure(EntityTypeBuilder<ActivityImage> builder)
    {
        builder.ToTable("activity_images");

        builder.HasKey(ai => ai.Id);

        builder.Property(ai => ai.ImageUrl)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(ai => ai.Caption)
            .HasMaxLength(255);

        builder.Property(ai => ai.IsPrimary)
            .HasDefaultValue(false);

        builder.Property(ai => ai.SortOrder)
            .HasDefaultValue(0);

        // Relationships
        builder.HasOne(ai => ai.Activity)
            .WithMany(a => a.Images)
            .HasForeignKey(ai => ai.ActivityId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(ai => ai.ActivityId);
        builder.HasIndex(ai => new { ai.ActivityId, ai.IsPrimary });
        builder.HasIndex(ai => new { ai.ActivityId, ai.SortOrder });
    }
}
