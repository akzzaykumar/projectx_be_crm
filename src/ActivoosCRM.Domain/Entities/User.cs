using ActivoosCRM.Domain.Common;
using ActivoosCRM.Domain.Enums;

namespace ActivoosCRM.Domain.Entities;

/// <summary>
/// User entity - Represents system users with authentication and authorization
/// Responsible for: User identity, authentication data, role management
/// </summary>
public class User : AuditableEntity
{
    private User() { } // Private constructor for EF Core

    // Identity properties
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string? PhoneNumber { get; private set; }

    // External authentication
    public string? GoogleId { get; private set; }
    public string? ExternalAuthProvider { get; private set; }
    public bool IsExternalAuth { get; private set; } = false;

    // Role and status
    public UserRole Role { get; private set; }
    public bool IsActive { get; private set; } = true;
    public bool IsEmailVerified { get; private set; } = false;
    public DateTime? EmailVerifiedAt { get; private set; }

    // Authentication tokens
    public string? RefreshToken { get; private set; }
    public DateTime? RefreshTokenExpiry { get; private set; }
    public string? PasswordResetToken { get; private set; }
    public DateTime? PasswordResetTokenExpiry { get; private set; }
    public string? EmailVerificationToken { get; private set; }
    public DateTime? EmailVerificationTokenExpiry { get; private set; }
    public int EmailVerificationAttempts { get; private set; } = 0;
    public DateTime? LastEmailVerificationAttempt { get; private set; }
    public int EmailResendCount { get; private set; } = 0;
    public DateTime? LastEmailResendAt { get; private set; }

    // Account management
    public DateTime? LastLoginAt { get; private set; }
    public int FailedLoginAttempts { get; private set; } = 0;
    public DateTime? LockedUntil { get; private set; }

    // Computed properties
    public string FullName => $"{FirstName} {LastName}";
    public bool IsLocked => LockedUntil.HasValue && LockedUntil.Value > DateTime.UtcNow;

    // Navigation properties
    public virtual ActivityProvider? ActivityProvider { get; private set; }
    public virtual CustomerProfile? CustomerProfile { get; private set; }
    public virtual ICollection<Review> Reviews { get; private set; } = new List<Review>();
    public virtual ICollection<Wishlist> Wishlists { get; private set; } = new List<Wishlist>();
    public virtual ICollection<Notification> Notifications { get; private set; } = new List<Notification>();
    public virtual ICollection<CouponUsage> CouponUsages { get; private set; } = new List<CouponUsage>();

