using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using Klacks.Api.Application.Queries;
using Klacks.Api.Application.Queries.LLM;
using Klacks.Api.Application.Commands.LLM;
using Klacks.Api.Domain.Models.LLM;
using System.Security.Claims;

namespace Klacks.Api.Presentation.Controllers.v1.Assistant;

[ApiController]
[Route("api/v1/assistant/models")]
[Authorize]
public class ModelsController : ControllerBase
{
    private readonly ILogger<ModelsController> _logger;
    private readonly IMediator _mediator;

    public ModelsController(ILogger<ModelsController> logger, IMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<object>> GetAvailableModels()
    {
        try
        {
            var models = await _mediator.Send(new GetEnabledLLMModelsQuery(OnlyEnabled: false));
            
            var response = models.Select(m => new
            {
                Id = m.ModelId,
                Name = m.ModelName,
                Provider = m.Provider.ProviderId,
                IsDefault = m.IsDefault,
                IsEnabled = m.IsEnabled,
                CostPer1kTokens = new
                {
                    Input = m.CostPerInputToken * 1000,
                    Output = m.CostPerOutputToken * 1000
                },
                MaxTokens = m.MaxTokens,
                ContextWindow = m.ContextWindow,
                Category = m.Category
            }).ToList();

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available models");
            return StatusCode(500, new { Message = "Fehler beim Abrufen der Modelle" });
        }
    }

    [HttpPost("{modelId}/enable")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> EnableModel(string modelId)
    {
        try
        {
            await _mediator.Send(new EnableLLMModelCommand(modelId));
            
            _logger.LogInformation("Admin {UserId} enabled model {ModelId}", User.FindFirst(ClaimTypes.NameIdentifier)?.Value, modelId);
            return Ok(new { Message = $"Model {modelId} erfolgreich aktiviert" });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enabling model {ModelId}", modelId);
            return StatusCode(500, new { Message = "Fehler beim Aktivieren des Modells" });
        }
    }

    [HttpPost("{modelId}/disable")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> DisableModel(string modelId)
    {
        try
        {
            await _mediator.Send(new DisableLLMModelCommand(modelId));
            
            _logger.LogInformation("Admin {UserId} disabled model {ModelId}", User.FindFirst(ClaimTypes.NameIdentifier)?.Value, modelId);
            return Ok(new { Message = $"Model {modelId} erfolgreich deaktiviert" });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disabling model {ModelId}", modelId);
            return StatusCode(500, new { Message = "Fehler beim Deaktivieren des Modells" });
        }
    }

    [HttpPost("{modelId}/set-default")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> SetDefaultModel(string modelId)
    {
        try
        {
            await _mediator.Send(new SetDefaultLLMModelCommand(modelId));
            
            _logger.LogInformation("Admin {UserId} set default model to {ModelId}", User.FindFirst(ClaimTypes.NameIdentifier)?.Value, modelId);
            return Ok(new { Message = $"Model {modelId} als Standard gesetzt" });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting default model {ModelId}", modelId);
            return StatusCode(500, new { Message = "Fehler beim Setzen des Standard-Modells" });
        }
    }
}