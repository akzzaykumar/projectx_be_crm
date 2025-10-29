using ActivoosCRM.Application.Common.Interfaces;
using ActivoosCRM.Application.Common.Models;
using ActivoosCRM.Domain.Entities;
using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;

namespace ActivoosCRM.Application.Features.Authentication.Commands.RegisterUser;

/// <summary>
/// Handler for RegisterUserCommand
/// </summary>
public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, Result<RegisterUserResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHashingService _passwordHashingService;
    private readonly IEmailService _emailService;
    private readonly IMapper _mapper;
    private readonly ILogger<RegisterUserCommandHandler> _logger;

    public RegisterUserCommandHandler(
        IApplicationDbContext context,
        IPasswordHashingService passwordHashingService,
        IEmailService emailService,
        IMapper mapper,
        ILogger<RegisterUserCommandHandler> logger)
    {
        _context = context;
        _passwordHashingService = passwordHashingService;
        _emailService = emailService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<RegisterUserResponse>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing user registration for email: {Email}", request.Email);

        try
        {
            // Check if user already exists
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email.ToLowerInvariant(), cancellationToken);

            if (existingUser != null)
            {
                _logger.LogWarning("Registration failed: User with email {Email} already exists", request.Email);
                return Result<RegisterUserResponse>.CreateFailure("A user with this email already exists");
            }

            // Hash password
            var passwordHash = _passwordHashingService.HashPassword(request.Password);

            // Create user entity using factory method
            var user = User.Create(
                email: request.Email,
                passwordHash: passwordHash,
                firstName: request.FirstName,
                lastName: request.LastName,
                role: request.Role,
                phoneNumber: request.PhoneNumber);

            // Generate email verification token (6-digit OTP)
            var verificationToken = GenerateVerificationToken();
            var expiry = DateTime.UtcNow.AddHours(24); // Token valid for 24 hours
            user.SetEmailVerificationToken(verificationToken, expiry);

            // Add user to context and save
            _context.Users.Add(user);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("User registered successfully with ID: {UserId}", user.Id);

            // Send verification email (don't fail registration if email fails)
            try
            {
                await _emailService.SendEmailVerificationAsync(
                    user.Email,
                    verificationToken,
                    user.FirstName);

                _logger.LogInformation("Verification email sent to: {Email}", user.Email);
            }
            catch (Exception emailEx)
            {
                _logger.LogError(emailEx, "Failed to send verification email to: {Email}", user.Email);
                // Continue with registration even if email fails
            }

            // Map to response
            var response = _mapper.Map<RegisterUserResponse>(user);

            return Result<RegisterUserResponse>.CreateSuccess(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while registering user with email: {Email}", request.Email);
            return Result<RegisterUserResponse>.CreateFailure("An error occurred during registration");
        }
    }

    private static string GenerateVerificationToken()
    {
        // Generate a 6-digit numeric code
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[4];
        rng.GetBytes(bytes);
        var code = Math.Abs(BitConverter.ToInt32(bytes, 0)) % 1000000;
        return code.ToString("D6");
    }
}