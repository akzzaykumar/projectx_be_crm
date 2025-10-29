using ActivoosCRM.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ActivoosCRM.Infrastructure.Persistence.Configurations;

public class CouponConfiguration : IEntityTypeConfiguration<Coupon>
{
    public void Configure(EntityTypeBuilder<Coupon> builder)
    {
        builder.ToTable("coupons");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Code)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(c => c.Description)
            .HasColumnType("text");

        builder.Property(c => c.DiscountType)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(c => c.DiscountValue)
            .HasPrecision(10, 2);

        builder.Property(c => c.MinOrderAmount)
            .HasPrecision(10, 2);

        builder.Property(c => c.MaxDiscountAmount)
            .HasPrecision(10, 2);

        builder.Property(c => c.ValidFrom)
            .IsRequired();

        builder.Property(c => c.ValidUntil)
            .IsRequired();

        builder.Property(c => c.UsedCount)
            .HasDefaultValue(0);

        builder.Property(c => c.IsActive)
            .HasDefaultValue(true);

        // Store applicable categories as JSON
        builder.Property(c => c.ApplicableCategories)
            .HasColumnType("jsonb");

        // Indexes
        builder.HasIndex(c => c.Code).IsUnique();
        builder.HasIndex(c => c.IsActive);
        builder.HasIndex(c => c.ValidFrom);
        builder.HasIndex(c => c.ValidUntil);
    }
}
