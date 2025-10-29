using ActivoosCRM.Domain.Common;
using ActivoosCRM.Domain.Enums;

namespace ActivoosCRM.Domain.Entities;

/// <summary>
/// Payment entity - Represents payment transactions for bookings
/// Responsible for: Payment processing, gateway integration, refund management
/// </summary>
public class Payment : AuditableEntity
{
    private Payment() { } // Private constructor for EF Core

    // Booking relationship
    public Guid BookingId { get; private set; }
    public virtual Booking Booking { get; private set; } = null!;

    // Payment details
    public string PaymentReference { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "INR";
    public PaymentStatus Status { get; private set; } = PaymentStatus.Pending;

    // Payment gateway integration
    public string PaymentGateway { get; private set; } = string.Empty;
    public string? PaymentGatewayTransactionId { get; private set; }
    public string? PaymentGatewayOrderId { get; private set; }
    public string? PaymentMethod { get; private set; } // Card, UPI, NetBanking, etc.

    // Card details (last 4 digits only for security)
    public string? CardLast4Digits { get; private set; }
    public string? CardBrand { get; private set; } // Visa, Mastercard, etc.

    // Status tracking
    public DateTime? PaidAt { get; private set; }
    public DateTime? FailedAt { get; private set; }
    public string? FailureReason { get; private set; }
    public int RetryAttempts { get; private set; } = 0;

    // Refund tracking
    public decimal RefundedAmount { get; private set; } = 0;
    public DateTime? RefundedAt { get; private set; }
    public string? RefundTransactionId { get; private set; }
    public string? RefundReason { get; private set; }

    // Gateway response (stored as JSON)
    public string? GatewayResponse { get; private set; }

    // Computed properties
    public bool IsFullyRefunded => RefundedAmount >= Amount;
    public bool IsPartiallyRefunded => RefundedAmount > 0 && RefundedAmount < Amount;
    public decimal RemainingAmount => Amount - RefundedAmount;

    /// <summary>
    /// Factory method to create a new payment
    /// </summary>
    public static Payment Create(
        Guid bookingId,
        decimal amount,
        string paymentGateway,
        string currency = "INR")
    {
        if (bookingId == Guid.Empty)
            throw new ArgumentException("Booking ID is required", nameof(bookingId));

        if (amount <= 0)
            throw new ArgumentException("Amount must be greater than 0", nameof(amount));

        if (string.IsNullOrWhiteSpace(paymentGateway))
            throw new ArgumentException("Payment gateway is required", nameof(paymentGateway));

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            BookingId = bookingId,
            PaymentReference = GeneratePaymentReference(),
            Amount = amount,
            Currency = currency.ToUpperInvariant(),
            PaymentGateway = paymentGateway.Trim(),
            Status = PaymentStatus.Pending,
            RetryAttempts = 0,
            RefundedAmount = 0
        };

        return payment;
    }

    /// <summary>
    /// Generate unique payment reference
    /// </summary>
    private static string GeneratePaymentReference()
    {
        return $"PAY{DateTime.UtcNow:yyyyMMdd}{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
    }

    /// <summary>
    /// Set payment gateway order ID
    /// </summary>
    public void SetGatewayOrderId(string orderId)
    {
        if (string.IsNullOrWhiteSpace(orderId))
            throw new ArgumentException("Gateway order ID is required", nameof(orderId));

        PaymentGatewayOrderId = orderId.Trim();
    }

    /// <summary>
    /// Mark payment as completed
    /// </summary>
    public void MarkAsCompleted(
        string transactionId,
        string? paymentMethod = null,
        string? cardLast4Digits = null,
        string? cardBrand = null,
        string? gatewayResponse = null)
    {
        if (Status == PaymentStatus.Completed)
            throw new InvalidOperationException("Payment is already completed");

        if (string.IsNullOrWhiteSpace(transactionId))
            throw new ArgumentException("Transaction ID is required", nameof(transactionId));

        Status = PaymentStatus.Completed;
        PaidAt = DateTime.UtcNow;
        PaymentGatewayTransactionId = transactionId.Trim();
        PaymentMethod = paymentMethod?.Trim();
        CardLast4Digits = cardLast4Digits?.Trim();
        CardBrand = cardBrand?.Trim();
        GatewayResponse = gatewayResponse?.Trim();
        FailedAt = null;
        FailureReason = null;
    }

    /// <summary>
    /// Mark payment as failed
    /// </summary>
    public void MarkAsFailed(string reason, string? gatewayResponse = null)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Failure reason is required", nameof(reason));

        Status = PaymentStatus.Failed;
        FailedAt = DateTime.UtcNow;
        FailureReason = reason.Trim();
        GatewayResponse = gatewayResponse?.Trim();
        RetryAttempts++;
    }

    /// <summary>
    /// Retry payment
    /// </summary>
    public void Retry()
    {
        if (Status != PaymentStatus.Failed)
            throw new InvalidOperationException("Only failed payments can be retried");

        Status = PaymentStatus.Pending;
        FailedAt = null;
        FailureReason = null;
    }

    /// <summary>
    /// Process full refund
    /// </summary>
    public void ProcessFullRefund(string refundTransactionId, string reason)
    {
        ProcessRefund(Amount, refundTransactionId, reason);
    }

    /// <summary>
    /// Process partial refund
    /// </summary>
    public void ProcessPartialRefund(decimal refundAmount, string refundTransactionId, string reason)
    {
        if (refundAmount <= 0)
            throw new ArgumentException("Refund amount must be greater than 0", nameof(refundAmount));

        if (refundAmount > RemainingAmount)
            throw new ArgumentException($"Refund amount cannot exceed remaining amount of {RemainingAmount}", nameof(refundAmount));

        ProcessRefund(refundAmount, refundTransactionId, reason);
    }

    /// <summary>
    /// Process refund (internal)
    /// </summary>
    private void ProcessRefund(decimal refundAmount, string refundTransactionId, string reason)
    {
        if (Status != PaymentStatus.Completed)
            throw new InvalidOperationException("Only completed payments can be refunded");

        if (string.IsNullOrWhiteSpace(refundTransactionId))
            throw new ArgumentException("Refund transaction ID is required", nameof(refundTransactionId));

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Refund reason is required", nameof(reason));

        RefundedAmount += refundAmount;
        RefundedAt = DateTime.UtcNow;
        RefundTransactionId = refundTransactionId.Trim();
        RefundReason = reason.Trim();

        Status = IsFullyRefunded ? PaymentStatus.Refunded : PaymentStatus.PartiallyRefunded;
    }

    /// <summary>
    /// Check if payment can be retried
    /// </summary>
    public bool CanBeRetried()
    {
        return Status == PaymentStatus.Failed && RetryAttempts < 3;
    }

    /// <summary>
    /// Check if payment can be refunded
    /// </summary>
    public bool CanBeRefunded()
    {
        return Status == PaymentStatus.Completed && RemainingAmount > 0;
    }

    /// <summary>
    /// Get payment age in hours
    /// </summary>
    public double GetPaymentAgeHours()
    {
        if (!PaidAt.HasValue)
            return 0;

        return (DateTime.UtcNow - PaidAt.Value).TotalHours;
    }

    /// <summary>
    /// Check if payment is within refund window (e.g., 48 hours)
    /// </summary>
    public bool IsWithinRefundWindow(int windowHours = 48)
    {
        return GetPaymentAgeHours() <= windowHours;
    }
}
