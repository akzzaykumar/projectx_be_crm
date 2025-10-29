using FluentValidation;

namespace ActivoosCRM.Application.Features.Wishlist.Commands.RemoveFromWishlist;

public class RemoveFromWishlistCommandValidator : AbstractValidator<RemoveFromWishlistCommand>
{
    public RemoveFromWishlistCommandValidator()
    {
        RuleFor(x => x.ActivityId)
            .NotEmpty().WithMessage("Activity ID is required");
    }
}
