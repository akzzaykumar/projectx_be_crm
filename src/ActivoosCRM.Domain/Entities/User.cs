using ActivoosCRM.Domain.Common;

namespace ActivoosCRM.Domain.Entities;

/// <summary>
/// User entity representing system users
/// </summary>
public class User : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string Role { get; set; } = "admin"; // admin, manager, staff, viewer
    public string? ProfileImage { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiry { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public ICollection<Customer> CreatedCustomers { get; set; } = new List<Customer>();
    public ICollection<Activity> CreatedActivities { get; set; } = new List<Activity>();
    public ICollection<Booking> CreatedBookings { get; set; } = new List<Booking>();
}
