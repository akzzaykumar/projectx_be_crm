using Microsoft.AspNetCore.Mvc;

namespace ActivoosCRM.API.Controllers;

/// <summary>
/// Health check controller
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    /// <summary>
    /// Health check endpoint
    /// </summary>
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            status = "Healthy",
            timestamp = DateTime.UtcNow,
            service = "ActivoosCRM API",
            version = "1.0.0"
        });
    }
}
