using ActivoosCRM.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ActivoosCRM.Infrastructure.Persistence.Configurations;

public class WishlistConfiguration : IEntityTypeConfiguration<Wishlist>
{
    public void Configure(EntityTypeBuilder<Wishlist> builder)
    {
        builder.ToTable("wishlists");

        builder.HasKey(w => w.Id);

        // Relationships
        builder.HasOne(w => w.Customer)
            .WithMany(u => u.Wishlists)
            .HasForeignKey(w => w.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(w => w.Activity)
            .WithMany(a => a.Wishlists)
            .HasForeignKey(w => w.ActivityId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique constraint
        builder.HasIndex(w => new { w.CustomerId, w.ActivityId }).IsUnique();

        // Additional indexes
        builder.HasIndex(w => w.CreatedAt);
    }
}
