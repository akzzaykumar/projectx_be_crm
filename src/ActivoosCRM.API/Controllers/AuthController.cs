using ActivoosCRM.Application.Common.Models;
using ActivoosCRM.Application.Features.Authentication.Commands.RegisterUser;
using ActivoosCRM.Application.Features.Authentication.Commands.LoginUser;
using ActivoosCRM.Application.Features.Authentication.Commands.RefreshToken;
using ActivoosCRM.Application.Features.Authentication.Commands.Logout;
using ActivoosCRM.Application.Features.Authentication.Commands.ForgotPassword;
using ActivoosCRM.Application.Features.Authentication.Commands.ResetPassword;
using ActivoosCRM.Application.Features.Authentication.Commands.VerifyEmail;
using ActivoosCRM.Application.Features.Authentication.Commands.ResendVerificationEmail;
using ActivoosCRM.Application.Features.Authentication.Commands.GoogleLogin;
using ActivoosCRM.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ActivoosCRM.API.Controllers;

/// <summary>
/// Authentication controller
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IMediator mediator, ILogger<AuthController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Register a new user account
    /// </summary>
    /// <param name="request">Registration request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Registration response</returns>
    /// <response code="201">User registered successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="409">User with email already exists</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponse<RegisterUserResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterUserRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("User registration attempt for email: {Email}", request.Email);

        var command = new RegisterUserCommand
        {
            Email = request.Email,
            Password = request.Password,
            FirstName = request.FirstName,
            LastName = request.LastName,
            PhoneNumber = request.PhoneNumber,
            Role = request.Role ?? UserRole.Customer // Default to Customer if not specified
        };

        var result = await _mediator.Send(command, cancellationToken);

        return result.Match<IActionResult>(
            onSuccess: data =>
            {
                _logger.LogInformation("User registered successfully with ID: {UserId}", data.UserId);
                return CreatedAtAction(
                    nameof(Register),
                    ApiResponse<RegisterUserResponse>.CreateSuccess(data, "User registered successfully"));
            },
            onFailure: error =>
            {
                _logger.LogWarning("User registration failed: {Error}", error);

                if (error.Contains("already exists"))
                {
                    return Conflict(ApiResponse.CreateFailure(error, "USER_ALREADY_EXISTS"));
                }

                return BadRequest(ApiResponse.CreateFailure(error, "REGISTRATION_FAILED"));
            });
    }

    /// <summary>
    /// User login
    /// </summary>
    /// <param name="request">Login request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Login response with tokens and user information</returns>
    /// <response code="200">Login successful</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="401">Invalid credentials or account locked</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<LoginUserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginUserRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("User login attempt for email: {Email}", request.Email);

        var command = new LoginUserCommand
        {
            Email = request.Email,
            Password = request.Password,
            RememberMe = request.RememberMe
        };

        var result = await _mediator.Send(command, cancellationToken);

        return result.Match<IActionResult>(
            onSuccess: data =>
            {
                _logger.LogInformation("User logged in successfully: {Email}", request.Email);
                return Ok(ApiResponse<LoginUserResponse>.CreateSuccess(data, "Login successful"));
            },
            onFailure: error =>
            {
                _logger.LogWarning("User login failed for email: {Email}, Error: {Error}", request.Email, error);

                if (error.Contains("Invalid email or password"))
                {
                    return Unauthorized(ApiResponse.CreateFailure(error, "INVALID_CREDENTIALS"));
                }

                if (error.Contains("locked") || error.Contains("inactive"))
                {
                    return Unauthorized(ApiResponse.CreateFailure(error, "ACCOUNT_LOCKED_OR_INACTIVE"));
                }

                return BadRequest(ApiResponse.CreateFailure(error, "LOGIN_FAILED"));
            });
    }

    /// <summary>
    /// Google Sign-In authentication
    /// </summary>
    /// <param name="request">Google login request containing ID token</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Login response with tokens and user information</returns>
    /// <response code="200">Google login successful</response>
    /// <response code="400">Invalid Google token or request data</response>
    /// <response code="401">Account locked or inactive</response>
    [HttpPost("google-login")]
    [ProducesResponseType(typeof(ApiResponse<GoogleLoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Google login attempt");

        var command = new GoogleLoginCommand
        {
            IdToken = request.IdToken,
            RememberMe = request.RememberMe
        };

        var result = await _mediator.Send(command, cancellationToken);

        return result.Match<IActionResult>(
            onSuccess: data =>
            {
                _logger.LogInformation("Google login successful for user: {Email}, IsNewUser: {IsNewUser}",
                    data.Email, data.IsNewUser);
                var message = data.IsNewUser
                    ? "Account created and logged in successfully"
                    : "Login successful";
                return Ok(ApiResponse<GoogleLoginResponse>.CreateSuccess(data, message));
            },
            onFailure: error =>
            {
                _logger.LogWarning("Google login failed: {Error}", error);

                if (error.Contains("locked") || error.Contains("inactive"))
                {
                    return Unauthorized(ApiResponse.CreateFailure(error, "ACCOUNT_LOCKED_OR_INACTIVE"));
                }

                return BadRequest(ApiResponse.CreateFailure(error, "GOOGLE_LOGIN_FAILED"));
            });
    }

    /// <summary>
    /// Refresh JWT access token using refresh token
    /// </summary>
    /// <param name="request">Refresh token request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>New access and refresh tokens</returns>
    /// <response code="200">Tokens refreshed successfully</response>
    /// <response code="400">Invalid or expired refresh token</response>
    /// <response code="401">Refresh token is invalid</response>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(ApiResponse<RefreshTokenResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var command = new RefreshTokenCommand
        {
            RefreshToken = request.RefreshToken,
            RememberMe = request.RememberMe
        };

        var result = await _mediator.Send(command, cancellationToken);

        return result.Match<IActionResult>(
            success => Ok(ApiResponse<RefreshTokenResponse>.CreateSuccess(success, "Tokens refreshed successfully")),
            error =>
            {
                if (error.Contains("expired") || error.Contains("invalid", StringComparison.OrdinalIgnoreCase))
                {
                    return Unauthorized(ApiResponse.CreateFailure(error, "TOKEN_INVALID"));
                }

                return BadRequest(ApiResponse.CreateFailure(error, "REFRESH_FAILED"));
            });
    }

    /// <summary>
    /// Logout user by invalidating refresh token
    /// </summary>
    /// <param name="request">Logout request containing refresh token</param>
    /// <returns>Logout confirmation</returns>
    /// <response code="200">User logged out successfully</response>
    /// <response code="400">Invalid request or refresh token</response>
    /// <response code="401">Invalid or expired refresh token</response>
    [HttpPost("logout")]
    [ProducesResponseType(typeof(ApiResponse<LogoutResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
    {
        _logger.LogInformation("User logout attempt");

        var command = new LogoutCommand
        {
            RefreshToken = request.RefreshToken
        };

        var result = await _mediator.Send(command);

        return result.Match<IActionResult>(
            success =>
            {
                _logger.LogInformation("User logged out successfully");
                return Ok(ApiResponse<LogoutResponse>.CreateSuccess(success, "Logged out successfully"));
            },
            error =>
            {
                _logger.LogWarning("User logout failed: {Error}", error);

                if (error.Contains("Invalid or expired", StringComparison.OrdinalIgnoreCase))
                {
                    return Unauthorized(ApiResponse.CreateFailure(error, "TOKEN_INVALID"));
                }

                return BadRequest(ApiResponse.CreateFailure(error, "LOGOUT_FAILED"));
            });
    }

    /// <summary>
    /// Request password reset for user account
    /// </summary>
    /// <param name="request">Forgot password request containing email</param>
    /// <returns>Password reset confirmation</returns>
    /// <response code="200">Password reset instructions sent (if account exists)</response>
    /// <response code="400">Invalid request</response>
    [HttpPost("forgot-password")]
    [ProducesResponseType(typeof(ApiResponse<ForgotPasswordResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        _logger.LogInformation("Forgot password request for email: {Email}", request.Email);

        var command = new ForgotPasswordCommand
        {
            Email = request.Email
        };

        var result = await _mediator.Send(command);

        return result.Match<IActionResult>(
            success =>
            {
                _logger.LogInformation("Forgot password request processed for email: {Email}", request.Email);
                return Ok(ApiResponse<ForgotPasswordResponse>.CreateSuccess(success, "Password reset instructions sent"));
            },
            error =>
            {
                _logger.LogWarning("Forgot password request failed: {Error}", error);
                return BadRequest(ApiResponse.CreateFailure(error, "FORGOT_PASSWORD_FAILED"));
            });
    }

    /// <summary>
    /// Reset user password using reset token
    /// </summary>
    /// <param name="request">Reset password request</param>
    /// <returns>Password reset confirmation</returns>
    /// <response code="200">Password reset successful</response>
    /// <response code="400">Invalid request or expired token</response>
    [HttpPost("reset-password")]
    [ProducesResponseType(typeof(ApiResponse<ResetPasswordResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        _logger.LogInformation("Password reset attempt with token: {Token}", request.ResetToken);

        var command = new ResetPasswordCommand
        {
            ResetToken = request.ResetToken,
            NewPassword = request.NewPassword,
            ConfirmPassword = request.ConfirmPassword
        };

        var result = await _mediator.Send(command);

        return result.Match<IActionResult>(
            success =>
            {
                _logger.LogInformation("Password reset successful for token: {Token}", request.ResetToken);
                return Ok(ApiResponse<ResetPasswordResponse>.CreateSuccess(success, "Password reset successful"));
            },
            error =>
            {
                _logger.LogWarning("Password reset failed: {Error}", error);
                return BadRequest(ApiResponse.CreateFailure(error, "RESET_PASSWORD_FAILED"));
            });
    }

    /// <summary>
    /// Verify user email address using verification token
    /// </summary>
    /// <param name="request">Email verification request</param>
    /// <returns>Email verification confirmation</returns>
    /// <response code="200">Email verified successfully</response>
    /// <response code="400">Invalid request or expired token</response>
    [HttpPost("verify-email")]
    [ProducesResponseType(typeof(ApiResponse<VerifyEmailResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request)
    {
        _logger.LogInformation("Email verification attempt for email: {Email}", request.Email);

        var command = new VerifyEmailCommand
        {
            Email = request.Email,
            Token = request.Token
        };

        var result = await _mediator.Send(command);

        if (result.Success)
        {
            _logger.LogInformation("Email verified successfully for: {Email}", request.Email);
            return Ok(ApiResponse<VerifyEmailResponse>.CreateSuccess(result, "Email verified successfully"));
        }
        else
        {
            _logger.LogWarning("Email verification failed: {Message}", result.Message);
            return BadRequest(ApiResponse.CreateFailure(result.Message, "EMAIL_VERIFICATION_FAILED"));
        }
    }

    /// <summary>
    /// Resend email verification to user
    /// </summary>
    /// <param name="request">Resend verification email request</param>
    /// <returns>Confirmation that verification email has been sent</returns>
    /// <response code="200">Verification email sent</response>
    /// <response code="400">Invalid request</response>
    [HttpPost("resend-verification")]
    [ProducesResponseType(typeof(ApiResponse<ResendVerificationEmailResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResendVerificationEmail([FromBody] ResendVerificationEmailRequest request)
    {
        _logger.LogInformation("Resend verification email request for: {Email}", request.Email);

        var command = new ResendVerificationEmailCommand
        {
            Email = request.Email
        };

        var result = await _mediator.Send(command);

        if (result.Success)
        {
            _logger.LogInformation("Verification email sent for: {Email}", request.Email);
            return Ok(ApiResponse<ResendVerificationEmailResponse>.CreateSuccess(result, result.Message));
        }
        else
        {
            _logger.LogWarning("Resend verification email failed: {Message}", result.Message);
            return BadRequest(ApiResponse.CreateFailure(result.Message, "RESEND_VERIFICATION_FAILED"));
        }
    }
}

/// <summary>
/// User registration request model
/// </summary>
public class RegisterUserRequest
{
    /// <summary>
    /// User email address
    /// </summary>
    /// <example>user@example.com</example>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User password (minimum 8 characters with uppercase, lowercase, number and special character)
    /// </summary>
    /// <example>SecurePassword123!</example>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// User first name
    /// </summary>
    /// <example>John</example>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// User last name
    /// </summary>
    /// <example>Doe</example>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// User phone number in international format (optional)
    /// </summary>
    /// <example>+919876543210</example>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// User role (defaults to Customer if not specified)
    /// </summary>
    /// <example>Customer</example>
    public UserRole? Role { get; set; }
}

/// <summary>
/// User login request model
/// </summary>
public class LoginUserRequest
{
    /// <summary>
    /// User email address
    /// </summary>
    /// <example>user@example.com</example>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User password
    /// </summary>
    /// <example>SecurePassword123!</example>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Whether to remember the user (longer token expiry)
    /// </summary>
    /// <example>false</example>
    public bool RememberMe { get; set; } = false;
}

/// <summary>
/// Google login request model
/// </summary>
public class GoogleLoginRequest
{
    /// <summary>
    /// Google ID token received from Google Sign-In client
    /// </summary>
    /// <example>eyJhbGciOiJSUzI1NiIsImtpZCI6IjEifQ...</example>
    public string IdToken { get; set; } = string.Empty;

    /// <summary>
    /// Whether to remember the user (longer token expiry)
    /// </summary>
    /// <example>false</example>
    public bool RememberMe { get; set; } = false;
}

/// <summary>
/// Refresh token request model
/// </summary>
public class RefreshTokenRequest
{
    /// <summary>
    /// Refresh token to exchange for new access token
    /// </summary>
    /// <example>eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...</example>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// Whether to remember the user (longer token expiry)
    /// </summary>
    /// <example>false</example>
    public bool RememberMe { get; set; } = false;
}

/// <summary>
/// Logout request model
/// </summary>
public class LogoutRequest
{
    /// <summary>
    /// Refresh token to invalidate
    /// </summary>
    /// <example>eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...</example>
    public string RefreshToken { get; set; } = string.Empty;
}

/// <summary>
/// Forgot password request model
/// </summary>
public class ForgotPasswordRequest
{
    /// <summary>
    /// Email address for password reset
    /// </summary>
    /// <example>user@example.com</example>
    public string Email { get; set; } = string.Empty;
}

/// <summary>
/// Reset password request model
/// </summary>
public class ResetPasswordRequest
{
    /// <summary>
    /// Password reset token received via email
    /// </summary>
    /// <example>eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...</example>
    public string ResetToken { get; set; } = string.Empty;

    /// <summary>
    /// New password (minimum 8 characters with uppercase, lowercase, number and special character)
    /// </summary>
    /// <example>NewSecurePassword123!</example>
    public string NewPassword { get; set; } = string.Empty;

    /// <summary>
    /// Confirmation of the new password
    /// </summary>
    /// <example>NewSecurePassword123!</example>
    public string ConfirmPassword { get; set; } = string.Empty;
}

/// <summary>
/// Email verification request model
/// </summary>
public class VerifyEmailRequest
{
    /// <summary>
    /// Email address to verify
    /// </summary>
    /// <example>user@example.com</example>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Email verification token received via email
    /// </summary>
    /// <example>123456</example>
    public string Token { get; set; } = string.Empty;
}

/// <summary>
/// Resend verification email request model
/// </summary>
public class ResendVerificationEmailRequest
{
    /// <summary>
    /// Email address to resend verification to
    /// </summary>
    /// <example>user@example.com</example>
    public string Email { get; set; } = string.Empty;
}