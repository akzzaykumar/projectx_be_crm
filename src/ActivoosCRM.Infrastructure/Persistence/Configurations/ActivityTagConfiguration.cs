using ActivoosCRM.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ActivoosCRM.Infrastructure.Persistence.Configurations;

public class ActivityTagConfiguration : IEntityTypeConfiguration<ActivityTag>
{
    public void Configure(EntityTypeBuilder<ActivityTag> builder)
    {
        builder.ToTable("activity_tags");

        builder.HasKey(at => at.Id);

        builder.Property(at => at.Tag)
            .IsRequired()
            .HasMaxLength(50);

        // Relationships
        builder.HasOne(at => at.Activity)
            .WithMany(a => a.Tags)
            .HasForeignKey(at => at.ActivityId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique constraint
        builder.HasIndex(at => new { at.ActivityId, at.Tag }).IsUnique();

        // Additional indexes
        builder.HasIndex(at => at.Tag);
    }
}
