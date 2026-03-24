// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Commands.Assistant;
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.DTOs.Assistant;
using Klacks.Api.Application.Services.Assistant;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace Klacks.Api.Presentation.Controllers.Assistant;

[ApiController]
[Route("api/backend/assistant/chat")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class ChatController : ControllerBase
{
    private readonly ILogger<ChatController> _logger;
    private readonly IMediator _mediator;
    private readonly IAgentSkillRepository _agentSkillRepository;
    private readonly IAgentRepository _agentRepository;
    private readonly ILLMStreamingOrchestrator _streamingOrchestrator;

    public ChatController(
        ILogger<ChatController> logger,
        IMediator mediator,
        IAgentSkillRepository agentSkillRepository,
        IAgentRepository agentRepository,
        ILLMStreamingOrchestrator streamingOrchestrator)
    {
        this._logger = logger;
        _mediator = mediator;
        _agentSkillRepository = agentSkillRepository;
        _agentRepository = agentRepository;
        _streamingOrchestrator = streamingOrchestrator;
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

    [HttpPost("stream")]
    public async Task ProcessMessageStream([FromBody] LLMRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            Response.StatusCode = 400;
            return;
        }

        var userId = GetCurrentUserId();
        var userRights = GetCurrentUserRights();

        _logger.LogInformation("Processing streaming assistant request for user {UserId}", userId);

        Response.ContentType = "text/event-stream";
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");
        Response.Headers.Append("X-Accel-Buffering", "no");

        await Response.StartAsync(cancellationToken);

        var streamRequest = new LLMStreamRequest
        {
            Message = request.Message,
            UserId = userId,
            ConversationId = request.ConversationId,
            ModelId = request.ModelId,
            Language = request.Language,
            UserRights = userRights
        };

        var jsonOptions = new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        try
        {
            await foreach (var chunk in _streamingOrchestrator.ProcessStreamAsync(streamRequest, cancellationToken))
            {
                var eventName = chunk.Type switch
                {
                    SseChunkType.StreamStart => "stream_start",
                    SseChunkType.Content => "content",
                    SseChunkType.FunctionCall => "function_call",
                    SseChunkType.FunctionResult => "function_result",
                    SseChunkType.Metadata => "metadata",
                    SseChunkType.Done => "done",
                    SseChunkType.Error => "error",
                    _ => "unknown"
                };

                var data = System.Text.Json.JsonSerializer.Serialize(chunk, jsonOptions);
                await Response.WriteAsync($"event: {eventName}\ndata: {data}\n\n", cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during SSE streaming for user {UserId}", userId);
            var errorChunk = SseChunk.Error(ex.Message);
            var errorData = System.Text.Json.JsonSerializer.Serialize(errorChunk, jsonOptions);
            await Response.WriteAsync($"event: error\ndata: {errorData}\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }
    }

    [HttpGet("functions")]
    public async Task<ActionResult<object>> GetAvailableFunctions()
    {
        var userRights = GetCurrentUserRights();
        var agent = await _agentRepository.GetDefaultAgentAsync();

        if (agent == null)
            return Ok(Array.Empty<object>());

        var skills = await _agentSkillRepository.GetEnabledAsync(agent.Id);

        var filtered = skills
            .Where(s => s.RequiredPermission == null ||
                        userRights.Contains(s.RequiredPermission) ||
                        userRights.Contains(Roles.Admin))
            .Select(s => new { s.Name, s.Description, s.ExecutionType, s.Category })
            .ToList();

        return Ok(filtered);
    }

    [HttpGet("function-definitions")]
    public async Task<ActionResult<object>> GetFunctionDefinitions()
    {
        var agent = await _agentRepository.GetDefaultAgentAsync();

        if (agent == null)
            return Ok(Array.Empty<object>());

        var skills = await _agentSkillRepository.GetEnabledAsync(agent.Id);

        var result = skills.Select(s => new
        {
            s.Id,
            s.Name,
            s.Description,
            s.ParametersJson,
            s.RequiredPermission,
            s.ExecutionType,
            s.Category,
            s.IsEnabled,
            s.SortOrder
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

    [HttpPost("execute-functions-batch")]
    public async Task<ActionResult<List<LLMFunctionResult>>> ExecuteFunctionsBatch([FromBody] List<LLMFunctionExecuteRequest> requests)
    {
        if (requests == null || !requests.Any())
            return BadRequest("At least one function is required");

        var userId = GetCurrentUserId();
        var userRights = GetCurrentUserRights();
        var results = new List<LLMFunctionResult>();

        foreach (var request in requests)
        {
            if (string.IsNullOrWhiteSpace(request.FunctionName))
            {
                results.Add(new LLMFunctionResult { Success = false, Error = "Function name cannot be empty" });
                continue;
            }

            _logger.LogInformation("Batch executing function {FunctionName} for user {UserId}", request.FunctionName, userId);

            var response = await _mediator.Send(new ExecuteLLMFunctionCommand
            {
                FunctionName = request.FunctionName,
                Parameters = request.Parameters,
                UserId = userId,
                UserRights = userRights
            });

            results.Add(response);
        }

        return Ok(results);
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

        return rights;
    }
}
