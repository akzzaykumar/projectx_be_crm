using System.Security.Claims;
using ActivoosCRM.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace ActivoosCRM.Infrastructure.Services;

/// <summary>
/// Service for tracking the current authenticated user from HTTP context
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Gets the current user's ID from JWT claims
    /// </summary>
    /// <returns>Current user ID or null if not authenticated</returns>
    public Guid? GetCurrentUserId()
    {
        var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim))
            return null;

        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    /// <summary>
    /// Gets the current user's email from JWT claims
    /// </summary>
    /// <returns>Current user email or null if not authenticated</returns>
    public string? GetCurrentUserEmail()
    {
        return _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Email)?.Value;
    }

    /// <summary>
    /// Checks if a user is currently authenticated
    /// </summary>
    /// <returns>True if user is authenticated</returns>
    public bool IsAuthenticated()
    {
        return _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
    }
}