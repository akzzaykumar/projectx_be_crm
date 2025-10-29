using ActivoosCRM.Application.Common.Models;
using MediatR;

namespace ActivoosCRM.Application.Features.Activities.Commands.UpdateActivity;

/// <summary>
/// Command to update an existing activity (Provider only)
/// </summary>
public record UpdateActivityCommand : IRequest<Result<bool>>
{
    public Guid ActivityId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string? ShortDescription { get; init; }
    public string? CoverImageUrl { get; init; }
    public decimal Price { get; init; }
    public string Currency { get; init; } = "INR";
    public int MinParticipants { get; init; } = 1;
    public int MaxParticipants { get; init; }
    public int DurationMinutes { get; init; }
    public int? MinAge { get; init; }
    public int? MaxAge { get; init; }
    public string? DifficultyLevel { get; init; }
    public string? WhatToBring { get; init; }
    public string? MeetingPoint { get; init; }
    public string? CancellationPolicy { get; init; }
    public string? RefundPolicy { get; init; }
    public string? SafetyInstructions { get; init; }
}
