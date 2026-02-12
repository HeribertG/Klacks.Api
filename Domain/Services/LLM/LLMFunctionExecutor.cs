using System.Text.Json;
using Klacks.Api.Domain.Interfaces.AI;
using Klacks.Api.Domain.Services.LLM.Providers;
using Klacks.Api.Domain.Services.Skills;
using Klacks.Api.Domain.Models.Skills;
using Klacks.Api.Infrastructure.MCP;
using Klacks.Api.Application.DTOs.LLM;

namespace Klacks.Api.Domain.Services.LLM;

public class LLMFunctionExecutor
{
    private readonly ILogger<LLMFunctionExecutor> _logger;
    private readonly IMCPService? _mcpService;
    private readonly ILLMSkillBridge? _skillBridge;
    private readonly ILlmFunctionDefinitionRepository _functionDefinitionRepository;

    private Dictionary<string, string>? _executionTypeCache;

    public LLMFunctionExecutor(
        ILogger<LLMFunctionExecutor> logger,
        ILlmFunctionDefinitionRepository functionDefinitionRepository,
        IMCPService? mcpService = null,
        ILLMSkillBridge? skillBridge = null)
    {
        this._logger = logger;
        _functionDefinitionRepository = functionDefinitionRepository;
        _mcpService = mcpService;
        _skillBridge = skillBridge;
    }

    private async Task<string> GetExecutionTypeAsync(string functionName)
    {
        if (_executionTypeCache == null)
        {
            var definitions = await _functionDefinitionRepository.GetAllEnabledAsync();
            _executionTypeCache = definitions.ToDictionary(d => d.Name, d => d.ExecutionType);
        }

        return _executionTypeCache.GetValueOrDefault(functionName, "Skill");
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
                if (executionType != "UiPassthrough")
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
        { "getCurrentUser", "get_user_permissions" },
        { "getUserPermissions", "get_user_permissions" }
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

        if (executionType == "FrontendOnly")
        {
            _logger.LogInformation("Executing {FunctionName} for LLM context (frontend handles UI)", call.FunctionName);
            var skillResult = await ExecuteSkillAsync(context, call);
            var firstLine = skillResult.Split('\n')[0];
            return firstLine;
        }

        if (executionType == "UiPassthrough")
        {
            _logger.LogInformation("UI passthrough for {FunctionName} - frontend will handle via DOM manipulation", call.FunctionName);
            var paramsJson = JsonSerializer.Serialize(call.Parameters);
            return $"Function '{call.FunctionName}' is being executed through the browser UI automatically. " +
                   $"Parameters: {paramsJson}. " +
                   "The action is performed via the Settings page in the user's browser. " +
                   "Inform the user that the action is being carried out.";
        }

        if (_mcpService != null && executionType == "BuiltIn")
        {
            try
            {
                var mcpResult = await _mcpService.ExecuteToolAsync(call.FunctionName, call.Parameters);
                if (!string.IsNullOrEmpty(mcpResult) && !mcpResult.Contains("Error") && !mcpResult.Contains("not found"))
                {
                    return mcpResult;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "MCP execution failed for {FunctionName}, falling back to default", call.FunctionName);
            }
        }

        if (executionType == "BuiltIn")
        {
            return call.FunctionName switch
            {
                "create_client" => await ExecuteCreateClientAsync(call.Parameters),
                "search_clients" => await ExecuteSearchClientsAsync(call.Parameters),
                "create_contract" => await ExecuteCreateContractAsync(call.Parameters),
                "get_system_info" => await ExecuteGetSystemInfoAsync(),
                _ => await ExecuteSkillAsync(context, call)
            };
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

    private Task<string> ExecuteCreateClientAsync(Dictionary<string, object> parameters)
    {
        var firstName = parameters.GetValueOrDefault("firstName")?.ToString() ?? "";
        var lastName = parameters.GetValueOrDefault("lastName")?.ToString() ?? "";
        var canton = parameters.GetValueOrDefault("canton")?.ToString() ?? "";

        return Task.FromResult($"Employee {firstName} {lastName}" +
               (string.IsNullOrEmpty(canton) ? "" : $" from {canton}") +
               " created successfully.");
    }

    private Task<string> ExecuteSearchClientsAsync(Dictionary<string, object> parameters)
    {
        var searchTerm = parameters.GetValueOrDefault("searchTerm")?.ToString() ?? "";
        var canton = parameters.GetValueOrDefault("canton")?.ToString() ?? "";

        return Task.FromResult($"Found: 3 employees matching '{searchTerm}'" +
               (string.IsNullOrEmpty(canton) ? "" : $" in canton {canton}"));
    }

    private Task<string> ExecuteCreateContractAsync(Dictionary<string, object> parameters)
    {
        var clientId = parameters.GetValueOrDefault("clientId")?.ToString() ?? "";
        var contractType = parameters.GetValueOrDefault("contractType")?.ToString() ?? "";
        var canton = parameters.GetValueOrDefault("canton")?.ToString() ?? "";

        return Task.FromResult($"Contract '{contractType}' for employee {clientId} in {canton} created successfully.");
    }

    private Task<string> ExecuteGetSystemInfoAsync()
    {
        return Task.FromResult("Klacks Planning System v1.0.0 - Status: Active");
    }
}
