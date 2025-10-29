using ActivoosCRM.Domain.Common;

namespace ActivoosCRM.Domain.Entities;

/// <summary>
/// Notification entity - Represents system notifications for users
/// Responsible for: User notifications, read status, notification types
/// </summary>
public class Notification : BaseEntity
{
    private Notification() { } // Private constructor for EF Core

    // Relationships
    public Guid UserId { get; private set; }
    public virtual User User { get; private set; } = null!;

    public Guid? RelatedBookingId { get; private set; }
    public virtual Booking? RelatedBooking { get; private set; }

    // Notification content
    public string Title { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public string Type { get; private set; } = string.Empty; // 'booking', 'payment', 'review', 'promotion'

    // Status
    public bool IsRead { get; private set; } = false;

    /// <summary>
    /// Factory method to create a new notification
    /// </summary>
    public static Notification Create(
        Guid userId,
        string title,
        string message,
        string type,
        Guid? relatedBookingId = null)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID is required", nameof(userId));

        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required", nameof(title));

        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Message is required", nameof(message));

        if (string.IsNullOrWhiteSpace(type))
            throw new ArgumentException("Type is required", nameof(type));

        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = title.Trim(),
            Message = message.Trim(),
            Type = type.ToLowerInvariant().Trim(),
            RelatedBookingId = relatedBookingId,
            IsRead = false
        };

        return notification;
    }

    /// <summary>
    /// Mark notification as read
    /// </summary>
    public void MarkAsRead()
    {
        IsRead = true;
    }

    /// <summary>
    /// Mark notification as unread
    /// </summary>
    public void MarkAsUnread()
    {
        IsRead = false;
    }
}
