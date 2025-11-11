using ActivoosCRM.Application.Common.Interfaces;
using ActivoosCRM.Application.Common.Models;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ActivoosCRM.Application.Features.Loyalty.Queries.GetLoyaltyHistory;

/// <summary>
/// Query to get user's loyalty transaction history
/// </summary>
public class GetLoyaltyHistoryQuery : IRequest<Result<List<LoyaltyHistoryItemDto>>>
{
    public int PageSize { get; set; } = 50;
}

/// <summary>
/// Loyalty transaction history item DTO
/// </summary>
public class LoyaltyHistoryItemDto
{
    public Guid Id { get; set; }
    public int Points { get; set; }
    public string TransactionType { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public bool IsExpired { get; set; }
    public bool IsEarned => Points > 0;
    public bool IsRedeemed => Points < 0;
    public int DaysUntilExpiry { get; set; }
}

/// <summary>
/// Validator for GetLoyaltyHistoryQuery
/// </summary>
public class GetLoyaltyHistoryQueryValidator : AbstractValidator<GetLoyaltyHistoryQuery>
{
    public GetLoyaltyHistoryQueryValidator()
    {
        RuleFor(x => x.PageSize)
            .GreaterThan(0)
            .WithMessage("Page size must be greater than 0")
            .LessThanOrEqualTo(200)
            .WithMessage("Page size cannot exceed 200");
    }
}

/// <summary>
/// Handler for GetLoyaltyHistoryQuery
/// </summary>
public class GetLoyaltyHistoryQueryHandler : IRequestHandler<GetLoyaltyHistoryQuery, Result<List<LoyaltyHistoryItemDto>>>
{
    private readonly ILoyaltyService _loyaltyService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GetLoyaltyHistoryQueryHandler> _logger;

    public GetLoyaltyHistoryQueryHandler(
        ILoyaltyService loyaltyService,
        ICurrentUserService currentUserService,
        ILogger<GetLoyaltyHistoryQueryHandler> logger)
    {
        _loyaltyService = loyaltyService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<List<LoyaltyHistoryItemDto>>> Handle(
        GetLoyaltyHistoryQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = _currentUserService.GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Result<List<LoyaltyHistoryItemDto>>.CreateFailure("User not authenticated");
            }

            _logger.LogInformation(
                "Getting loyalty history for user {UserId}, pageSize: {PageSize}",
                userId.Value, request.PageSize);

            var history = await _loyaltyService.GetLoyaltyHistoryAsync(
                userId.Value,
                request.PageSize,
                cancellationToken);

            var response = history.Select(t =>
            {
                var daysUntilExpiry = t.ExpiryDate.HasValue
                    ? (int)(t.ExpiryDate.Value - DateTime.UtcNow).TotalDays
                    : int.MaxValue;

                return new LoyaltyHistoryItemDto
                {
                    Id = t.Id,
                    Points = t.Points,
                    TransactionType = t.TransactionType,
                    Description = t.Description,
                    CreatedAt = t.CreatedAt,
                    ExpiryDate = t.ExpiryDate,
                    IsExpired = t.IsExpired,
                    DaysUntilExpiry = Math.Max(0, daysUntilExpiry)
                };
            }).ToList();

            _logger.LogInformation("Retrieved {Count} loyalty transactions", response.Count);

            return Result<List<LoyaltyHistoryItemDto>>.CreateSuccess(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting loyalty history");
            return Result<List<LoyaltyHistoryItemDto>>.CreateFailure(
                "Failed to retrieve loyalty history");
        }
    }
}