using FluentValidation;

namespace ActivoosCRM.Application.Features.Wishlist.Commands.AddToWishlist;

public class AddToWishlistCommandValidator : AbstractValidator<AddToWishlistCommand>
{
    public AddToWishlistCommandValidator()
    {
        RuleFor(x => x.ActivityId)
            .NotEmpty().WithMessage("Activity ID is required");
    }
}
