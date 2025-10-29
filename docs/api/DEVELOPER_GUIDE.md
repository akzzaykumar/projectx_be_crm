# Developer Guide - Adding New Features

This guide shows you how to add new features following the established clean architecture pattern.

## 🎯 Adding a New API Endpoint (Example: Customer Registration)

### Step 1: Create DTOs in Application Layer

```csharp
// File: src/ActivoosCRM.Application/Features/Customers/DTOs/CreateCustomerDto.cs

namespace ActivoosCRM.Application.Features.Customers.DTOs;

public class CreateCustomerDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Notes { get; set; }
}

public class CustomerResponseDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
```

### Step 2: Create Validator (FluentValidation)

```csharp
// File: src/ActivoosCRM.Application/Features/Customers/Validators/CreateCustomerValidator.cs

using FluentValidation;

namespace ActivoosCRM.Application.Features.Customers.Validators;

public class CreateCustomerValidator : AbstractValidator<CreateCustomerDto>
{
    public CreateCustomerValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required")
            .MaximumLength(100).WithMessage("First name cannot exceed 100 characters");
            
        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required")
            .MaximumLength(100).WithMessage("Last name cannot exceed 100 characters");
            
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");
            
        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Phone is required")
            .Matches(@"^\+?[1-9]\d{1,14}$").WithMessage("Invalid phone format");
    }
}
```

### Step 3: Create Service Interface

```csharp
// File: src/ActivoosCRM.Application/Features/Customers/ICustomerService.cs

namespace ActivoosCRM.Application.Features.Customers;

public interface ICustomerService
{
    Task<CustomerResponseDto> CreateCustomerAsync(CreateCustomerDto dto, CancellationToken cancellationToken = default);
    Task<CustomerResponseDto?> GetCustomerByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<PaginatedResponse<CustomerResponseDto>> GetCustomersAsync(PaginationParams pagination, string? search = null);
    Task<CustomerResponseDto> UpdateCustomerAsync(int id, UpdateCustomerDto dto, CancellationToken cancellationToken = default);
    Task DeleteCustomerAsync(int id, CancellationToken cancellationToken = default);
}
```

### Step 4: Implement Service in Infrastructure

```csharp
// File: src/ActivoosCRM.Infrastructure/Services/CustomerService.cs

using ActivoosCRM.Application.Common.Exceptions;
using ActivoosCRM.Application.Common.Interfaces;
using ActivoosCRM.Application.Features.Customers;
using ActivoosCRM.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ActivoosCRM.Infrastructure.Services;

public class CustomerService : ICustomerService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IApplicationDbContext _context;

    public CustomerService(IUnitOfWork unitOfWork, IApplicationDbContext context)
    {
        _unitOfWork = unitOfWork;
        _context = context;
    }

    public async Task<CustomerResponseDto> CreateCustomerAsync(CreateCustomerDto dto, CancellationToken cancellationToken = default)
    {
        // Check for duplicate email
        var existingCustomer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Email == dto.Email, cancellationToken);
            
        if (existingCustomer != null)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                { "Email", new[] { "A customer with this email already exists" } }
            });
        }

        var customer = new Customer
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Email = dto.Email,
            Phone = dto.Phone,
            Address = dto.Address,
            Notes = dto.Notes,
            Status = "Active"
        };

        await _unitOfWork.Customers.AddAsync(customer, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(customer);
    }

    public async Task<CustomerResponseDto?> GetCustomerByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var customer = await _unitOfWork.Customers.GetByIdAsync(id, cancellationToken);
        
        if (customer == null)
        {
            throw new NotFoundException(nameof(Customer), id);
        }

        return MapToDto(customer);
    }

    private static CustomerResponseDto MapToDto(Customer customer)
    {
        return new CustomerResponseDto
        {
            Id = customer.Id,
            FirstName = customer.FirstName,
            LastName = customer.LastName,
            Email = customer.Email,
            Phone = customer.Phone,
            Status = customer.Status,
            CreatedAt = customer.CreatedAt
        };
    }
}
```

### Step 5: Register Service in Program.cs

```csharp
// File: src/ActivoosCRM.API/Program.cs

// Add this line with other service registrations
builder.Services.AddScoped<ICustomerService, CustomerService>();
```

### Step 6: Create Controller

```csharp
// File: src/ActivoosCRM.API/Controllers/CustomersController.cs

using ActivoosCRM.Application.Common.Models;
using ActivoosCRM.Application.Features.Customers;
using ActivoosCRM.Application.Features.Customers.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ActivoosCRM.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Require authentication
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;
    private readonly ILogger<CustomersController> _logger;

    public CustomersController(ICustomerService customerService, ILogger<CustomersController> logger)
    {
        _customerService = customerService;
        _logger = logger;
    }

    /// <summary>
    /// Create a new customer
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<CustomerResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateCustomer([FromBody] CreateCustomerDto dto)
    {
        _logger.LogInformation("Creating new customer with email: {Email}", dto.Email);
        
        var customer = await _customerService.CreateCustomerAsync(dto);
        
        var response = ApiResponse<CustomerResponseDto>.SuccessResponse(
            customer, 
            "Customer created successfully"
        );
        
        return CreatedAtAction(nameof(GetCustomerById), new { id = customer.Id }, response);
    }

    /// <summary>
    /// Get customer by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<CustomerResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCustomerById(int id)
    {
        var customer = await _customerService.GetCustomerByIdAsync(id);
        
        return Ok(ApiResponse<CustomerResponseDto>.SuccessResponse(customer));
    }

    /// <summary>
    /// Get all customers with pagination
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResponse<CustomerResponseDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCustomers(
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20,
        [FromQuery] string? search = null)
    {
        var pagination = new PaginationParams { Page = page, Limit = limit };
        var customers = await _customerService.GetCustomersAsync(pagination, search);
        
        return Ok(ApiResponse<PaginatedResponse<CustomerResponseDto>>.SuccessResponse(customers));
    }
}
```

