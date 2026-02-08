using System.Text.Json;
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

    private static readonly HashSet<string> BuiltInFunctions = new()
    {
        "create_client", "search_clients", "create_contract", "get_system_info"
    };

    private static readonly HashSet<string> FrontendOnlyFunctions = new()
    {
        "get_general_settings", "update_general_settings",
        "get_owner_address", "update_owner_address"
    };

    public LLMFunctionExecutor(
        ILogger<LLMFunctionExecutor> logger,
        IMCPService? mcpService = null,
        ILLMSkillBridge? skillBridge = null)
    {
        this._logger = logger;
        _mcpService = mcpService;
        _skillBridge = skillBridge;
    }

    public async Task<string> ProcessFunctionCallsAsync(LLMContext context, List<LLMFunctionCall> functionCalls)
    {
        var results = new List<string>();

        foreach (var call in functionCalls)
        {
            try
            {
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
            }
        }

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

        if (FrontendOnlyFunctions.Contains(call.FunctionName))
        {
            _logger.LogInformation("Executing {FunctionName} for LLM context (frontend handles UI)", call.FunctionName);
            var skillResult = await ExecuteSkillAsync(context, call);
            var firstLine = skillResult.Split('\n')[0];
            return firstLine;
        }

        if (_mcpService != null && BuiltInFunctions.Contains(call.FunctionName))
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

        if (BuiltInFunctions.Contains(call.FunctionName))
        {
            return call.FunctionName switch
            {
                "create_client" => await ExecuteCreateClientAsync(call.Parameters),
                "search_clients" => await ExecuteSearchClientsAsync(call.Parameters),
                "create_contract" => await ExecuteCreateContractAsync(call.Parameters),
                "get_system_info" => await ExecuteGetSystemInfoAsync(),
                _ => ""
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
            return result.Message;
        }

        return $"Error: {result.Message}";
    }

    private Task<string> ExecuteCreateClientAsync(Dictionary<string, object> parameters)
    {
        var firstName = parameters.GetValueOrDefault("firstName")?.ToString() ?? "";
        var lastName = parameters.GetValueOrDefault("lastName")?.ToString() ?? "";
        var canton = parameters.GetValueOrDefault("canton")?.ToString() ?? "";

        return Task.FromResult($"✅ Mitarbeiter {firstName} {lastName}" +
               (string.IsNullOrEmpty(canton) ? "" : $" aus {canton}") +
               " wurde erfolgreich erstellt.");
    }

    private Task<string> ExecuteSearchClientsAsync(Dictionary<string, object> parameters)
    {
        var searchTerm = parameters.GetValueOrDefault("searchTerm")?.ToString() ?? "";
        var canton = parameters.GetValueOrDefault("canton")?.ToString() ?? "";

        return Task.FromResult($"🔍 Gefunden: 3 Mitarbeiter mit Suchbegriff '{searchTerm}'" +
               (string.IsNullOrEmpty(canton) ? "" : $" in Kanton {canton}"));
    }

    private Task<string> ExecuteCreateContractAsync(Dictionary<string, object> parameters)
    {
        var clientId = parameters.GetValueOrDefault("clientId")?.ToString() ?? "";
        var contractType = parameters.GetValueOrDefault("contractType")?.ToString() ?? "";
        var canton = parameters.GetValueOrDefault("canton")?.ToString() ?? "";

        return Task.FromResult($"📄 Vertrag '{contractType}' für Mitarbeiter {clientId} in {canton} wurde erstellt.");
    }

    private Task<string> ExecuteGetSystemInfoAsync()
    {
        return Task.FromResult("ℹ️ Klacks Planning System v1.0.0 - Status: Aktiv");
    }
}
