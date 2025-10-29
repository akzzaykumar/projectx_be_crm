using ActivoosCRM.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ActivoosCRM.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity configuration for Location
/// Responsible for: Location table schema, geographic data constraints
/// </summary>
public class LocationConfiguration : IEntityTypeConfiguration<Location>
{
    public void Configure(EntityTypeBuilder<Location> builder)
    {
        builder.ToTable("locations");

        // Primary key
        builder.HasKey(l => l.Id);

        // Properties
        builder.Property(l => l.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(l => l.City)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(l => l.State)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(l => l.Country)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(l => l.PostalCode)
            .HasMaxLength(20);

        builder.Property(l => l.AddressLine1)
            .HasMaxLength(255);

        builder.Property(l => l.AddressLine2)
            .HasMaxLength(255);

        builder.Property(l => l.Latitude)
            .HasPrecision(10, 7);

        builder.Property(l => l.Longitude)
            .HasPrecision(10, 7);

        builder.Property(l => l.Description)
            .HasMaxLength(1000);

        builder.Property(l => l.IsActive)
            .IsRequired();

        // Audit fields
        builder.Property(l => l.CreatedAt)
            .IsRequired();

        builder.Property(l => l.UpdatedAt)
            .IsRequired();

        // Indexes
        builder.HasIndex(l => l.City)
            .HasDatabaseName("ix_locations_city");

        builder.HasIndex(l => l.State)
            .HasDatabaseName("ix_locations_state");

        builder.HasIndex(l => l.Country)
            .HasDatabaseName("ix_locations_country");

        builder.HasIndex(l => l.IsActive)
            .HasDatabaseName("ix_locations_is_active");

        builder.HasIndex(l => new { l.Latitude, l.Longitude })
            .HasDatabaseName("ix_locations_coordinates");

        // Ignore computed properties
        builder.Ignore(l => l.FullAddress);
        builder.Ignore(l => l.DomainEvents);
    }
}
