using Klacks.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.Internal;

/// <summary>
/// Internal administrative controller for database initialization and seeding operations.
/// This controller provides endpoints for setting up the database with initial data
/// and is intended for use during development, testing, or initial deployment scenarios.
/// 
/// IMPORTANT: This controller is restricted to Admin role users only for security reasons.
/// In production environments, consider disabling these endpoints or implementing
/// additional security measures such as:
/// - IP whitelisting
/// - Environment-based availability (e.g., only in Development/Staging)
/// - One-time initialization tokens
/// 
/// Use cases:
/// - Initial database setup during deployment
/// - Resetting test databases
/// - Populating demo environments with sample data
/// </summary>
[ApiController]
[Route("api/internal/[controller]")]
[Authorize(Roles = "Admin")]
public class DatabaseController : ControllerBase
{
    private readonly IDatabaseInitializer _databaseInitializer;
    private readonly ILogger<DatabaseController> _logger;

    public DatabaseController(IDatabaseInitializer databaseInitializer, ILogger<DatabaseController> logger)
    {
        _databaseInitializer = databaseInitializer;
        _logger = logger;
    }

    /// <summary>
    /// Initializes the database and inserts seed data
    /// </summary>
    /// <returns>Initialization status</returns>
    [HttpPost("initialize")]
    public async Task<IActionResult> InitializeDatabase()
    {
        try
        {
            await _databaseInitializer.InitializeAsync();
            return Ok(new { message = "Database successfully initialized." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing database");
            return StatusCode(500, new { error = "Database initialization failed", details = ex.Message });
        }
    }

    /// <summary>
    /// Inserts seed data only (without migrations)
    /// </summary>
    /// <returns>Seeding status</returns>
    [HttpPost("seed")]
    public async Task<IActionResult> SeedDatabase()
    {
        try
        {
            await _databaseInitializer.SeedDataAsync();
            return Ok(new { message = "Seed data successfully inserted." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding database");
            return StatusCode(500, new { error = "Database seeding failed", details = ex.Message });
        }
    }
}