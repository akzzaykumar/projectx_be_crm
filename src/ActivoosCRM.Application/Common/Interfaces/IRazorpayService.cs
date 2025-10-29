namespace ActivoosCRM.Application.Common.Interfaces;

/// <summary>
/// Interface for Razorpay payment gateway integration
/// </summary>
public interface IRazorpayService
{
    /// <summary>
    /// Create a Razorpay order for payment
    /// </summary>
    Task<RazorpayOrderResponse> CreateOrderAsync(decimal amount, string currency, string bookingReference, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verify payment signature from webhook
    /// </summary>
    bool VerifyWebhookSignature(string webhookBody, string signature, string webhookSecret);

    /// <summary>
    /// Verify payment signature from client
    /// </summary>
    bool VerifyPaymentSignature(string orderId, string paymentId, string signature);

    /// <summary>
    /// Fetch payment details from Razorpay
    /// </summary>
    Task<RazorpayPaymentResponse> FetchPaymentAsync(string paymentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a refund for a payment
    /// </summary>
    Task<RazorpayRefundResponse> CreateRefundAsync(string paymentId, decimal amount, string reason, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetch refund details
    /// </summary>
    Task<RazorpayRefundResponse> FetchRefundAsync(string refundId, CancellationToken cancellationToken = default);
}

public record RazorpayOrderResponse
{
    public string OrderId { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string Currency { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string Receipt { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}

public record RazorpayPaymentResponse
{
    public string PaymentId { get; init; } = string.Empty;
    public string OrderId { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string Currency { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string Method { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string? Contact { get; init; }
    public string? CardId { get; init; }
    public string? Bank { get; init; }
    public string? Wallet { get; init; }
    public string? Vpa { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record RazorpayRefundResponse
{
    public string RefundId { get; init; } = string.Empty;
    public string PaymentId { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string Currency { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string? SpeedRequested { get; init; }
    public string? SpeedProcessed { get; init; }
    public DateTime CreatedAt { get; init; }
}
