using ActivoosCRM.Application.Common.Interfaces;
using ActivoosCRM.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ActivoosCRM.Infrastructure.Services;

/// <summary>
/// Service for sending notifications to users via email and in-app notifications
/// </summary>
public class NotificationService : INotificationService
{
    private readonly IApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IApplicationDbContext context,
        IEmailService emailService,
        ILogger<NotificationService> logger)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    /// <summary>
    /// Send booking confirmation notification
    /// </summary>
    public async Task SendBookingConfirmationAsync(
        Guid bookingId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var booking = await _context.Bookings
                .Include(b => b.Activity)
                .Include(b => b.Customer)
                    .ThenInclude(c => c.User)
                .FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken);

            if (booking == null)
            {
                _logger.LogWarning("Booking not found for confirmation notification: {BookingId}", bookingId);
                return;
            }

            var user = booking.Customer.User;

            // Create in-app notification
            await CreateNotificationAsync(
                userId,
                "Booking Confirmed",
                $"Your booking for {booking.Activity.Title} on {booking.BookingDate:MMM dd, yyyy} at {booking.BookingTime} has been confirmed. Booking reference: {booking.BookingReference}",
                "booking",
                $"/bookings/{bookingId}",
                new Dictionary<string, string>
                {
                    { "booking_id", bookingId.ToString() },
                    { "activity_name", booking.Activity.Title }
                },
                cancellationToken);

            // Send email notification
            var emailHtml = $@"
<html>
<body>
    <h2>Booking Confirmed!</h2>
    <p>Dear {user.FirstName},</p>
    <p>Your booking has been confirmed.</p>
    
    <h3>Booking Details:</h3>
    <ul>
        <li><strong>Activity:</strong> {booking.Activity.Title}</li>
        <li><strong>Date:</strong> {booking.BookingDate:MMMM dd, yyyy}</li>
        <li><strong>Time:</strong> {booking.BookingTime}</li>
        <li><strong>Participants:</strong> {booking.NumberOfParticipants}</li>
        <li><strong>Total Amount:</strong> {booking.Currency} {booking.TotalAmount:N2}</li>
        <li><strong>Booking Reference:</strong> {booking.BookingReference}</li>
    </ul>
    
    <p>We look forward to seeing you!</p>
    <p>Best regards,<br/>The FunBookr Team</p>
</body>
</html>";

            await _emailService.SendEmailAsync(
                user.Email,
                "Booking Confirmation - FunBookr",
                emailHtml);

            _logger.LogInformation("Booking confirmation notification sent for booking: {BookingId}", bookingId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending booking confirmation notification for booking: {BookingId}", bookingId);
        }
    }

    /// <summary>
    /// Send booking cancellation notification
    /// </summary>
    public async Task SendBookingCancellationAsync(
        Guid bookingId,
        Guid userId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var booking = await _context.Bookings
                .Include(b => b.Activity)
                .Include(b => b.Customer)
                    .ThenInclude(c => c.User)
                .FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken);

            if (booking == null)
            {
                _logger.LogWarning("Booking not found for cancellation notification: {BookingId}", bookingId);
                return;
            }

            var user = booking.Customer.User;

            // Create in-app notification
            await CreateNotificationAsync(
                userId,
                "Booking Cancelled",
                $"Your booking for {booking.Activity.Title} on {booking.BookingDate:MMM dd, yyyy} has been cancelled. {(booking.RefundAmount > 0 ? $"Refund of {booking.Currency} {booking.RefundAmount:N2} will be processed within 5-7 business days." : "")}",
                "booking",
                $"/bookings/{bookingId}",
                new Dictionary<string, string>
                {
                    { "booking_id", bookingId.ToString() },
                    { "activity_name", booking.Activity.Title },
                    { "refund_amount", booking.RefundAmount.ToString() }
                },
                cancellationToken);

            // Send email notification
            var emailHtml = $@"
<html>
<body>
    <h2>Booking Cancelled</h2>
    <p>Dear {user.FirstName},</p>
    <p>Your booking has been cancelled.</p>
    
    <h3>Booking Details:</h3>
    <ul>
        <li><strong>Activity:</strong> {booking.Activity.Title}</li>
        <li><strong>Date:</strong> {booking.BookingDate:MMMM dd, yyyy}</li>
        <li><strong>Time:</strong> {booking.BookingTime}</li>
        <li><strong>Booking Reference:</strong> {booking.BookingReference}</li>
        <li><strong>Cancellation Reason:</strong> {reason}</li>
    </ul>
    
    {(booking.RefundAmount > 0 
        ? $"<p><strong>Refund Information:</strong><br/>A refund of {booking.Currency} {booking.RefundAmount:N2} will be processed to your original payment method within 5-7 business days.</p>" 
        : "")}
    
    <p>We hope to see you again soon!</p>
    <p>Best regards,<br/>The FunBookr Team</p>
