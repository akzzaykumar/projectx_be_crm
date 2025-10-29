using ActivoosCRM.Application.Common.Models;
using MediatR;

namespace ActivoosCRM.Application.Features.Activities.Commands.CreateActivity;

/// <summary>
/// Command to create a new activity (Provider only)
/// </summary>
public record CreateActivityCommand : IRequest<Result<Guid>>
{
    public Guid CategoryId { get; init; }
    public Guid LocationId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string? ShortDescription { get; init; }
    public decimal Price { get; init; }
    public string Currency { get; init; } = "INR";
    public int MaxParticipants { get; init; }
    public int DurationMinutes { get; init; }
    public int? MinAge { get; init; }
    public int? MaxAge { get; init; }
    public string? DifficultyLevel { get; init; }
    public string? WhatToBring { get; init; }
    public string? MeetingPoint { get; init; }
    public string? CancellationPolicy { get; init; }
}
