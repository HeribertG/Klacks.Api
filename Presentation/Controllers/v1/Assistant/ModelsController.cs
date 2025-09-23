using Klacks.Api.Application.Commands.LLM;
using Klacks.Api.Application.Queries.LLM;
using Klacks.Api.Domain.Models.LLM;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Klacks.Api.Presentation.Controllers.v1.Assistant;

[ApiController]
[Route("api/v1/backend/assistant/models")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
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
                modelId = m.ModelId,
                providerId = m.Provider.ProviderId,
                displayName = m.ModelName,
                description = m.Description ?? $"{m.Provider.ProviderName} {m.ModelName}",
                contextWindow = m.ContextWindow,
                maxOutputTokens = m.MaxTokens,
                costPerInputToken = m.CostPerInputToken,
                costPerOutputToken = m.CostPerOutputToken,
                isEnabled = m.IsEnabled,
                isDefault = m.IsDefault,
                capabilities = GetModelCapabilities(m)
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
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
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
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
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
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
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



    private string[] GetModelCapabilities(LLMModel model)
    {
        var capabilities = new List<string> { "chat" };
        
        if (model.Provider.ProviderId == "openai" || 
            (model.Provider.ProviderId == "anthropic" && !model.ModelId.Contains("claude-instant")) ||
            (model.Provider.ProviderId == "google" && model.ModelId.Contains("gemini")))
        {
            capabilities.Add("function_calling");
        }
        
        if ((model.Provider.ProviderId == "openai" && model.ModelId.Contains("gpt-4")) ||
            (model.Provider.ProviderId == "anthropic" && model.ModelId.Contains("claude-3")) ||
            (model.Provider.ProviderId == "google" && model.ModelId.Contains("gemini")))
        {
            capabilities.Add("vision");
        }
        
        return capabilities.ToArray();
    }
}