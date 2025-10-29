using ActivoosCRM.Application.Common.Models;
using MediatR;

namespace ActivoosCRM.Application.Features.Wishlist.Commands.AddToWishlist;

/// <summary>
/// Command to add activity to user's wishlist
/// </summary>
public record AddToWishlistCommand(Guid ActivityId) : IRequest<Result<bool>>;
