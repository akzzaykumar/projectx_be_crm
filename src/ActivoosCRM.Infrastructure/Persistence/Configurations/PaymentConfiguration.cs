using ActivoosCRM.Domain.Entities;
using ActivoosCRM.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ActivoosCRM.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity configuration for Payment
/// Responsible for: Payment table schema, booking relationship
/// </summary>
public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payments");

        // Primary key
        builder.HasKey(p => p.Id);

        // Properties
        builder.Property(p => p.PaymentReference)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(p => p.Amount)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(p => p.Currency)
            .IsRequired()
            .HasMaxLength(3);

        builder.Property(p => p.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(p => p.PaymentGateway)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.PaymentGatewayTransactionId)
            .HasMaxLength(200);

        builder.Property(p => p.PaymentGatewayOrderId)
            .HasMaxLength(200);

        builder.Property(p => p.PaymentMethod)
            .HasMaxLength(50);

        builder.Property(p => p.CardLast4Digits)
            .HasMaxLength(4);

        builder.Property(p => p.CardBrand)
            .HasMaxLength(50);

        builder.Property(p => p.FailureReason)
            .HasMaxLength(500);

        builder.Property(p => p.RefundedAmount)
            .HasPrecision(18, 2);

        builder.Property(p => p.RefundTransactionId)
            .HasMaxLength(200);

        builder.Property(p => p.RefundReason)
            .HasMaxLength(500);

        builder.Property(p => p.GatewayResponse)
            .HasMaxLength(4000);

        // Audit fields
        builder.Property(p => p.CreatedAt)
            .IsRequired();

        builder.Property(p => p.UpdatedAt)
            .IsRequired();

        // Relationships
        builder.HasOne(p => p.Booking)
            .WithOne(b => b.Payment)
            .HasForeignKey<Payment>(p => p.BookingId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(p => p.PaymentReference)
            .IsUnique()
            .HasDatabaseName("ix_payments_payment_reference");

        builder.HasIndex(p => p.BookingId)
            .IsUnique()
            .HasDatabaseName("ix_payments_booking_id");

        builder.HasIndex(p => p.Status)
            .HasDatabaseName("ix_payments_status");

        builder.HasIndex(p => p.PaymentGatewayTransactionId)
            .HasDatabaseName("ix_payments_gateway_transaction_id");

        builder.HasIndex(p => p.PaidAt)
            .HasDatabaseName("ix_payments_paid_at");

        builder.HasIndex(p => p.CreatedAt)
            .HasDatabaseName("ix_payments_created_at");

        // Ignore computed properties
        builder.Ignore(p => p.IsFullyRefunded);
        builder.Ignore(p => p.IsPartiallyRefunded);
        builder.Ignore(p => p.RemainingAmount);
        builder.Ignore(p => p.DomainEvents);
    }
}
