using ActivoosCRM.Application.Common.Interfaces;
using ActivoosCRM.Application.Common.Models;
using FluentValidation;
using MediatR;

namespace ActivoosCRM.Application.Features.GiftCards.Commands.CreateGiftCard;

/// <summary>
/// Command to create and purchase a gift card
/// </summary>
public class CreateGiftCardCommand : IRequest<Result<CreateGiftCardResponse>>
{
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "INR";
    public string? RecipientEmail { get; set; }
    public string? RecipientName { get; set; }
    public string? Message { get; set; }
}

/// <summary>
/// Response for create gift card command
/// </summary>
public class CreateGiftCardResponse
{
    public Guid GiftCardId { get; set; }
    public string Code { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "INR";
    public DateTime ExpiresAt { get; set; }
    public string? RecipientEmail { get; set; }
    public bool EmailSent { get; set; }
}

/// <summary>
/// Validator for CreateGiftCardCommand
/// </summary>
public class CreateGiftCardCommandValidator : AbstractValidator<CreateGiftCardCommand>
{
    public CreateGiftCardCommandValidator()
    {
        RuleFor(x => x.Amount)
            .GreaterThanOrEqualTo(500)
            .WithMessage("Minimum gift card amount is ₹500")
            .LessThanOrEqualTo(50000)
            .WithMessage("Maximum gift card amount is ₹50,000");

        RuleFor(x => x.Currency)
            .NotEmpty()
            .WithMessage("Currency is required")
            .Must(c => c == "INR" || c == "USD")
            .WithMessage("Only INR and USD currencies are supported");

        RuleFor(x => x.RecipientEmail)
            .EmailAddress()
            .When(x => !string.IsNullOrEmpty(x.RecipientEmail))
            .WithMessage("Invalid email address format");

        RuleFor(x => x.RecipientName)
            .MaximumLength(100)
            .When(x => !string.IsNullOrEmpty(x.RecipientName))
            .WithMessage("Recipient name cannot exceed 100 characters");

        RuleFor(x => x.Message)
            .MaximumLength(500)
            .When(x => !string.IsNullOrEmpty(x.Message))
            .WithMessage("Message cannot exceed 500 characters");
    }
}