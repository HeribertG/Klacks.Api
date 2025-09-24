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
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { Message = "Invalid or missing user authentication" });
        }

        var usage = await _mediator.Send(new GetLLMUsageQuery 
        { 
            UserId = userId, 
            Days = days 
        });

        _logger.LogInformation("User {UserId} retrieved usage statistics for {Days} days", userId, days);
        
        return Ok(usage);
    }

    [HttpGet("export")]
    public ActionResult ExportUsageData([FromQuery] string format = "csv", [FromQuery] int days = 30)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { Message = "Invalid or missing user authentication" });
        }
        
        if (days <= 0 || days > 365)
        {
            return BadRequest(new { Message = "Days must be between 1 and 365" });
        }
        
        var validFormats = new[] { "csv", "json", "xlsx" };
        if (!validFormats.Contains(format.ToLower()))
        {
            return BadRequest(new { Message = $"Unsupported format. Valid formats: {string.Join(", ", validFormats)}" });
        }
        
        _logger.LogInformation("User {UserId} requested export in {Format} format for {Days} days", userId, format, days);
        
        return StatusCode(501, new { Message = "Export functionality not yet implemented" });
    }
}