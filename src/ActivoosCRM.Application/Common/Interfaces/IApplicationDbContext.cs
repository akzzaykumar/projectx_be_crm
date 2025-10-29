using ActivoosCRM.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ActivoosCRM.Application.Common.Interfaces;

/// <summary>
/// Application database context interface
/// </summary>
public interface IApplicationDbContext
{
    /// <summary>
    /// Users DbSet
    /// </summary>
    DbSet<User> Users { get; }

    /// <summary>
    /// Customer Profiles DbSet
    /// </summary>
    DbSet<CustomerProfile> CustomerProfiles { get; }

    /// <summary>
    /// Activity Providers DbSet
    /// </summary>
    DbSet<ActivityProvider> ActivityProviders { get; }

    /// <summary>
    /// Locations DbSet
    /// </summary>
    DbSet<Location> Locations { get; }

    /// <summary>
    /// Location Requests DbSet
    /// </summary>
    DbSet<LocationRequest> LocationRequests { get; }

    /// <summary>
    /// Categories DbSet
    /// </summary>
    DbSet<Category> Categories { get; }

    /// <summary>
    /// Activities DbSet
    /// </summary>
    DbSet<Activity> Activities { get; }

    /// <summary>
    /// Bookings DbSet
    /// </summary>
    DbSet<Booking> Bookings { get; }

    /// <summary>
    /// Booking Participants DbSet
    /// </summary>
    DbSet<BookingParticipant> BookingParticipants { get; }

    /// <summary>
    /// Payments DbSet
    /// </summary>
    DbSet<Payment> Payments { get; }

    /// <summary>
    /// Reviews DbSet
    /// </summary>
    DbSet<Review> Reviews { get; }

    /// <summary>
    /// Notifications DbSet
    /// </summary>
    DbSet<Notification> Notifications { get; }

    /// <summary>
    /// Coupons DbSet
    /// </summary>
    DbSet<Coupon> Coupons { get; }

    /// <summary>
    /// Coupon Usages DbSet
    /// </summary>
    DbSet<CouponUsage> CouponUsages { get; }

    /// <summary>
    /// Wishlists DbSet
    /// </summary>
    DbSet<Wishlist> Wishlists { get; }

    /// <summary>
    /// Save changes to the database
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of entities affected</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
