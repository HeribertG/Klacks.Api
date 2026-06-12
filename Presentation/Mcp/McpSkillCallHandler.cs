// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Executes an MCP tool call by dispatching it through the existing skill execution pipeline
/// (permission check, parameter validation, autonomy gate, audit) under the calling user's identity.
/// </summary>
/// <param name="request">MCP call parameters containing the tool name and JSON arguments</param>
/// <param name="user">Claims principal of the authenticated caller; actions run with this user's permissions</param>

using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using Klacks.Api.Application.Commands.Assistant;
using Klacks.Api.Application.DTOs.Assistant;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Infrastructure.Mediator;
using ModelContextProtocol.Protocol;

namespace Klacks.Api.Presentation.Mcp;

public class McpSkillCallHandler : IMcpSkillCallHandler
{
    private const string ConfirmationTokenMetadataKey = "confirmationToken";

    private static readonly JsonSerializerOptions ResultSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReferenceHandler = ReferenceHandler.IgnoreCycles
    };

    private readonly IMediator _mediator;
    private readonly ISkillRegistry _skillRegistry;
    private readonly IMcpSkillExposurePolicy _exposurePolicy;
    private readonly ILogger<McpSkillCallHandler> _logger;

    public McpSkillCallHandler(
        IMediator mediator,
        ISkillRegistry skillRegistry,
        IMcpSkillExposurePolicy exposurePolicy,
        ILogger<McpSkillCallHandler> logger)
    {
        _mediator = mediator;
        _skillRegistry = skillRegistry;
        _exposurePolicy = exposurePolicy;
        _logger = logger;
    }

    public async Task<CallToolResult> HandleAsync(
        CallToolRequestParams request,
        ClaimsPrincipal? user,
        CancellationToken cancellationToken)
    {
        var userContext = McpUserContextReader.Read(user);
        if (userContext.UserId == Guid.Empty)
        {
            return ErrorResult("Authentication required.");
        }

        var descriptor = _skillRegistry.GetSkillByName(request.Name);
        if (descriptor == null || !_exposurePolicy.IsExposed(descriptor))
        {
            return ErrorResult($"Tool '{request.Name}' is not available.");
        }

        var command = new ExecuteSkillCommand(
            new SkillExecuteRequest
            {
                SkillName = request.Name,
                Parameters = ConvertArguments(request.Arguments)
            },
            userContext.UserId,
            userContext.TenantId,
            userContext.UserName,
            userContext.Permissions);

        try
        {
            var response = await _mediator.Send(command, cancellationToken);
            return ToCallToolResult(response);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MCP tool call failed for skill {SkillName} and user {UserId}",
                request.Name, userContext.UserId);
            return ErrorResult($"Tool '{request.Name}' execution failed. See server logs for details.");
        }
    }

    private static Dictionary<string, object> ConvertArguments(IDictionary<string, JsonElement>? arguments)
    {
        var parameters = new Dictionary<string, object>();
        if (arguments == null)
        {
            return parameters;
        }

        foreach (var (key, value) in arguments)
        {
            parameters[key] = value;
        }

        return parameters;
    }

    private static CallToolResult ToCallToolResult(SkillExecuteResponse response)
    {
        var isConfirmation = response.ResultType == SkillResultType.Confirmation;

        return new CallToolResult
        {
            IsError = !response.Success && !isConfirmation,
            Content = [new TextContentBlock { Text = BuildResultText(response, isConfirmation) }],
            StructuredContent = JsonSerializer.SerializeToElement(response, ResultSerializerOptions)
        };
    }

    private static string BuildResultText(SkillExecuteResponse response, bool isConfirmation)
    {
        if (isConfirmation)
        {
            var prefix = string.IsNullOrWhiteSpace(response.Message) ? string.Empty : $"{response.Message} ";
            return $"{prefix}Confirmation required: call the '{AutonomyDefaults.ConfirmPendingActionSkillName}' tool " +
                   $"with parameter '{AutonomyDefaults.ConfirmationTokenParameter}' set to '{ExtractConfirmationToken(response)}'.";
        }

        if (!string.IsNullOrWhiteSpace(response.Message))
        {
            return response.Message;
        }

        return response.Success ? "Tool executed successfully." : "Tool execution failed.";
    }

    private static string ExtractConfirmationToken(SkillExecuteResponse response)
    {
        if (response.Metadata != null
            && response.Metadata.TryGetValue(ConfirmationTokenMetadataKey, out var token))
        {
            return token?.ToString() ?? string.Empty;
        }

        return string.Empty;
    }

    private static CallToolResult ErrorResult(string message)
    {
        return new CallToolResult
        {
            IsError = true,
            Content = [new TextContentBlock { Text = message }]
        };
    }
}
