using System.Text.Json;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Services.Assistant.Providers;
using Klacks.Api.Domain.Services.Assistant.Skills;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Application.DTOs.Assistant;

namespace Klacks.Api.Domain.Services.Assistant;

public class LLMFunctionExecutor
{
    private readonly ILogger<LLMFunctionExecutor> _logger;
    private readonly ILLMSkillBridge? _skillBridge;
    private readonly IAgentSkillRepository _agentSkillRepository;
    private readonly IAgentRepository _agentRepository;

    private Dictionary<string, string>? _executionTypeCache;

    public LLMFunctionExecutor(
        ILogger<LLMFunctionExecutor> logger,
        IAgentSkillRepository agentSkillRepository,
        IAgentRepository agentRepository,
        ILLMSkillBridge? skillBridge = null)
    {
        this._logger = logger;
        _agentSkillRepository = agentSkillRepository;
        _agentRepository = agentRepository;
        _skillBridge = skillBridge;
    }

    private async Task<string> GetExecutionTypeAsync(string functionName)
    {
        if (_executionTypeCache == null)
        {
            var agent = await _agentRepository.GetDefaultAgentAsync();
            if (agent != null)
            {
                var skills = await _agentSkillRepository.GetEnabledAsync(agent.Id);
                _executionTypeCache = skills.ToDictionary(d => d.Name, d => d.ExecutionType);
            }
            else
            {
                _executionTypeCache = new Dictionary<string, string>();
            }
        }

        return _executionTypeCache.GetValueOrDefault(functionName, LlmExecutionTypes.Skill);
    }

    public bool HasOnlyUiPassthroughCalls { get; private set; }

    public async Task<string> ProcessFunctionCallsAsync(LLMContext context, List<LLMFunctionCall> functionCalls)
    {
        var results = new List<string>();
        var allUiPassthrough = true;

        foreach (var call in functionCalls)
        {
            try
            {
                var executionType = await GetExecutionTypeAsync(call.FunctionName);
                if (executionType != LlmExecutionTypes.UiPassthrough)
                    allUiPassthrough = false;

                var result = await ExecuteFunctionAsync(context, call);
                call.Result = result;
                if (!string.IsNullOrEmpty(result))
                {
                    results.Add(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing function {FunctionName}", call.FunctionName);
                results.Add($"Error executing {call.FunctionName}: {ex.Message}");
                allUiPassthrough = false;
            }
        }

        HasOnlyUiPassthroughCalls = allUiPassthrough;
        return string.Join("\n", results);
    }

    private static readonly Dictionary<string, string> FunctionNameAliases = new()
    {
        { "get_user_context", "get_user_permissions" },
        { "get_current_user", "get_user_permissions" },
        { "getUserPermissions", "get_user_permissions" },
        { "create_client", "create_employee" },
        { "search_clients", "search_employees" },
        { "navigate_to_page", "navigate_to" }
    };

    private async Task<string> ExecuteFunctionAsync(LLMContext context, LLMFunctionCall call)
    {
        if (FunctionNameAliases.TryGetValue(call.FunctionName, out var normalizedName))
        {
            _logger.LogInformation("Normalizing function name {Original} to {Normalized}", call.FunctionName, normalizedName);
            call.FunctionName = normalizedName;
        }

        _logger.LogInformation("Executing function {FunctionName} with parameters {Parameters}",
            call.FunctionName, JsonSerializer.Serialize(call.Parameters));

        var executionType = await GetExecutionTypeAsync(call.FunctionName);

        if (executionType == LlmExecutionTypes.FrontendOnly)
        {
            _logger.LogInformation("Executing {FunctionName} for LLM context (frontend handles UI)", call.FunctionName);
            var skillResult = await ExecuteSkillAsync(context, call);
            var firstLine = skillResult.Split('\n')[0];
            return firstLine;
        }

        if (executionType == LlmExecutionTypes.UiPassthrough)
        {
            _logger.LogInformation("UI passthrough for {FunctionName} - frontend will handle via DOM manipulation", call.FunctionName);
            var paramsJson = JsonSerializer.Serialize(call.Parameters);
            return $"Function '{call.FunctionName}' is being executed through the browser UI automatically. " +
                   $"Parameters: {paramsJson}. " +
                   "The action is performed via the Settings page in the user's browser. " +
                   "Inform the user that the action is being carried out.";
        }

        return await ExecuteSkillAsync(context, call);
    }

    private async Task<string> ExecuteSkillAsync(LLMContext context, LLMFunctionCall call)
    {
        if (_skillBridge == null)
        {
            _logger.LogWarning("No skill bridge available to execute {FunctionName}", call.FunctionName);
            return $"Function '{call.FunctionName}' is not available.";
        }

        var skillContext = new SkillExecutionContext
        {
            UserId = Guid.TryParse(context.UserId, out var uid) ? uid : Guid.Empty,
            TenantId = Guid.Empty,
            UserName = context.UserId,
            UserPermissions = context.UserRights
        };

        var skillCall = new Providers.LLMFunctionCall
        {
            FunctionName = call.FunctionName,
            Parameters = call.Parameters
        };

        var result = await _skillBridge.ExecuteSkillFromLLMCallAsync(skillCall, skillContext);

        if (result.Success)
        {
            if (result.Data != null)
            {
                var dataJson = JsonSerializer.Serialize(result.Data, new JsonSerializerOptions { WriteIndented = false });
                return $"{result.Message}\nData: {dataJson}";
            }

            return result.Message;
        }

        return $"Error: {result.Message}";
    }
}