</body>
</html>";

            await _emailService.SendEmailAsync(
                user.Email,
                "Booking Cancellation - FunBookr",
                emailHtml);

            _logger.LogInformation("Booking cancellation notification sent for booking: {BookingId}", bookingId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending booking cancellation notification for booking: {BookingId}", bookingId);
        }
    }

    /// <summary>
    /// Send payment success notification
    /// </summary>
    public async Task SendPaymentSuccessAsync(
        Guid bookingId,
        Guid userId,
        decimal amount,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var booking = await _context.Bookings
                .Include(b => b.Activity)
                .Include(b => b.Customer)
                    .ThenInclude(c => c.User)
                .Include(b => b.Payment)
                .FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken);

            if (booking == null)
            {
                _logger.LogWarning("Booking not found for payment success notification: {BookingId}", bookingId);
                return;
            }

            var user = booking.Customer.User;

            // Create in-app notification
            await CreateNotificationAsync(
                userId,
                "Payment Successful",
                $"Your payment of {booking.Currency} {amount:N2} for {booking.Activity.Title} has been processed successfully.",
                "payment",
                $"/bookings/{bookingId}",
                new Dictionary<string, string>
                {
                    { "booking_id", bookingId.ToString() },
                    { "amount", amount.ToString() },
                    { "payment_id", booking.Payment?.Id.ToString() ?? "" }
                },
                cancellationToken);

            // Send email notification
            var emailHtml = $@"
<html>
<body>
    <h2>Payment Successful!</h2>
    <p>Dear {user.FirstName},</p>
    <p>Your payment has been processed successfully.</p>
    
    <h3>Payment Details:</h3>
    <ul>
        <li><strong>Amount:</strong> {booking.Currency} {amount:N2}</li>
        <li><strong>Activity:</strong> {booking.Activity.Title}</li>
        <li><strong>Booking Reference:</strong> {booking.BookingReference}</li>
        {(booking.Payment != null ? $"<li><strong>Payment Reference:</strong> {booking.Payment.PaymentReference}</li>" : "")}
        <li><strong>Date & Time:</strong> {booking.BookingDate:MMMM dd, yyyy} at {booking.BookingTime}</li>
    </ul>
    
    <p>Your booking is now confirmed!</p>
    <p>Best regards,<br/>The FunBookr Team</p>
</body>
</html>";

            await _emailService.SendEmailAsync(
                user.Email,
                "Payment Successful - FunBookr",
                emailHtml);

            _logger.LogInformation("Payment success notification sent for booking: {BookingId}", bookingId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending payment success notification for booking: {BookingId}", bookingId);
        }
    }

    /// <summary>
    /// Send payment failure notification
    /// </summary>
    public async Task SendPaymentFailureAsync(
        Guid bookingId,
        Guid userId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var booking = await _context.Bookings
                .Include(b => b.Activity)
                .Include(b => b.Customer)
                    .ThenInclude(c => c.User)
                .FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken);

            if (booking == null)
            {
                _logger.LogWarning("Booking not found for payment failure notification: {BookingId}", bookingId);
                return;
            }

            var user = booking.Customer.User;

            // Create in-app notification
            await CreateNotificationAsync(
                userId,
                "Payment Failed",
                $"Your payment for {booking.Activity.Title} could not be processed. Please try again or contact support.",
                "payment",
                $"/bookings/{bookingId}/payment",
                new Dictionary<string, string>
                {
                    { "booking_id", bookingId.ToString() },
                    { "reason", reason }
                },
                cancellationToken);

            // Send email notification
            var emailHtml = $@"
<html>
<body>
    <h2>Payment Failed</h2>
    <p>Dear {user.FirstName},</p>
    <p>We were unable to process your payment for the following booking:</p>
    
    <h3>Booking Details:</h3>
    <ul>
        <li><strong>Activity:</strong> {booking.Activity.Title}</li>
        <li><strong>Date & Time:</strong> {booking.BookingDate:MMMM dd, yyyy} at {booking.BookingTime}</li>
        <li><strong>Amount:</strong> {booking.Currency} {booking.TotalAmount:N2}</li>
        <li><strong>Booking Reference:</strong> {booking.BookingReference}</li>
    </ul>
    
    <p><strong>Reason:</strong> {reason}</p>
    
    <p>Please try again or contact our support team if you continue to experience issues.</p>
    <p>Best regards,<br/>The FunBookr Team</p>
