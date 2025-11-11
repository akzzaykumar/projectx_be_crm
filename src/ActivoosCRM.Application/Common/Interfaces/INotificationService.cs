namespace ActivoosCRM.Application.Common.Interfaces;

/// <summary>
/// Service interface for sending notifications to users
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Send booking confirmation notification
    /// </summary>
    Task SendBookingConfirmationAsync(Guid bookingId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send booking cancellation notification
    /// </summary>
    Task SendBookingCancellationAsync(Guid bookingId, Guid userId, string reason, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send payment success notification
    /// </summary>
    Task SendPaymentSuccessAsync(Guid bookingId, Guid userId, decimal amount, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send payment failure notification
    /// </summary>
    Task SendPaymentFailureAsync(Guid bookingId, Guid userId, string reason, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send refund processed notification
    /// </summary>
    Task SendRefundProcessedAsync(Guid bookingId, Guid userId, decimal refundAmount, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send booking reminder notification (24 hours before)
    /// </summary>
    Task SendBookingReminderAsync(Guid bookingId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a notification record in database
    /// </summary>
    Task CreateNotificationAsync(
        Guid userId,
        string title,
        string message,
        string type,
        string? actionUrl = null,
        Dictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default);
}