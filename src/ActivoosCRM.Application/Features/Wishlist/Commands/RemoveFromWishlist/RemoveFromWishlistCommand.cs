using ActivoosCRM.Application.Common.Models;
using MediatR;

namespace ActivoosCRM.Application.Features.Wishlist.Commands.RemoveFromWishlist;

/// <summary>
/// Command to remove activity from user's wishlist
/// </summary>
public record RemoveFromWishlistCommand(Guid ActivityId) : IRequest<Result<bool>>;
