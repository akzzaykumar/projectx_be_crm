using ActivoosCRM.Application.Common.Interfaces;
using ActivoosCRM.Application.Common.Models;
using ActivoosCRM.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ActivoosCRM.Application.Features.Bookings.Commands.CreateBooking;

/// <summary>
/// Handler for CreateBookingCommand
/// </summary>
public class CreateBookingCommandHandler : IRequestHandler<CreateBookingCommand, Result<CreateBookingResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAvailabilityService _availabilityService;
    private readonly ICouponService _couponService;
    private readonly ILogger<CreateBookingCommandHandler> _logger;

    public CreateBookingCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IAvailabilityService availabilityService,
        ICouponService couponService,
        ILogger<CreateBookingCommandHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _availabilityService = availabilityService;
        _couponService = couponService;
        _logger = logger;
    }

    public async Task<Result<CreateBookingResponse>> Handle(
        CreateBookingCommand request,
        CancellationToken cancellationToken)
    {
        // Get current user
        var userId = _currentUserService.GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Result<CreateBookingResponse>.CreateFailure("User not authenticated");
        }

        // Get customer profile
        var customerProfile = await _context.CustomerProfiles
            .FirstOrDefaultAsync(cp => cp.UserId == userId.Value, cancellationToken);

        if (customerProfile == null)
        {
            return Result<CreateBookingResponse>.CreateFailure("Customer profile not found. Please complete your profile first.");
        }

        // Get activity with details
        var activity = await _context.Activities
            .FirstOrDefaultAsync(a => a.Id == request.ActivityId && a.IsActive, cancellationToken);

        if (activity == null)
        {
            return Result<CreateBookingResponse>.CreateFailure("Activity not found or is not available");
        }

        // Validate participant count
        if (!activity.IsValidParticipantCount(request.NumberOfParticipants))
        {
            return Result<CreateBookingResponse>.CreateFailure(
                $"Number of participants must be between {activity.MinParticipants} and {activity.MaxParticipants}");
        }

        // Check if activity is bookable
        if (!activity.IsBookable())
        {
            return Result<CreateBookingResponse>.CreateFailure("Activity is not available for booking");
        }

        // FIXED: Check availability against schedules
        var availabilityCheck = await _availabilityService.CheckAvailabilityAsync(
            activity.Id,
            request.BookingDate.Date,
            request.BookingTime,
            request.NumberOfParticipants,
            cancellationToken);

        if (!availabilityCheck.IsAvailable)
        {
            return Result<CreateBookingResponse>.CreateFailure(
                availabilityCheck.Reason ?? "Activity is not available at the selected time");
        }

        // Calculate pricing
        var pricePerParticipant = activity.EffectivePrice;
        var subtotal = pricePerParticipant * request.NumberOfParticipants;

        // Create booking
        var booking = Booking.Create(
            customerId: customerProfile.Id,
            activityId: activity.Id,
            bookingDate: request.BookingDate.Date,
            bookingTime: request.BookingTime,
            numberOfParticipants: request.NumberOfParticipants,
            pricePerParticipant: pricePerParticipant,
            currency: activity.Currency);

        // Add optional information
        if (!string.IsNullOrWhiteSpace(request.SpecialRequests))
        {
            booking.AddSpecialRequests(request.SpecialRequests);
        }

        if (!string.IsNullOrWhiteSpace(request.ParticipantNames))
        {
            booking.AddParticipantNames(request.ParticipantNames);
        }

        if (!string.IsNullOrWhiteSpace(request.CustomerNotes))
        {
            booking.AddCustomerNotes(request.CustomerNotes);
        }

        // Apply coupon if provided
        if (!string.IsNullOrWhiteSpace(request.CouponCode))
        {
            var couponResult = await _couponService.ValidateCouponAsync(
                request.CouponCode,
                activity.Id,
                subtotal,
                userId.Value,
                cancellationToken);

            if (couponResult.IsValid && couponResult.CouponId.HasValue)
            {
                // Apply discount to booking
                booking.ApplyDiscount(
                    couponResult.DiscountAmount,
                    request.CouponCode,
                    couponResult.DiscountPercentage);

                _logger.LogInformation(
                    "Coupon {CouponCode} applied to booking. Discount: {Discount}",
                    request.CouponCode,
                    couponResult.DiscountAmount);
            }
            else
            {
                _logger.LogWarning(
                    "Failed to apply coupon {CouponCode}: {Error}",
                    request.CouponCode,
                    couponResult.ErrorMessage);
                
                // Return error if coupon validation failed
                return Result<CreateBookingResponse>.CreateFailure(
                    couponResult.ErrorMessage ?? "Invalid coupon code");
            }
        }

        // Add booking to context
        _context.Bookings.Add(booking);

        // Add participants if provided
        if (request.Participants != null && request.Participants.Any())
        {
            foreach (var participantDto in request.Participants)
            {
                var participant = BookingParticipant.Create(
                    bookingId: booking.Id,
                    name: participantDto.Name,
                    age: participantDto.Age,
                    gender: participantDto.Gender,
                    contactPhone: participantDto.ContactPhone);

                _context.BookingParticipants.Add(participant);
            }
        }

        // Save changes
        await _context.SaveChangesAsync(cancellationToken);

        // Record coupon usage if coupon was applied
        if (!string.IsNullOrWhiteSpace(booking.CouponCode))
        {
            try
            {
                // Find the coupon by code to get its ID
                var coupon = await _context.Coupons
                    .FirstOrDefaultAsync(c => c.Code == booking.CouponCode.ToUpperInvariant(), cancellationToken);

                if (coupon != null)
                {
                    await _couponService.ApplyCouponToBookingAsync(
                        coupon.Id,
                        booking.Id,
                        userId.Value,
                        booking.DiscountAmount,
                        cancellationToken);

                    _logger.LogInformation("Coupon usage recorded for booking {BookingId}", booking.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to record coupon usage for booking {BookingId}", booking.Id);
                // Don't fail the booking creation if coupon usage recording fails
            }
        }

        // Return response
        var response = new CreateBookingResponse
        {
            BookingId = booking.Id,
            BookingReference = booking.BookingReference,
            TotalAmount = booking.TotalAmount,
            PaymentRequired = booking.TotalAmount > 0
        };

        return Result<CreateBookingResponse>.CreateSuccess(response);
    }
}
