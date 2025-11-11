using ActivoosCRM.Application.Common.Interfaces;
using ActivoosCRM.Application.Common.Models;
using ActivoosCRM.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ActivoosCRM.Application.Features.Bookings.Queries.GetBookingById;

public class GetBookingByIdQueryHandler : IRequestHandler<GetBookingByIdQuery, Result<BookingDetailDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetBookingByIdQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<BookingDetailDto>> Handle(GetBookingByIdQuery request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.GetCurrentUserId();
        if (!currentUserId.HasValue)
        {
            return Result<BookingDetailDto>.CreateFailure("User not authenticated");
        }

        // Get booking with all related data
        var booking = await _context.Bookings
            .Include(b => b.Activity)
                .ThenInclude(a => a.Location)
            .Include(b => b.Activity)
                .ThenInclude(a => a.Provider)
            .Include(b => b.Customer)
                .ThenInclude(c => c.User)
            .Include(b => b.Payment)
            .Include(b => b.Participants)
            .FirstOrDefaultAsync(b => b.Id == request.BookingId, cancellationToken);

        if (booking == null)
        {
            return Result<BookingDetailDto>.CreateFailure("Booking not found");
        }

        // Authorization check: customer can view own bookings, provider can view bookings for their activities
        var isCustomer = booking.CustomerId == currentUserId;

        var providerProfile = await _context.ActivityProviders
            .FirstOrDefaultAsync(p => p.UserId == currentUserId, cancellationToken);
        var isProvider = providerProfile != null && booking.Activity.ProviderId == providerProfile.Id;

        if (!isCustomer && !isProvider)
        {
            return Result<BookingDetailDto>.CreateFailure("You do not have permission to view this booking");
        }

        // Map to DTO
        var dto = new BookingDetailDto
        {
            Id = booking.Id,
            BookingReference = booking.BookingReference,
            BookingDate = booking.BookingDate,
            BookingTime = booking.BookingTime,
            NumberOfParticipants = booking.NumberOfParticipants,
            Status = booking.Status.ToString(),

            // Pricing
            PricePerParticipant = booking.PricePerParticipant,
            SubtotalAmount = booking.SubtotalAmount,
            DiscountAmount = booking.DiscountAmount,
            TaxAmount = booking.TaxAmount,
            CommissionAmount = booking.CommissionAmount,
            ProviderPayoutAmount = booking.ProviderPayoutAmount,
            TotalAmount = booking.TotalAmount,
            Currency = booking.Currency,

            // Optional information
            CouponCode = booking.CouponCode,
            CouponDiscountPercentage = booking.CouponDiscountPercentage,
            SpecialRequests = booking.SpecialRequests,
            ParticipantNames = booking.ParticipantNames,
            CustomerNotes = booking.CustomerNotes,
            CancellationReason = booking.CancellationReason,
            RefundAmount = booking.RefundAmount,

            // Activity details
            Activity = new ActivityInfo
            {
                Id = booking.Activity.Id,
                Title = booking.Activity.Title,
                Slug = booking.Activity.Slug,
                Description = booking.Activity.Description,
                CoverImageUrl = booking.Activity.CoverImageUrl,
                DurationMinutes = booking.Activity.DurationMinutes,
                MeetingPoint = booking.Activity.MeetingPoint,
                WhatToBring = booking.Activity.WhatToBring,

                Location = new LocationInfo
                {
                    Id = booking.Activity.Location.Id,
                    Name = booking.Activity.Location.Name,
                    City = booking.Activity.Location.City,
                    State = booking.Activity.Location.State,
                    Country = booking.Activity.Location.Country,
                    Latitude = booking.Activity.Location.Latitude,
                    Longitude = booking.Activity.Location.Longitude
                },

                Provider = new ProviderInfo
                {
                    Id = booking.Activity.Provider.Id,
                    BusinessName = booking.Activity.Provider.BusinessName,
                    LogoUrl = booking.Activity.Provider.LogoUrl
                }
            },

            // Customer details
            Customer = new CustomerInfo
            {
                Id = booking.Customer.Id,
                FullName = booking.Customer.User.FullName,
                Email = booking.Customer.User.Email
            },

            // Payment details
            Payment = booking.Payment != null ? new PaymentInfo
            {
                Id = booking.Payment.Id,
                PaymentReference = booking.Payment.PaymentReference,
                Amount = booking.Payment.Amount,
                Currency = booking.Payment.Currency,
                Status = booking.Payment.Status.ToString(),
                PaymentGateway = booking.Payment.PaymentGateway,
                PaymentMethod = booking.Payment.PaymentMethod,
                CardLast4Digits = booking.Payment.CardLast4Digits,
                CardBrand = booking.Payment.CardBrand,
                RefundedAmount = booking.Payment.RefundedAmount,
                PaidAt = booking.Payment.PaidAt
            } : null,

            // Participants
            Participants = booking.Participants.Select(p => new ParticipantInfo
            {
                Id = p.Id,
                Name = p.Name,
                Age = p.Age ?? 0,
                Gender = p.Gender,
                ContactPhone = p.ContactPhone,
                DietaryRestrictions = p.DietaryRestrictions,
                MedicalConditions = p.MedicalConditions
            }).ToList(),

            // Computed flags
            CanBeCancelled = booking.CanBeCancelled,
            IsPaid = booking.IsPaid,
            IsUpcoming = booking.IsUpcoming,

            // Timestamps
            CreatedAt = booking.CreatedAt,
            ConfirmedAt = booking.ConfirmedAt,
            CompletedAt = booking.CompletedAt,
            CancelledAt = booking.CancelledAt,
            CheckedInAt = booking.CheckedInAt
        };

        return Result<BookingDetailDto>.CreateSuccess(dto);
    }
}
