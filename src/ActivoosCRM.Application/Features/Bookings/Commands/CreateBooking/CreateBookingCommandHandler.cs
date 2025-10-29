using ActivoosCRM.Application.Common.Interfaces;
using ActivoosCRM.Application.Common.Models;
using ActivoosCRM.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ActivoosCRM.Application.Features.Bookings.Commands.CreateBooking;

/// <summary>
/// Handler for CreateBookingCommand
/// </summary>
public class CreateBookingCommandHandler : IRequestHandler<CreateBookingCommand, Result<CreateBookingResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public CreateBookingCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
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

        // TODO: Apply coupon if provided
        // This would require a Coupon service to validate and calculate discount
        if (!string.IsNullOrWhiteSpace(request.CouponCode))
        {
            // Placeholder for coupon validation
            // var couponResult = await _couponService.ValidateAndApply(request.CouponCode, subtotal);
            // if (couponResult.IsValid)
            // {
            //     booking.ApplyDiscount(couponResult.DiscountAmount, request.CouponCode, couponResult.DiscountPercentage);
            // }
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
