using ActivoosCRM.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ActivoosCRM.Application.Common.Interfaces;

/// <summary>
/// Application database context interface
/// </summary>
public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<Customer> Customers { get; }
    DbSet<Activity> Activities { get; }
    DbSet<Booking> Bookings { get; }
    DbSet<TimeSlot> TimeSlots { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
