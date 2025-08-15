using Klacks.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
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