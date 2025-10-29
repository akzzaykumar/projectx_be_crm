namespace ActivoosCRM.Application.Common.Interfaces;

/// <summary>
/// Service for tracking the current authenticated user
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// Gets the current user's ID
    /// </summary>
    /// <returns>Current user ID or null if not authenticated</returns>
    Guid? GetCurrentUserId();

    /// <summary>
    /// Gets the current user's email
    /// </summary>
    /// <returns>Current user email or null if not authenticated</returns>
    string? GetCurrentUserEmail();

    /// <summary>
    /// Checks if a user is currently authenticated
    /// </summary>
    /// <returns>True if user is authenticated</returns>
    bool IsAuthenticated();
}