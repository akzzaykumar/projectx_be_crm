using ActivoosCRM.Domain.Entities;
using ActivoosCRM.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ActivoosCRM.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity configuration for Booking
/// Responsible for: Booking table schema, customer/activity relationships
/// </summary>
public class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable("bookings");

        // Primary key
        builder.HasKey(b => b.Id);

        // Properties
        builder.Property(b => b.BookingReference)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(b => b.BookingDate)
            .IsRequired();

        builder.Property(b => b.BookingTime)
            .IsRequired();

        builder.Property(b => b.NumberOfParticipants)
            .IsRequired();

        builder.Property(b => b.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(b => b.PricePerParticipant)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(b => b.SubtotalAmount)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(b => b.DiscountAmount)
            .HasPrecision(18, 2);

        builder.Property(b => b.TaxAmount)
            .HasPrecision(18, 2);

        builder.Property(b => b.TotalAmount)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(b => b.Currency)
            .IsRequired()
            .HasMaxLength(3);

        builder.Property(b => b.CouponCode)
            .HasMaxLength(50);

        builder.Property(b => b.CouponDiscountPercentage)
            .HasPrecision(5, 2);

        builder.Property(b => b.SpecialRequests)
            .HasMaxLength(1000);

        builder.Property(b => b.ParticipantNames)
            .HasMaxLength(1000);

        builder.Property(b => b.CustomerNotes)
            .HasMaxLength(1000);

        builder.Property(b => b.ProviderNotes)
            .HasMaxLength(1000);

        builder.Property(b => b.CancellationReason)
            .HasMaxLength(500);

        builder.Property(b => b.RefundAmount)
            .HasPrecision(18, 2);

        // Audit fields
        builder.Property(b => b.CreatedAt)
            .IsRequired();

        builder.Property(b => b.UpdatedAt)
            .IsRequired();

        // Relationships
        builder.HasOne(b => b.Customer)
            .WithMany(c => c.Bookings)
            .HasForeignKey(b => b.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.Activity)
            .WithMany(a => a.Bookings)
            .HasForeignKey(b => b.ActivityId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(b => b.BookingReference)
            .IsUnique()
            .HasDatabaseName("ix_bookings_booking_reference");

        builder.HasIndex(b => b.CustomerId)
            .HasDatabaseName("ix_bookings_customer_id");

        builder.HasIndex(b => b.ActivityId)
            .HasDatabaseName("ix_bookings_activity_id");

        builder.HasIndex(b => b.BookingDate)
            .HasDatabaseName("ix_bookings_booking_date");

        builder.HasIndex(b => b.Status)
            .HasDatabaseName("ix_bookings_status");

        builder.HasIndex(b => b.CreatedAt)
            .HasDatabaseName("ix_bookings_created_at");

        builder.HasIndex(b => new { b.CustomerId, b.Status })
            .HasDatabaseName("ix_bookings_customer_status");

        builder.HasIndex(b => new { b.ActivityId, b.BookingDate })
            .HasDatabaseName("ix_bookings_activity_date");

        // Ignore computed properties
        builder.Ignore(b => b.IsPaid);
        builder.Ignore(b => b.CanBeCancelled);
        builder.Ignore(b => b.IsUpcoming);
        builder.Ignore(b => b.DomainEvents);
    }
}
