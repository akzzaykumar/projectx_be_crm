using ActivoosCRM.Application.Common.Interfaces;
using ActivoosCRM.Application.Common.Models;
using ActivoosCRM.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ActivoosCRM.Application.Features.Bookings.Queries.GetBookings;

/// <summary>
/// Handler for GetBookingsQuery
/// </summary>
public class GetBookingsQueryHandler : IRequestHandler<GetBookingsQuery, Result<PaginatedList<BookingDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetBookingsQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<PaginatedList<BookingDto>>> Handle(
        GetBookingsQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Result<PaginatedList<BookingDto>>.CreateFailure("User not authenticated");
        }

        // Get customer profile
        var customerProfile = await _context.CustomerProfiles
            .FirstOrDefaultAsync(cp => cp.UserId == userId.Value, cancellationToken);

        if (customerProfile == null)
        {
            return Result<PaginatedList<BookingDto>>.CreateFailure("Customer profile not found");
        }

        // Validate page size
        var pageSize = Math.Min(request.PageSize, 50);

        // Build query
        var query = _context.Bookings
            .Include(b => b.Activity)
                .ThenInclude(a => a.Location)
            .Include(b => b.Activity)
                .ThenInclude(a => a.Provider)
            .Include(b => b.Payment)
            .Where(b => b.CustomerId == customerProfile.Id)
            .AsQueryable();

        // Apply status filter
        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            if (Enum.TryParse<BookingStatus>(request.Status, true, out var status))
            {
                query = query.Where(b => b.Status == status);
            }
        }

        // Apply date range filters
        if (request.FromDate.HasValue)
        {
            query = query.Where(b => b.BookingDate >= request.FromDate.Value.Date);
        }

        if (request.ToDate.HasValue)
        {
            query = query.Where(b => b.BookingDate <= request.ToDate.Value.Date);
        }

        // Apply activity filter
        if (request.ActivityId.HasValue)
        {
            query = query.Where(b => b.ActivityId == request.ActivityId.Value);
        }

        // Order by booking date descending (most recent first)
        query = query.OrderByDescending(b => b.BookingDate).ThenByDescending(b => b.CreatedAt);

        // Get total count
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply pagination
        var bookings = await query
            .Skip((request.Page - 1) * pageSize)
            .Take(pageSize)
            .Select(b => new BookingDto
            {
                BookingId = b.Id,
                BookingReference = b.BookingReference,
                BookingDate = b.BookingDate,
                BookingTime = b.BookingTime,
                NumberOfParticipants = b.NumberOfParticipants,
                Status = b.Status.ToString(),
                TotalAmount = b.TotalAmount,
                Currency = b.Currency,
                SpecialRequests = b.SpecialRequests,
                Activity = new ActivitySummary
                {
                    ActivityId = b.Activity.Id,
                    Title = b.Activity.Title,
                    CoverImageUrl = b.Activity.CoverImageUrl,
                    DurationMinutes = b.Activity.DurationMinutes,
                    Location = new LocationSummary
                    {
                        Name = b.Activity.Location.Name,
                        City = b.Activity.Location.City
                    },
                    Provider = new ProviderSummary
                    {
                        BusinessName = b.Activity.Provider.BusinessName,
                        BusinessPhone = b.Activity.Provider.BusinessPhone
                    }
                },
                Payment = b.Payment == null ? null : new PaymentSummary
                {
                    PaymentId = b.Payment.Id,
                    Status = b.Payment.Status.ToString(),
                    PaidAt = b.Payment.PaidAt
                },
                CanBeCancelled = b.CanBeCancelled,
                IsPaid = b.IsPaid,
                ConfirmedAt = b.ConfirmedAt,
                CreatedAt = b.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var paginatedList = new PaginatedList<BookingDto>(
            bookings,
            totalCount,
            request.Page,
            pageSize);

        return Result<PaginatedList<BookingDto>>.CreateSuccess(paginatedList);
    }
}
