using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Commands.Assistant;
using Klacks.Api.Application.Queries.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Klacks.Api.Presentation.Controllers.Assistant;

[ApiController]
[Route("api/backend/assistant/models")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class ModelsController : ControllerBase
{
    private readonly ILogger<ModelsController> _logger;
    private readonly IMediator _mediator;

    public ModelsController(ILogger<ModelsController> logger, IMediator mediator)
    {
        this._logger = logger;
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<object>> GetAvailableModels()
    {
        var models = await _mediator.Send(new GetEnabledLLMModelsQuery(OnlyEnabled: false));
        
        var response = models.Select(m => new
        {
            id = m.Id.ToString(),
            modelId = m.ModelId,
            apiModelId = m.ApiModelId,
            providerId = m.ProviderId,
            modelName = m.ModelName,
            description = m.Description ?? $"{m.ProviderId} {m.ModelName}",
            contextWindow = m.ContextWindow,
            maxTokens = m.MaxTokens,
            costPerInputToken = m.CostPerInputToken,
            costPerOutputToken = m.CostPerOutputToken,
            isEnabled = m.IsEnabled,
            isDefault = m.IsDefault,
            capabilities = LLMCapabilityService.GetCapabilities(m).Select(c => c.ToString().ToLower()).ToArray(),
            displayName = m.ModelName,
            maxOutputTokens = m.MaxTokens
        }).ToList();

        return Ok(response);
    }

    [HttpPost("{modelId}/enable")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
    public async Task<ActionResult> EnableModel(string modelId)
    {
        await _mediator.Send(new EnableLLMModelCommand(modelId));
        _logger.LogInformation("Admin {UserId} enabled model {ModelId}", User.FindFirst(ClaimTypes.NameIdentifier)?.Value, modelId);
        return Ok(new { Message = $"Model {modelId} enabled successfully" });
    }

    [HttpPost("{modelId}/disable")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
    public async Task<ActionResult> DisableModel(string modelId)
    {
        await _mediator.Send(new DisableLLMModelCommand(modelId));
        _logger.LogInformation("Admin {UserId} disabled model {ModelId}", User.FindFirst(ClaimTypes.NameIdentifier)?.Value, modelId);
        return Ok(new { Message = $"Model {modelId} disabled successfully" });
    }

    [HttpPost("{modelId}/set-default")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
    public async Task<ActionResult> SetDefaultModel(string modelId)
    {
        await _mediator.Send(new SetDefaultLLMModelCommand(modelId));
        _logger.LogInformation("Admin {UserId} set default model to {ModelId}", User.FindFirst(ClaimTypes.NameIdentifier)?.Value, modelId);
        return Ok(new { Message = $"Model {modelId} set as default" });
    }

    [HttpPost]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
    public async Task<ActionResult<LLMModel>> CreateModel([FromBody] LLMModel model)
    {
        var result = await _mediator.Send(new PostCommand<LLMModel>(model));
        _logger.LogInformation("Admin {UserId} created model {ModelId}", User.FindFirst(ClaimTypes.NameIdentifier)?.Value, model.ModelId);
        return CreatedAtAction(nameof(GetAvailableModels), new { id = result?.Id }, result);
    }

    [HttpPut("{id}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
    public async Task<ActionResult<LLMModel>> UpdateModel(Guid id, [FromBody] LLMModel model)
    {
        model.Id = id;
        var result = await _mediator.Send(new PutCommand<LLMModel>(model));
        _logger.LogInformation("Admin {UserId} updated model {ModelId}", User.FindFirst(ClaimTypes.NameIdentifier)?.Value, model.ModelId);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
    public async Task<ActionResult> DeleteModel(string id)
    {
        if (Guid.TryParse(id, out var guidId))
        {
            await _mediator.Send(new DeleteCommand<LLMModel>(guidId));
            _logger.LogInformation("Admin {UserId} deleted model with ID {ModelId}", User.FindFirst(ClaimTypes.NameIdentifier)?.Value, guidId);
        }
        else
        {
            var models = await _mediator.Send(new GetEnabledLLMModelsQuery(OnlyEnabled: false));
            var model = models.FirstOrDefault(m => m.ModelId == id);
            
            if (model == null)
            {
                return NotFound($"Model with ID {id} not found");
            }
            
            await _mediator.Send(new DeleteCommand<LLMModel>(model.Id));
            _logger.LogInformation("Admin {UserId} deleted model with modelId {ModelId}", User.FindFirst(ClaimTypes.NameIdentifier)?.Value, id);
        }
        
        return NoContent();
    }
}