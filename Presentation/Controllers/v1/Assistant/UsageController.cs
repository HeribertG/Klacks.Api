using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using MediatR;
using Klacks.Api.Application.Queries.LLM;
using System.Security.Claims;

namespace Klacks.Api.Presentation.Controllers.v1.Assistant;

[ApiController]
[Route("api/v1/backend/assistant/usage")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class UsageController : ControllerBase
{
    private readonly ILogger<UsageController> _logger;
    private readonly IMediator _mediator;

    public UsageController(ILogger<UsageController> logger, IMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<object>> GetUsageStatistics([FromQuery] int days = 30)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest("Ungültige Benutzer-ID");
            }

            var usage = await _mediator.Send(new GetLLMUsageQuery 
            { 
                UserId = userId, 
                Days = days 
            });

            _logger.LogInformation("User {UserId} retrieved usage statistics for {Days} days", userId, days);
            
            return Ok(usage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting usage statistics");
            return StatusCode(500, new { Message = "Fehler beim Abrufen der Nutzungsstatistiken" });
        }
    }

    [HttpGet("conversations")]
    public ActionResult<object> GetConversationHistory([FromQuery] int limit = 10, [FromQuery] int offset = 0)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        // TODO: Echte Daten aus der Datenbank holen
        var conversations = new
        {
            Total = 42,
            Limit = limit,
            Offset = offset,
            Conversations = new[]
            {
                new
                {
                    Id = Guid.NewGuid().ToString(),
                    Title = "Mitarbeiter Hans Muster erstellt",
                    ModelId = "gpt-35-turbo",
                    MessageCount = 3,
                    TokensUsed = 1250,
                    CreatedAt = DateTime.UtcNow.AddHours(-2),
                    UpdatedAt = DateTime.UtcNow.AddHours(-2)
                },
                new
                {
                    Id = Guid.NewGuid().ToString(),
                    Title = "Suche nach Personen aus Bern",
                    ModelId = "gpt-4",
                    MessageCount = 5,
                    TokensUsed = 2340,
                    CreatedAt = DateTime.UtcNow.AddDays(-1),
                    UpdatedAt = DateTime.UtcNow.AddDays(-1)
                }
            }
        };

        return Ok(conversations);
    }

    [HttpGet("export")]
    public ActionResult ExportUsageData([FromQuery] string format = "csv", [FromQuery] int days = 30)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        _logger.LogInformation("User {UserId} exported usage data as {Format} for {Days} days", userId, format, days);
        
        // TODO: Implementiere Export-Funktionalität
        if (format.ToLower() == "csv")
        {
            var csvContent = "Date,Model,Tokens,Cost,Requests\n";
            csvContent += "2024-01-15,GPT-3.5 Turbo,5430,0.08,12\n";
            csvContent += "2024-01-15,GPT-4,1200,0.19,3\n";
            
            return File(System.Text.Encoding.UTF8.GetBytes(csvContent), "text/csv", $"assistant_usage_{DateTime.Now:yyyy-MM-dd}.csv");
        }
        
        return BadRequest($"Unsupported format: {format}");
    }

    private object[] GenerateDailyUsage(int days)
    {
        var dailyUsage = new List<object>();
        var random = new Random();
        
        for (int i = days - 1; i >= 0; i--)
        {
            dailyUsage.Add(new
            {
                Date = DateTime.UtcNow.AddDays(-i).ToString("yyyy-MM-dd"),
                Tokens = random.Next(1000, 10000),
                Cost = Math.Round(random.NextDouble() * 0.5, 2),
                Requests = random.Next(5, 50)
            });
        }
        
        return dailyUsage.ToArray();
    }
}