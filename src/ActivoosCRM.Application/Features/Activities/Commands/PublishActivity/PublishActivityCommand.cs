using ActivoosCRM.Application.Common.Interfaces;
using ActivoosCRM.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ActivoosCRM.Application.Features.Activities.Commands.PublishActivity;

/// <summary>
/// Command to publish an activity (Provider only)
/// </summary>
public record PublishActivityCommand(Guid ActivityId) : IRequest<Result<bool>>;

/// <summary>
/// Handler for PublishActivityCommand
/// </summary>
public class PublishActivityCommandHandler : IRequestHandler<PublishActivityCommand, Result<bool>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public PublishActivityCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<bool>> Handle(
        PublishActivityCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetCurrentUserId();
        if (!userId.HasValue || userId.Value == Guid.Empty)
        {
            return Result<bool>.CreateFailure("User not authenticated");
        }

        var activity = await _context.Activities
            .Include(a => a.Provider)
            .FirstOrDefaultAsync(a => a.Id == request.ActivityId, cancellationToken);

        if (activity == null)
        {
            return Result<bool>.CreateFailure("Activity not found");
        }

        if (activity.Provider.UserId != userId.Value)
        {
            return Result<bool>.CreateFailure("You don't have permission to publish this activity");
        }

        try
        {
            activity.Publish();
            await _context.SaveChangesAsync(cancellationToken);
            return Result<bool>.CreateSuccess(true);
        }
        catch (InvalidOperationException ex)
        {
            return Result<bool>.CreateFailure(ex.Message);
        }
    }
}
