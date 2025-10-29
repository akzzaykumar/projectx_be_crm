using ActivoosCRM.Application.Common.Interfaces;
using ActivoosCRM.Application.Common.Models;
using ActivoosCRM.Domain.Entities;
using ActivoosCRM.Domain.Enums;
using Google.Apis.Auth;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ActivoosCRM.Application.Features.Authentication.Commands.GoogleLogin;

public class GoogleLoginCommandHandler : IRequestHandler<GoogleLoginCommand, Result<GoogleLoginResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GoogleLoginCommandHandler> _logger;

    public GoogleLoginCommandHandler(
        IApplicationDbContext context,
        IJwtTokenService jwtTokenService,
        IConfiguration configuration,
        ILogger<GoogleLoginCommandHandler> logger)
    {
        _context = context;
        _jwtTokenService = jwtTokenService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<Result<GoogleLoginResponse>> Handle(GoogleLoginCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var googleClientId = _configuration["Authentication:Google:ClientId"];
            if (string.IsNullOrEmpty(googleClientId))
            {
                return Result<GoogleLoginResponse>.CreateFailure("Google authentication is not configured");
            }

            GoogleJsonWebSignature.Payload payload;
            try
            {
                var settings = new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { googleClientId }
                };

                payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken, settings);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Invalid Google ID token");
                return Result<GoogleLoginResponse>.CreateFailure("Invalid Google authentication token");
            }

            var email = payload.Email;
            var googleId = payload.Subject;
            var firstName = payload.GivenName ?? string.Empty;
            var lastName = payload.FamilyName ?? string.Empty;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(googleId))
            {
                return Result<GoogleLoginResponse>.CreateFailure("Invalid Google user information");
            }

            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower() && !u.IsDeleted, cancellationToken);

            User user;
            bool isNewUser = false;

            if (existingUser == null)
            {
                user = User.CreateFromGoogle(
                    email: email,
                    googleId: googleId,
                    firstName: firstName,
                    lastName: lastName,
                    role: UserRole.Customer
                );

                _context.Users.Add(user);
                await _context.SaveChangesAsync(cancellationToken);

                isNewUser = true;
                _logger.LogInformation("New user created from Google authentication: {Email}", email);
            }
            else
            {
                user = existingUser;

                if (!user.IsActive)
                {
                    return Result<GoogleLoginResponse>.CreateFailure("User account is inactive");
                }

                if (user.IsLocked)
                {
                    return Result<GoogleLoginResponse>.CreateFailure("User account is temporarily locked");
                }

                _logger.LogInformation("Existing user logged in with Google: {Email}", email);
            }

            user.RecordSuccessfulLogin();

            var accessToken = _jwtTokenService.GenerateAccessToken(user, request.RememberMe);
            var refreshToken = _jwtTokenService.GenerateRefreshToken();
            var accessTokenExpiry = _jwtTokenService.GetAccessTokenExpiry(request.RememberMe);
            var refreshTokenExpiry = _jwtTokenService.GetRefreshTokenExpiry(request.RememberMe);

            user.SetRefreshToken(refreshToken, refreshTokenExpiry);
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            var expiresInSeconds = (int)(accessTokenExpiry - DateTime.UtcNow).TotalSeconds;

            return Result<GoogleLoginResponse>.CreateSuccess(new GoogleLoginResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresIn = expiresInSeconds,
                UserId = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = user.Role.ToString(),
                IsNewUser = isNewUser
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Google login");
            return Result<GoogleLoginResponse>.CreateFailure("An error occurred during Google authentication");
        }
    }
}
