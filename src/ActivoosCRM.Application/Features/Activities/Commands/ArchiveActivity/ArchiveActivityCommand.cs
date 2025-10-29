using ActivoosCRM.Application.Common.Interfaces;
using ActivoosCRM.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ActivoosCRM.Application.Features.Activities.Commands.ArchiveActivity;

/// <summary>
/// Command to archive an activity (Provider only)
/// </summary>
public record ArchiveActivityCommand(Guid ActivityId) : IRequest<Result<bool>>;

/// <summary>
/// Handler for ArchiveActivityCommand
/// </summary>
public class ArchiveActivityCommandHandler : IRequestHandler<ArchiveActivityCommand, Result<bool>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public ArchiveActivityCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<bool>> Handle(
        ArchiveActivityCommand request,
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
            return Result<bool>.CreateFailure("You don't have permission to archive this activity");
        }

        activity.Archive();
        await _context.SaveChangesAsync(cancellationToken);

        return Result<bool>.CreateSuccess(true);
    }
}
