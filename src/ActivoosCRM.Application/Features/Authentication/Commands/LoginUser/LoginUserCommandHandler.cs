using ActivoosCRM.Application.Common.Interfaces;
using ActivoosCRM.Application.Common.Models;
using ActivoosCRM.Domain.Interfaces;
using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ActivoosCRM.Application.Features.Authentication.Commands.LoginUser;

/// <summary>
/// Handler for LoginUserCommand
/// </summary>
public class LoginUserCommandHandler : IRequestHandler<LoginUserCommand, Result<LoginUserResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IMapper _mapper;
    private readonly ILogger<LoginUserCommandHandler> _logger;

    public LoginUserCommandHandler(
        IApplicationDbContext context,
        IJwtTokenService jwtTokenService,
        IMapper mapper,
        ILogger<LoginUserCommandHandler> logger)
    {
        _context = context;
        _jwtTokenService = jwtTokenService;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Handles the login user request
    /// </summary>
    /// <param name="request">Login request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Login response with tokens and user info</returns>
    public async Task<Result<LoginUserResponse>> Handle(LoginUserCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Processing login request for email: {Email}", request.Email);

            // Find user by email
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower() && !u.IsDeleted, cancellationToken);

            if (user == null)
            {
                _logger.LogWarning("Login attempt with non-existent email: {Email}", request.Email);
                return Result<LoginUserResponse>.CreateFailure("Invalid email or password");
            }

            // Check if account is locked
            if (user.LockedUntil.HasValue && user.LockedUntil.Value > DateTime.UtcNow)
            {
                _logger.LogWarning("Login attempt for locked account: {Email}", request.Email);
                return Result<LoginUserResponse>.CreateFailure("Account is temporarily locked due to multiple failed login attempts");
            }

            // Check if account is active
            if (!user.IsActive)
            {
                _logger.LogWarning("Login attempt for inactive account: {Email}", request.Email);
                return Result<LoginUserResponse>.CreateFailure("Account is not active");
            }

            // Verify password
            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                _logger.LogWarning("Invalid password attempt for email: {Email}", request.Email);

                // Record failed login attempt
                user.RecordFailedLogin();
                user.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);

                if (user.IsLocked)
                {
                    _logger.LogWarning("Account locked due to multiple failed login attempts: {Email}", request.Email);
                }

                return Result<LoginUserResponse>.CreateFailure("Invalid email or password");
            }

            // Successful login - record success and update last login
            user.RecordSuccessfulLogin();

            // Generate tokens
            var accessToken = _jwtTokenService.GenerateAccessToken(user, request.RememberMe);
            var refreshToken = _jwtTokenService.GenerateRefreshToken();
            var accessTokenExpiry = _jwtTokenService.GetAccessTokenExpiry(request.RememberMe);
            var refreshTokenExpiry = _jwtTokenService.GetRefreshTokenExpiry(request.RememberMe);

            // Update user with new refresh token
            user.SetRefreshToken(refreshToken, refreshTokenExpiry);
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            // Create response
            var response = new LoginUserResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                AccessTokenExpiry = accessTokenExpiry,
                RefreshTokenExpiry = refreshTokenExpiry,
                User = new UserInfo
                {
                    Id = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    PhoneNumber = user.PhoneNumber,
                    Role = user.Role.ToString(),
                    IsEmailVerified = user.IsEmailVerified
                }
            };

            _logger.LogInformation("User successfully logged in: {Email}", request.Email);
            return Result<LoginUserResponse>.CreateSuccess(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during login for email: {Email}", request.Email);
            return Result<LoginUserResponse>.CreateFailure("An error occurred during login");
        }
    }
}