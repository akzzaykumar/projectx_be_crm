using ActivoosCRM.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ActivoosCRM.Infrastructure.Persistence.Configurations;

public class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.ToTable("reviews");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Rating)
            .IsRequired();

        builder.Property(r => r.Title)
            .HasMaxLength(200);

        builder.Property(r => r.ReviewText)
            .HasColumnType("text");

        builder.Property(r => r.IsVerified)
            .HasDefaultValue(true);

        builder.Property(r => r.IsFeatured)
            .HasDefaultValue(false);

        builder.Property(r => r.HelpfulCount)
            .HasDefaultValue(0);

        // Relationships
        builder.HasOne(r => r.Booking)
            .WithOne(b => b.Review)
            .HasForeignKey<Review>(r => r.BookingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Customer)
            .WithMany(u => u.Reviews)
            .HasForeignKey(r => r.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Activity)
            .WithMany(a => a.Reviews)
            .HasForeignKey(r => r.ActivityId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Provider)
            .WithMany(p => p.Reviews)
            .HasForeignKey(r => r.ProviderId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(r => r.BookingId).IsUnique();
        builder.HasIndex(r => r.ActivityId);
        builder.HasIndex(r => r.ProviderId);
        builder.HasIndex(r => r.Rating);
        builder.HasIndex(r => r.IsFeatured);
        builder.HasIndex(r => r.CreatedAt);
    }
}
