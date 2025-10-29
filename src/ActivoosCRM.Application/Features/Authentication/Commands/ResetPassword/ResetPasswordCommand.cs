using ActivoosCRM.Application.Common.Models;
using MediatR;

namespace ActivoosCRM.Application.Features.Authentication.Commands.ResetPassword;

/// <summary>
/// Command to reset user password using reset token
/// </summary>
public class ResetPasswordCommand : IRequest<Result<ResetPasswordResponse>>
{
    /// <summary>
    /// Password reset token received via email
    /// </summary>
    public string ResetToken { get; set; } = string.Empty;

    /// <summary>
    /// New password for the user
    /// </summary>
    public string NewPassword { get; set; } = string.Empty;

    /// <summary>
    /// Confirmation of the new password
    /// </summary>
    public string ConfirmPassword { get; set; } = string.Empty;
}