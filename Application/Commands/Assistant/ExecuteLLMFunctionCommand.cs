// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Providers;

namespace Klacks.Api.Application.Commands.Assistant;

public class ExecuteLLMFunctionCommand : IRequest<LLMFunctionResult>
{
    public string FunctionName { get; set; } = string.Empty;
    public Dictionary<string, object>? Parameters { get; set; }
    public string UserId { get; set; } = string.Empty;
    public List<string> UserRights { get; set; } = new();
}

public class ExecuteLLMFunctionCommandHandler : IRequestHandler<ExecuteLLMFunctionCommand, LLMFunctionResult>
{
    private readonly ILogger<ExecuteLLMFunctionCommandHandler> _logger;
    private readonly Klacks.Api.Domain.Services.Assistant.Skills.ILLMSkillBridge? _skillBridge;

    private static readonly Dictionary<string, string> FunctionNameAliases = new()
    {
        { "get_user_context", "get_user_permissions" },
        { "get_current_user", "get_user_permissions" },
        { "getUserPermissions", "get_user_permissions" },
        { "create_client", "create_employee" },
        { "search_clients", "search_employees" },
        { "navigate_to_page", "navigate_to" }
    };

    public ExecuteLLMFunctionCommandHandler(
        ILogger<ExecuteLLMFunctionCommandHandler> logger,
        Klacks.Api.Domain.Services.Assistant.Skills.ILLMSkillBridge? skillBridge = null)
    {
        _logger = logger;
        _skillBridge = skillBridge;
    }

    public async Task<LLMFunctionResult> Handle(ExecuteLLMFunctionCommand request, CancellationToken cancellationToken)
    {
        if (FunctionNameAliases.TryGetValue(request.FunctionName, out var normalizedName))
        {
            _logger.LogInformation("Normalizing function name {Original} to {Normalized}", request.FunctionName, normalizedName);
            request.FunctionName = normalizedName;
        }

        _logger.LogInformation("Executing function {FunctionName}", request.FunctionName);

        try
        {
            return await ExecuteSkillAsync(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing function {FunctionName}", request.FunctionName);
            return new LLMFunctionResult
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    private async Task<LLMFunctionResult> ExecuteSkillAsync(ExecuteLLMFunctionCommand request)
    {
        if (_skillBridge == null)
        {
            return new LLMFunctionResult
            {
                Success = false,
                Error = $"Unknown function: {request.FunctionName}"
            };
        }

        var context = new SkillExecutionContext
        {
            UserId = Guid.TryParse(request.UserId, out var uid) ? uid : Guid.Empty,
            TenantId = Guid.Empty,
            UserName = request.UserId,
            UserPermissions = request.UserRights
        };

        var functionCall = new LLMFunctionCall
        {
            FunctionName = request.FunctionName,
            Parameters = request.Parameters ?? new()
        };

        var result = await _skillBridge.ExecuteSkillFromLLMCallAsync(functionCall, context);

        return new LLMFunctionResult
        {
            Success = result.Success,
            Message = result.Message,
            Result = result.Data,
            Error = result.Success ? null : result.Message
        };
    }
}
