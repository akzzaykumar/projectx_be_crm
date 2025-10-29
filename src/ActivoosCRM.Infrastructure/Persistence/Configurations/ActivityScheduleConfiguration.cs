using ActivoosCRM.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ActivoosCRM.Infrastructure.Persistence.Configurations;

public class ActivityScheduleConfiguration : IEntityTypeConfiguration<ActivitySchedule>
{
    public void Configure(EntityTypeBuilder<ActivitySchedule> builder)
    {
        builder.ToTable("activity_schedules");

        builder.HasKey(asch => asch.Id);

        builder.Property(asch => asch.StartTime)
            .IsRequired();

        builder.Property(asch => asch.EndTime)
            .IsRequired();

        // Store days of week as JSON array
        builder.Property(asch => asch.DaysOfWeek)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(asch => asch.AvailableSpots)
            .IsRequired();

        builder.Property(asch => asch.IsActive)
            .HasDefaultValue(true);

        // Relationships
        builder.HasOne(asch => asch.Activity)
            .WithMany(a => a.Schedules)
            .HasForeignKey(asch => asch.ActivityId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(asch => asch.ActivityId);
        builder.HasIndex(asch => asch.IsActive);
    }
}
