using ActivoosCRM.Application.Common.Models;
using MediatR;

namespace ActivoosCRM.Application.Features.Authentication.Commands.ForgotPassword;

/// <summary>
/// Command to initiate password reset process
/// </summary>
public class ForgotPasswordCommand : IRequest<Result<ForgotPasswordResponse>>
{
    /// <summary>
    /// Email address of the user requesting password reset
    /// </summary>
    public string Email { get; set; } = string.Empty;
}