    /// <summary>
    /// Factory method to create a new user
    /// </summary>
    public static User Create(
        string email,
        string passwordHash,
        string firstName,
        string lastName,
        UserRole role,
        string? phoneNumber = null)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required", nameof(email));

        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash is required", nameof(passwordHash));

        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("First name is required", nameof(firstName));

        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Last name is required", nameof(lastName));

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email.ToLowerInvariant().Trim(),
            PasswordHash = passwordHash,
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            PhoneNumber = phoneNumber?.Trim(),
            Role = role,
            IsActive = true,
            IsEmailVerified = false,
            FailedLoginAttempts = 0
        };

        return user;
    }

    /// <summary>
    /// Factory method to create a new user from Google authentication
    /// </summary>
    public static User CreateFromGoogle(
        string email,
        string googleId,
        string firstName,
        string lastName,
        UserRole role = UserRole.Customer)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required", nameof(email));

        if (string.IsNullOrWhiteSpace(googleId))
            throw new ArgumentException("Google ID is required", nameof(googleId));

        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("First name is required", nameof(firstName));

        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Last name is required", nameof(lastName));

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email.ToLowerInvariant().Trim(),
            PasswordHash = string.Empty, // No password for Google auth users
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            Role = role,
            IsActive = true,
            IsEmailVerified = true, // Google accounts are pre-verified
            EmailVerifiedAt = DateTime.UtcNow,
            GoogleId = googleId,
            ExternalAuthProvider = "Google",
            IsExternalAuth = true,
            FailedLoginAttempts = 0
        };

        return user;
    }

    /// <summary>
    /// Update user profile information
    /// </summary>
    public void UpdateProfile(string firstName, string lastName, string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("First name is required", nameof(firstName));

        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Last name is required", nameof(lastName));

        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        PhoneNumber = phoneNumber?.Trim();
    }

    /// <summary>
    /// Change user password
    /// </summary>
    public void ChangePassword(string newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
            throw new ArgumentException("Password hash is required", nameof(newPasswordHash));

        PasswordHash = newPasswordHash;

        // Clear password reset token if exists
        PasswordResetToken = null;
        PasswordResetTokenExpiry = null;
    }

    /// <summary>
    /// Set password reset token
    /// </summary>
    public void SetPasswordResetToken(string token, DateTime expiry)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentException("Token is required", nameof(token));

        if (expiry <= DateTime.UtcNow)
            throw new ArgumentException("Expiry must be in the future", nameof(expiry));

        PasswordResetToken = token;
        PasswordResetTokenExpiry = expiry;
    }

    /// <summary>
    /// Clear password reset token
    /// </summary>
    public void ClearPasswordResetToken()
    {
        PasswordResetToken = null;
        PasswordResetTokenExpiry = null;
    }

    /// <summary>
    /// Verify email address
    /// </summary>
    public void VerifyEmail()
    {
        IsEmailVerified = true;
        EmailVerifiedAt = DateTime.UtcNow;

        // Clear verification token and reset counters after successful verification
        EmailVerificationToken = null;
        EmailVerificationTokenExpiry = null;
        EmailVerificationAttempts = 0;
        LastEmailVerificationAttempt = null;
        EmailResendCount = 0;
        LastEmailResendAt = null;
    }

    /// <summary>
    /// Set email verification token (OTP)
    /// </summary>
    public void SetEmailVerificationToken(string token, DateTime expiry)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentException("Token is required", nameof(token));

        if (expiry <= DateTime.UtcNow)
            throw new ArgumentException("Expiry must be in the future", nameof(expiry));

        EmailVerificationToken = token;
        EmailVerificationTokenExpiry = expiry;

        // Track resend attempts
        EmailResendCount++;
        LastEmailResendAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Clear email verification token
    /// </summary>
    public void ClearEmailVerificationToken()
    {
        EmailVerificationToken = null;
        EmailVerificationTokenExpiry = null;
        EmailVerificationAttempts = 0;
        LastEmailVerificationAttempt = null;
    }

    /// <summary>
    /// Check if email verification token is valid
    /// </summary>
    public bool IsEmailVerificationTokenValid(string token)
    {
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(EmailVerificationToken))
            return false;

        return EmailVerificationToken == token &&
               EmailVerificationTokenExpiry.HasValue &&
               EmailVerificationTokenExpiry.Value > DateTime.UtcNow;
    }

    /// <summary>
    /// Record a failed email verification attempt
    /// </summary>
    public void RecordFailedEmailVerificationAttempt()
    {
        EmailVerificationAttempts++;
        LastEmailVerificationAttempt = DateTime.UtcNow;
    }

    /// <summary>
    /// Check if user can request a new verification email (rate limiting)
    /// </summary>
    public bool CanRequestNewVerificationEmail()
    {
        // Allow up to 5 emails per day
        if (EmailResendCount >= 5 && LastEmailResendAt.HasValue &&
            LastEmailResendAt.Value.Date == DateTime.UtcNow.Date)
        {
            return false;
        }

        // Must wait at least 1 minute between resend requests
        if (LastEmailResendAt.HasValue &&
            DateTime.UtcNow.Subtract(LastEmailResendAt.Value).TotalMinutes < 1)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Check if account is temporarily locked due to too many verification attempts
    /// </summary>
    public bool IsEmailVerificationLocked()
    {
        // Lock for 15 minutes after 5 failed attempts
        if (EmailVerificationAttempts >= 5 && LastEmailVerificationAttempt.HasValue)
        {
            var lockoutPeriod = TimeSpan.FromMinutes(15);
            return DateTime.UtcNow.Subtract(LastEmailVerificationAttempt.Value) < lockoutPeriod;
        }

        return false;
    }

    /// <summary>
    /// Reset daily email resend count (to be called by a background job)
    /// </summary>
    public void ResetDailyEmailResendCount()
    {
        if (LastEmailResendAt.HasValue && LastEmailResendAt.Value.Date < DateTime.UtcNow.Date)
        {
            EmailResendCount = 0;
        }
    }

    /// <summary>
    /// Set refresh token for authentication
    /// </summary>
    public void SetRefreshToken(string token, DateTime expiry)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentException("Token is required", nameof(token));

        if (expiry <= DateTime.UtcNow)
            throw new ArgumentException("Expiry must be in the future", nameof(expiry));

        RefreshToken = token;
        RefreshTokenExpiry = expiry;
    }

    /// <summary>
    /// Clear refresh token
    /// </summary>
    public void ClearRefreshToken()
    {
        RefreshToken = null;
        RefreshTokenExpiry = null;
    }

    /// <summary>
    /// Record successful login
    /// </summary>
    public void RecordSuccessfulLogin()
    {
        LastLoginAt = DateTime.UtcNow;
        FailedLoginAttempts = 0;
        LockedUntil = null;
    }

    /// <summary>
    /// Record failed login attempt
    /// </summary>
    public void RecordFailedLogin()
    {
        FailedLoginAttempts++;

        // Lock account after 5 failed attempts for 15 minutes
        if (FailedLoginAttempts >= 5)
        {
            LockedUntil = DateTime.UtcNow.AddMinutes(15);
        }
    }

    /// <summary>
    /// Unlock user account
    /// </summary>
    public void Unlock()
    {
        FailedLoginAttempts = 0;
        LockedUntil = null;
    }

    /// <summary>
    /// Activate user account
    /// </summary>
    public void Activate()
    {
        IsActive = true;
    }

    /// <summary>
    /// Deactivate user account
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        ClearRefreshToken();
    }

    /// <summary>
    /// Change user role
    /// </summary>
    public void ChangeRole(UserRole newRole)
    {
        Role = newRole;
    }

    /// <summary>
    /// Check if user has specific role
    /// </summary>
    public bool HasRole(UserRole role)
    {
        return Role == role;
    }

    /// <summary>
    /// Check if user is admin
    /// </summary>
    public bool IsAdmin()
    {
        return Role == UserRole.Admin;
    }
}