### Step 7: Test Your Endpoint

**Start the API:**
```powershell
cd c:\workspace\activoos_be\src\ActivoosCRM.API
dotnet run
```

**Test with Swagger:**
1. Navigate to https://localhost:5001
2. Find POST /api/customers
3. Click "Try it out"
4. Enter test data
5. Execute

**Test with curl:**
```bash
curl -X POST "https://localhost:5001/api/customers" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{
    "firstName": "John",
    "lastName": "Doe",
    "email": "john.doe@example.com",
    "phone": "+1234567890"
  }'
```

## 🔄 Complete Feature Checklist

When adding a new feature, ensure you've covered:

- [ ] **Domain Layer**
  - [ ] Entity created (if needed)
  - [ ] Navigation properties configured
  - [ ] Business rules defined

- [ ] **Application Layer**
  - [ ] DTOs created
  - [ ] Validators created
  - [ ] Service interface defined
  - [ ] Custom exceptions (if needed)

- [ ] **Infrastructure Layer**
  - [ ] Service implementation
  - [ ] Repository methods (if needed)
  - [ ] Database migration (if schema changed)

- [ ] **API Layer**
  - [ ] Controller created
  - [ ] Endpoints documented
  - [ ] Authorization configured
  - [ ] Error handling
  - [ ] Service registered in DI

- [ ] **Testing**
  - [ ] Unit tests written
  - [ ] Integration tests written
  - [ ] Manual testing completed

## 🗄️ Adding Database Migration

When you modify entities:

```powershell
# Navigate to Infrastructure project
cd c:\workspace\activoos_be\src\ActivoosCRM.Infrastructure

# Add migration
dotnet ef migrations add AddCustomerFieldXYZ --startup-project ..\ActivoosCRM.API --output-dir Persistence/Migrations

# Review generated migration
# Edit if necessary

# Apply to database
dotnet ef database update --startup-project ..\ActivoosCRM.API
```

## 🛡️ Adding Authorization

### Role-Based Authorization

```csharp
[Authorize(Roles = "admin")]
public async Task<IActionResult> DeleteCustomer(int id)
{
    // Only admins can delete
}
```

### Custom Policy

```csharp
// In Program.cs
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CanManageCustomers", policy =>
        policy.RequireRole("admin", "manager"));
});

// In Controller
[Authorize(Policy = "CanManageCustomers")]
public async Task<IActionResult> UpdateCustomer(int id, UpdateCustomerDto dto)
{
    // ...
}
```

## 🧪 Writing Unit Tests

```csharp
// File: tests/ActivoosCRM.UnitTests/Services/CustomerServiceTests.cs

using ActivoosCRM.Application.Common.Exceptions;
using ActivoosCRM.Infrastructure.Services;
using Moq;
using Xunit;

namespace ActivoosCRM.UnitTests.Services;

public class CustomerServiceTests
{
    [Fact]
    public async Task CreateCustomer_WithValidData_ShouldSucceed()
    {
        // Arrange
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        var service = new CustomerService(mockUnitOfWork.Object);
        
        var dto = new CreateCustomerDto
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com",
            Phone = "+1234567890"
        };

        // Act
        var result = await service.CreateCustomerAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("John", result.FirstName);
        mockUnitOfWork.Verify(x => x.SaveChangesAsync(default), Times.Once);
    }
}
```

## 📝 Best Practices Summary

1. **Always validate input** with FluentValidation
2. **Use DTOs** - Never expose entities directly
3. **Handle exceptions** - Use custom exceptions
4. **Log important events** - Use ILogger
5. **Use async/await** - For all I/O operations
6. **Follow naming conventions** - Consistent patterns
7. **Document APIs** - XML comments for Swagger
8. **Write tests** - Unit and integration tests
9. **Use transactions** - For multi-step operations
10. **Keep controllers thin** - Business logic in services

## 🚀 Common Patterns

### Pagination

```csharp
var query = _context.Customers
    .Where(c => c.Status == "Active")
    .OrderBy(c => c.CreatedAt);

var totalItems = await query.CountAsync();
var items = await query
    .Skip((page - 1) * limit)
    .Take(limit)
    .ToListAsync();

return new PaginatedResponse<CustomerDto>
{
    Items = items.Select(MapToDto).ToList(),
    Pagination = new PaginationMetadata
    {
        Page = page,
        Limit = limit,
        TotalItems = totalItems,
        TotalPages = (int)Math.Ceiling(totalItems / (double)limit),
        HasNext = page * limit < totalItems,
        HasPrev = page > 1
    }
};
```

### Search & Filter

```csharp
var query = _context.Customers.AsQueryable();

if (!string.IsNullOrEmpty(search))
{
    query = query.Where(c => 
        c.FirstName.Contains(search) ||
        c.LastName.Contains(search) ||
        c.Email.Contains(search));
}

if (!string.IsNullOrEmpty(status))
{
    query = query.Where(c => c.Status == status);
}

var customers = await query.ToListAsync();
```

## 🔍 Debugging Tips

**Enable detailed EF Core logging:**
```json
{
  "Logging": {
    "LogLevel": {
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"
    }
  }
}
```

**View SQL queries in logs:**
```csharp
_logger.LogInformation("Executing query for customer {Id}", id);
```

**Use Swagger for API testing:**
- Faster than Postman for development
- Auto-generated from code
- Always up-to-date

---

**Happy Coding! 🎉**
