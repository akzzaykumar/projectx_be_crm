using ActivoosCRM.Application.Features.Providers.Commands.CreateProvider;
using ActivoosCRM.Application.Features.Providers.Commands.UpdateProvider;
using ActivoosCRM.Application.Features.Providers.Queries.GetProviderById;
using ActivoosCRM.Application.Features.Providers.Queries.GetProviders;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ActivoosCRM.API.Controllers;

/// <summary>
/// Controller for Activity Provider management
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ProvidersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ProvidersController> _logger;

    public ProvidersController(IMediator mediator, ILogger<ProvidersController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get list of Activity Providers with filtering, sorting, and pagination
    /// </summary>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 10, max: 50)</param>
    /// <param name="search">Search by business name or description</param>
    /// <param name="locationId">Filter by location ID</param>
    /// <param name="isVerified">Filter by verification status</param>
    /// <param name="sortBy">Sort field: name, rating, createdAt (default: name)</param>
    /// <param name="sortOrder">Sort order: asc, desc (default: asc)</param>
    /// <returns>Paginated list of providers</returns>
    [HttpGet]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(typeof(object), 400)]
    [ProducesResponseType(typeof(object), 500)]
    public async Task<IActionResult> GetProviders(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] Guid? locationId = null,
        [FromQuery] bool? isVerified = null,
        [FromQuery] string? sortBy = "name",
        [FromQuery] string? sortOrder = "asc")
    {
        try
        {
            var query = new GetProvidersQuery
            {
                Page = page,
                PageSize = pageSize,
                Search = search,
                LocationId = locationId,
                IsVerified = isVerified,
                SortBy = sortBy,
                SortOrder = sortOrder
            };

            var result = await _mediator.Send(query);

            if (result.IsFailure)
            {
                return BadRequest(new { message = result.Message });
            }

            return Ok(new
            {
                success = true,
                data = result.Data,
                message = "Providers retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving providers");
            return StatusCode(500, new { message = "An error occurred while retrieving providers" });
        }
    }

    /// <summary>
    /// Get detailed information about a specific Activity Provider
    /// </summary>
    /// <param name="id">Provider ID</param>
    /// <returns>Provider details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(typeof(object), 404)]
    [ProducesResponseType(typeof(object), 500)]
    public async Task<IActionResult> GetProviderById(Guid id)
    {
        try
        {
            var query = new GetProviderByIdQuery { ProviderId = id };
            var result = await _mediator.Send(query);

            if (result.IsFailure)
            {
                return NotFound(new { message = result.Message });
            }

            return Ok(new
            {
                success = true,
                data = result.Data,
                message = "Provider retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving provider: {ProviderId}", id);
            return StatusCode(500, new { message = "An error occurred while retrieving provider" });
        }
    }

    /// <summary>
    /// Create a new Activity Provider profile
    /// </summary>
    /// <param name="request">Provider creation details</param>
    /// <returns>Created provider ID</returns>
    /// <remarks>
    /// Allows Customer role to upgrade to ActivityProvider, or existing ActivityProvider to create profile
    /// </remarks>
    [HttpPost]
    [Authorize(Roles = "Customer,ActivityProvider")]
    [ProducesResponseType(typeof(object), 201)]
    [ProducesResponseType(typeof(object), 400)]
    [ProducesResponseType(typeof(object), 401)]
    [ProducesResponseType(typeof(object), 403)]
    [ProducesResponseType(typeof(object), 500)]
    public async Task<IActionResult> CreateProvider([FromBody] CreateProviderRequest request)
    {
        try
        {
            var userId = GetUserIdFromClaims();
            if (userId == Guid.Empty)
            {
                return Unauthorized(new { message = "Invalid user token" });
            }

            var command = new CreateProviderCommand
            {
                UserId = userId,
                BusinessName = request.BusinessName,
                BusinessEmail = request.BusinessEmail,
                BusinessPhone = request.BusinessPhone,
                Description = request.Description,
                Website = request.Website,
                LogoUrl = request.LogoUrl,
                LocationId = request.LocationId,
                AddressLine1 = request.AddressLine1,
                AddressLine2 = request.AddressLine2,
                City = request.City,
                StateProvince = request.StateProvince,
                PostalCode = request.PostalCode,
                Country = request.Country,
                RegistrationNumber = request.RegistrationNumber,
                TaxId = request.TaxId,
                RegistrationDate = request.RegistrationDate,
                PaymentMethod = request.PaymentMethod,
                BankAccountNumber = request.BankAccountNumber,
                BankName = request.BankName
            };

            var result = await _mediator.Send(command);

            if (result.IsFailure)
            {
                return BadRequest(new { message = result.Message });
            }

            return CreatedAtAction(
                nameof(GetProviderById),
                new { id = result.Data },
                new
                {
                    success = true,
                    data = new { providerId = result.Data },
                    message = "Provider created successfully"
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating provider");
            return StatusCode(500, new { message = "An error occurred while creating provider" });
        }
    }

    /// <summary>
    /// Update an existing Activity Provider profile
    /// </summary>
    /// <param name="id">Provider ID</param>
    /// <param name="request">Updated provider information</param>
    /// <returns>Success status</returns>
    /// <remarks>
    /// Only ActivityProvider role can update their provider profile
    /// </remarks>
    [HttpPut("{id}")]
    [Authorize(Roles = "ActivityProvider")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(typeof(object), 400)]
    [ProducesResponseType(typeof(object), 401)]
    [ProducesResponseType(typeof(object), 403)]
    [ProducesResponseType(typeof(object), 404)]
    [ProducesResponseType(typeof(object), 500)]
    public async Task<IActionResult> UpdateProvider(Guid id, [FromBody] UpdateProviderRequest request)
    {
        try
        {
            var userId = GetUserIdFromClaims();
            if (userId == Guid.Empty)
            {
                return Unauthorized(new { message = "Invalid user token" });
            }

            var command = new UpdateProviderCommand
            {
                ProviderId = id,
                UserId = userId,
                BusinessName = request.BusinessName,
                BusinessEmail = request.BusinessEmail,
                BusinessPhone = request.BusinessPhone,
                Description = request.Description,
                Website = request.Website,
                LogoUrl = request.LogoUrl,
                LocationId = request.LocationId,
                AddressLine1 = request.AddressLine1,
                AddressLine2 = request.AddressLine2,
                City = request.City,
                StateProvince = request.StateProvince,
                PostalCode = request.PostalCode,
                Country = request.Country,
                RegistrationNumber = request.RegistrationNumber,
                TaxId = request.TaxId,
                RegistrationDate = request.RegistrationDate,
                PaymentMethod = request.PaymentMethod,
                BankAccountNumber = request.BankAccountNumber,
                BankName = request.BankName
            };

            var result = await _mediator.Send(command);

            if (result.IsFailure)
            {
                if (result.Message?.Contains("not authorized") == true)
                {
                    return StatusCode(403, new { message = result.Message });
                }
                if (result.Message?.Contains("not found") == true)
                {
                    return NotFound(new { message = result.Message });
                }
                return BadRequest(new { message = result.Message });
            }

            return Ok(new
            {
                success = true,
                message = "Provider updated successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating provider: {ProviderId}", id);
            return StatusCode(500, new { message = "An error occurred while updating provider" });
        }
    }

    /// <summary>
    /// Helper method to extract user ID from JWT claims
    /// </summary>
    private Guid GetUserIdFromClaims()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }
}

/// <summary>
/// Request model for creating a provider
/// </summary>
public class CreateProviderRequest
{
    public string BusinessName { get; set; } = string.Empty;
    public string? BusinessEmail { get; set; }
    public string? BusinessPhone { get; set; }
    public string? Description { get; set; }
    public string? Website { get; set; }
    public string? LogoUrl { get; set; }
    public Guid? LocationId { get; set; }
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? StateProvince { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
    public string? RegistrationNumber { get; set; }
    public string? TaxId { get; set; }
    public DateTime? RegistrationDate { get; set; }
    public string? PaymentMethod { get; set; }
    public string? BankAccountNumber { get; set; }
    public string? BankName { get; set; }
}

/// <summary>
/// Request model for updating a provider
/// </summary>
public class UpdateProviderRequest
{
    public string BusinessName { get; set; } = string.Empty;
    public string? BusinessEmail { get; set; }
    public string? BusinessPhone { get; set; }
    public string? Description { get; set; }
    public string? Website { get; set; }
    public string? LogoUrl { get; set; }
    public Guid? LocationId { get; set; }
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? StateProvince { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
    public string? RegistrationNumber { get; set; }
    public string? TaxId { get; set; }
    public DateTime? RegistrationDate { get; set; }
    public string? PaymentMethod { get; set; }
    public string? BankAccountNumber { get; set; }
    public string? BankName { get; set; }
}
