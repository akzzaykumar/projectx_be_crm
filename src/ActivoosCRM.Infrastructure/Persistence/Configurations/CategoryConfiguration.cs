using ActivoosCRM.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ActivoosCRM.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity configuration for Category
/// Responsible for: Category table schema, self-referencing relationship
/// </summary>
public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("categories");

        // Primary key
        builder.HasKey(c => c.Id);

        // Properties
        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.Slug)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(c => c.Description)
            .HasMaxLength(500);

        builder.Property(c => c.IconUrl)
            .HasMaxLength(500);

        builder.Property(c => c.DisplayOrder)
            .IsRequired();

        builder.Property(c => c.IsActive)
            .IsRequired();

        // Audit fields
        builder.Property(c => c.CreatedAt)
            .IsRequired();

        builder.Property(c => c.UpdatedAt)
            .IsRequired();

        // Self-referencing relationship
        builder.HasOne(c => c.ParentCategory)
            .WithMany(c => c.SubCategories)
            .HasForeignKey(c => c.ParentCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(c => c.Slug)
            .IsUnique()
            .HasDatabaseName("ix_categories_slug");

        builder.HasIndex(c => c.Name)
            .HasDatabaseName("ix_categories_name");

        builder.HasIndex(c => c.IsActive)
            .HasDatabaseName("ix_categories_is_active");

        builder.HasIndex(c => c.DisplayOrder)
            .HasDatabaseName("ix_categories_display_order");

        builder.HasIndex(c => c.ParentCategoryId)
            .HasDatabaseName("ix_categories_parent_category_id");

        // Ignore computed properties
        builder.Ignore(c => c.DomainEvents);
    }
}
