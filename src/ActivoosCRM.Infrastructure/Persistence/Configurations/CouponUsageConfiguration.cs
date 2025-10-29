using ActivoosCRM.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ActivoosCRM.Infrastructure.Persistence.Configurations;

public class CouponUsageConfiguration : IEntityTypeConfiguration<CouponUsage>
{
    public void Configure(EntityTypeBuilder<CouponUsage> builder)
    {
        builder.ToTable("coupon_usage");

        builder.HasKey(cu => cu.Id);

        builder.Property(cu => cu.DiscountAmount)
            .HasPrecision(10, 2);

        builder.Property(cu => cu.UsedAt)
            .IsRequired();

        // Relationships
        builder.HasOne(cu => cu.Coupon)
            .WithMany(c => c.CouponUsages)
            .HasForeignKey(cu => cu.CouponId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(cu => cu.Booking)
            .WithMany(b => b.CouponUsages)
            .HasForeignKey(cu => cu.BookingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(cu => cu.User)
            .WithMany(u => u.CouponUsages)
            .HasForeignKey(cu => cu.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(cu => cu.CouponId);
        builder.HasIndex(cu => cu.BookingId);
        builder.HasIndex(cu => cu.UserId);
        builder.HasIndex(cu => cu.UsedAt);
    }
}
