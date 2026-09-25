// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Executes an MCP tool call by dispatching it through the existing skill execution pipeline
/// (permission check, parameter validation, autonomy gate, audit) under the calling user's identity.
/// Skills that mutate state reach the REST API with a freshly minted token rather than the caller's own
/// credential, because this channel also accepts personal access tokens which those endpoints reject.
/// The minted token is capped at Authorised, matching the ceiling the rest of the MCP surface applies.
/// Results carrying externally authored content (skills in UntrustedSkillOutputs or tainted results relayed
/// by a wrapper skill) get the untrusted-content notice in front of their message AND their serialized data
/// (the external bodies, e.g. an e-mail text, live in the data), delimiter-escaped and capped like a tool
/// result of the chat loop, so the MCP client's model treats them as data. Such results carry no structured
/// content: it would hand the same bodies to the client raw, and no tool declares an output schema that
/// would make structured content mandatory. Trusted results keep the full response as structured content.
/// </summary>
/// <param name="request">MCP call parameters containing the tool name and JSON arguments</param>
/// <param name="user">Claims principal of the authenticated caller; actions run with this user's permissions</param>

using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Klacks.Api.Application.Commands.Assistant;
using Klacks.Api.Application.DTOs.Assistant;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Services.Assistant;
using Klacks.Api.Infrastructure.Mediator;
using ModelContextProtocol.Protocol;

namespace Klacks.Api.Presentation.Mcp;

public class McpSkillCallHandler : IMcpSkillCallHandler
{
    private static readonly JsonSerializerOptions ResultSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReferenceHandler = ReferenceHandler.IgnoreCycles
    };

    private static readonly JsonSerializerOptions UntrustedDataSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly IMediator _mediator;
    private readonly ISkillRegistry _skillRegistry;
    private readonly IMcpSkillExposurePolicy _exposurePolicy;
    private readonly IInternalTokenIssuer _internalTokenIssuer;
    private readonly ILogger<McpSkillCallHandler> _logger;

    public McpSkillCallHandler(
        IMediator mediator,
        ISkillRegistry skillRegistry,
        IMcpSkillExposurePolicy exposurePolicy,
        IInternalTokenIssuer internalTokenIssuer,
        ILogger<McpSkillCallHandler> logger)
    {
        _mediator = mediator;
        _skillRegistry = skillRegistry;
        _exposurePolicy = exposurePolicy;
        _internalTokenIssuer = internalTokenIssuer;
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

        // The caller's own credential is deliberately not forwarded: MCP also authenticates via
        // personal access tokens, which the JWT-pinned REST controllers reject. Instead a short-lived
        // JWT is minted for the caller, capped at Authorised — the same ceiling McpPrincipalCapper and
        // McpUserContextReader already apply, so an Admin's token cannot reach Admin-only endpoints
        // through this channel either.
        var token = await _internalTokenIssuer.IssueForOwnerAsync(
            userContext.UserId, Roles.Authorised, cancellationToken);
        if (!token.Success)
        {
            return ErrorResult(token.Reason!);
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
            userContext.Permissions,
            token.Token);

        try
        {
            var response = await _mediator.Send(command, cancellationToken);
            return ToCallToolResult(request.Name, response);
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

    private static CallToolResult ToCallToolResult(string skillName, SkillExecuteResponse response)
    {
        var isConfirmation = response.ResultType == SkillResultType.Confirmation;
        var isUntrusted = response.ContainsExternalContent || UntrustedSkillOutputs.Contains(skillName);
        var text = isConfirmation
            ? BuildConfirmationText(response, isUntrusted)
            : BuildResultText(response);

        return new CallToolResult
        {
            IsError = !response.Success && !isConfirmation,
            Content = [new TextContentBlock { Text = isUntrusted && !isConfirmation ? FrameUntrusted(text, response.Data) : text }],
            StructuredContent = isUntrusted
                ? null
                : JsonSerializer.SerializeToElement(response, ResultSerializerOptions)
        };
    }

    private static string BuildConfirmationText(SkillExecuteResponse response, bool isUntrusted)
    {
        var instruction = $"Confirmation required: call the '{AutonomyDefaults.ConfirmPendingActionSkillName}' tool " +
                          $"with parameter '{AutonomyDefaults.ConfirmationTokenParameter}' set to '{ExtractConfirmationToken(response)}'.";

        if (string.IsNullOrWhiteSpace(response.Message) && (!isUntrusted || response.Data == null))
        {
            return instruction;
        }

        return isUntrusted
            ? FrameUntrusted(response.Message ?? string.Empty, response.Data) + Environment.NewLine + instruction
            : $"{response.Message} {instruction}";
    }

    private static string BuildResultText(SkillExecuteResponse response)
    {
        if (!string.IsNullOrWhiteSpace(response.Message))
        {
            return response.Message;
        }

        return response.Success ? "Tool executed successfully." : "Tool execution failed.";
    }

    private static string FrameUntrusted(string text, object? data)
    {
        var body = data == null
            ? text
            : text + Environment.NewLine + JsonSerializer.Serialize(data, UntrustedDataSerializerOptions);

        return ToolResultMarkers.UntrustedContentNotice + Environment.NewLine
            + ToolResultFormatter.EscapeAndCap(body, LLMLoopConstants.DefaultMaxToolResultChars);
    }

    private static string ExtractConfirmationToken(SkillExecuteResponse response)
    {
        if (response.Metadata != null
            && response.Metadata.TryGetValue(SkillResultMetadataKeys.ConfirmationToken, out var token))
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
