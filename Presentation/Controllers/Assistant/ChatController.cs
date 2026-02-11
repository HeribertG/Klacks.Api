using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.Commands.LLM;
using Klacks.Api.Application.DTOs.LLM;
using Klacks.Api.Domain.Interfaces.AI;
using Klacks.Api.Domain.Constants;
using System.Security.Claims;

namespace Klacks.Api.Presentation.Controllers.Assistant;

[ApiController]
[Route("api/backend/assistant/chat")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class ChatController : ControllerBase
{
    private readonly ILogger<ChatController> _logger;
    private readonly IMediator _mediator;
    private readonly ILlmFunctionDefinitionRepository _functionDefinitionRepository;

    public ChatController(
        ILogger<ChatController> logger,
        IMediator mediator,
        ILlmFunctionDefinitionRepository functionDefinitionRepository)
    {
        this._logger = logger;
        _mediator = mediator;
        _functionDefinitionRepository = functionDefinitionRepository;
    }

    [HttpPost]
    public async Task<ActionResult<LLMResponse>> ProcessMessage([FromBody] LLMRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest("Message cannot be empty");
        }

        var userId = GetCurrentUserId();
        var userRights = GetCurrentUserRights();

        _logger.LogInformation("Processing assistant request for user {UserId}: {Message}", userId, request.Message);

        var response = await _mediator.Send(new ProcessLLMMessageCommand
        {
            Message = request.Message,
            UserId = userId,
            ConversationId = request.ConversationId,
            ModelId = request.ModelId,
            Language = request.Language,
            UserRights = userRights
        });

        response.ConversationId = request.ConversationId ?? Guid.NewGuid().ToString();

        return Ok(response);
    }

    [HttpGet("functions")]
    public async Task<ActionResult<object>> GetAvailableFunctions()
    {
        var userRights = GetCurrentUserRights();
        var definitions = await _functionDefinitionRepository.GetAllEnabledAsync();

        var filtered = definitions
            .Where(d => d.RequiredPermission == null ||
                        userRights.Contains(d.RequiredPermission) ||
                        userRights.Contains("Admin"))
            .Select(d => new { d.Name, d.Description, d.ExecutionType, d.Category })
            .ToList();

        return Ok(filtered);
    }

    [HttpGet("function-definitions")]
    public async Task<ActionResult<object>> GetFunctionDefinitions()
    {
        var definitions = await _functionDefinitionRepository.GetAllEnabledAsync();

        var result = definitions.Select(d => new
        {
            d.Id,
            d.Name,
            d.Description,
            d.ParametersJson,
            d.RequiredPermission,
            d.ExecutionType,
            d.Category,
            d.IsEnabled,
            d.SortOrder
        });

        return Ok(result);
    }

    [HttpPost("execute-function")]
    public async Task<ActionResult<LLMFunctionResult>> ExecuteFunction([FromBody] LLMFunctionExecuteRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FunctionName))
        {
            return BadRequest("Function name cannot be empty");
        }

        var userId = GetCurrentUserId();
        var userRights = GetCurrentUserRights();
        _logger.LogInformation("Executing function {FunctionName} for user {UserId}", request.FunctionName, userId);

        var response = await _mediator.Send(new ExecuteLLMFunctionCommand
        {
            FunctionName = request.FunctionName,
            Parameters = request.Parameters,
            UserId = userId,
            UserRights = userRights
        });

        return Ok(response);
    }

    [HttpGet("help")]
    public ActionResult<object> GetHelp()
    {
        return Ok(new
        {
            SupportedCommands = new[]
            {
                "Create employee [FirstName] [LastName] from [Canton]",
                "Search for [Term]",
                "Show persons from [Canton]",
                "Create contract [Type] for [Canton]"
            },
            SupportedCantons = new[] { "BE", "ZH", "SG", "VD" },
            ContractTypes = new[] { "Vollzeit 160", "Vollzeit 180", "Teilzeit 0 Std" },
            Examples = new[]
            {
                "Create employee Hans Muster from Zurich",
                "Search for Mueller",
                "Show all persons from Bern",
                "Create Vollzeit 160 contract for Zurich"
            }
        });
    }

    private string GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim != null)
        {
            return userIdClaim.Value;
        }

        return string.Empty;
    }

    private List<string> GetCurrentUserRights()
    {
        var rights = new List<string>();

        var roleClaims = User.FindAll(ClaimTypes.Role);
        foreach (var claim in roleClaims)
        {
            rights.Add(claim.Value);
            var permissions = Permissions.GetPermissionsForRole(claim.Value);
            foreach (var permission in permissions)
            {
                if (!rights.Contains(permission))
                {
                    rights.Add(permission);
                }
            }
        }

        if (!rights.Any())
        {
            rights.AddRange(Permissions.GetPermissionsForRole(Roles.Admin));
            rights.Add(Roles.Admin);
        }

        return rights;
    }
}