</body>
</html>";

            await _emailService.SendEmailAsync(
                user.Email,
                "Payment Failed - FunBookr",
                emailHtml);

            _logger.LogInformation("Payment failure notification sent for booking: {BookingId}", bookingId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending payment failure notification for booking: {BookingId}", bookingId);
        }
    }

    /// <summary>
    /// Send refund processed notification
    /// </summary>
    public async Task SendRefundProcessedAsync(
        Guid bookingId,
        Guid userId,
        decimal refundAmount,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var booking = await _context.Bookings
                .Include(b => b.Activity)
                .Include(b => b.Customer)
                    .ThenInclude(c => c.User)
                .FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken);

            if (booking == null)
            {
                _logger.LogWarning("Booking not found for refund notification: {BookingId}", bookingId);
                return;
            }

            var user = booking.Customer.User;

            // Create in-app notification
            await CreateNotificationAsync(
                userId,
                "Refund Processed",
                $"A refund of {booking.Currency} {refundAmount:N2} has been initiated for your cancelled booking of {booking.Activity.Title}.",
                "payment",
                $"/bookings/{bookingId}",
                new Dictionary<string, string>
                {
                    { "booking_id", bookingId.ToString() },
                    { "refund_amount", refundAmount.ToString() }
                },
                cancellationToken);

            // Send email notification
            var emailHtml = $@"
<html>
<body>
    <h2>Refund Processed</h2>
    <p>Dear {user.FirstName},</p>
    <p>A refund has been initiated for your cancelled booking.</p>
    
    <h3>Refund Details:</h3>
    <ul>
        <li><strong>Refund Amount:</strong> {booking.Currency} {refundAmount:N2}</li>
        <li><strong>Activity:</strong> {booking.Activity.Title}</li>
        <li><strong>Booking Reference:</strong> {booking.BookingReference}</li>
    </ul>
    
    <p>The refund will be credited to your original payment method within 5-7 business days.</p>
    <p>Best regards,<br/>The FunBookr Team</p>
</body>
</html>";

            await _emailService.SendEmailAsync(
                user.Email,
                "Refund Processed - FunBookr",
                emailHtml);

            _logger.LogInformation("Refund notification sent for booking: {BookingId}", bookingId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending refund notification for booking: {BookingId}", bookingId);
        }
    }

    /// <summary>
    /// Send booking reminder notification (24 hours before)
    /// </summary>
    public async Task SendBookingReminderAsync(
        Guid bookingId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var booking = await _context.Bookings
                .Include(b => b.Activity)
                    .ThenInclude(a => a.Location)
                .Include(b => b.Customer)
                    .ThenInclude(c => c.User)
                .FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken);

            if (booking == null)
            {
                _logger.LogWarning("Booking not found for reminder notification: {BookingId}", bookingId);
                return;
            }

            var user = booking.Customer.User;

            // Create in-app notification
            await CreateNotificationAsync(
                userId,
                "Booking Reminder",
                $"Reminder: Your booking for {booking.Activity.Title} is tomorrow at {booking.BookingTime}. Get ready for an amazing experience!",
                "booking",
                $"/bookings/{bookingId}",
                new Dictionary<string, string>
                {
                    { "booking_id", bookingId.ToString() },
                    { "activity_name", booking.Activity.Title }
                },
                cancellationToken);

            // Send email notification
            var emailHtml = $@"
<html>
<body>
    <h2>Booking Reminder</h2>
    <p>Dear {user.FirstName},</p>
    <p>This is a friendly reminder about your upcoming booking!</p>
    
    <h3>Booking Details:</h3>
    <ul>
        <li><strong>Activity:</strong> {booking.Activity.Title}</li>
        <li><strong>Date:</strong> {booking.BookingDate:MMMM dd, yyyy}</li>
        <li><strong>Time:</strong> {booking.BookingTime}</li>
        <li><strong>Participants:</strong> {booking.NumberOfParticipants}</li>
        <li><strong>Location:</strong> {booking.Activity.Location?.Name ?? "TBD"}</li>
        <li><strong>Booking Reference:</strong> {booking.BookingReference}</li>
    </ul>
    
    {(!string.IsNullOrEmpty(booking.SpecialRequests) 
        ? $"<p><strong>Your Special Requests:</strong><br/>{booking.SpecialRequests}</p>" 
        : "")}
    
    <p>We look forward to seeing you tomorrow!</p>
    <p>Best regards,<br/>The FunBookr Team</p>
</body>
</html>";

            await _emailService.SendEmailAsync(
                user.Email,
                "Booking Reminder - FunBookr",
                emailHtml);

            _logger.LogInformation("Booking reminder sent for booking: {BookingId}", bookingId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending booking reminder for booking: {BookingId}", bookingId);
        }
    }

    /// <summary>
    /// Create a notification record in database
    /// </summary>
    public async Task CreateNotificationAsync(
        Guid userId,
        string title,
        string message,
        string type,
        string? actionUrl = null,
        Dictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var notification = Notification.Create(
                userId,
                title,
                message,
                type);

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Notification created for user {UserId}: {Title}",
                userId,
                title);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error creating notification for user {UserId}",
                userId);
            throw;
        }
    }
}