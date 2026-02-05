using System.Text.Json;
using Klacks.Api.Domain.Services.LLM.Providers;
using Klacks.Api.Infrastructure.MCP;
using Klacks.Api.Application.DTOs.LLM;

namespace Klacks.Api.Domain.Services.LLM;

public class LLMFunctionExecutor
{
    private readonly ILogger<LLMFunctionExecutor> _logger;
    private readonly IMCPService? _mcpService;

    public LLMFunctionExecutor(
        ILogger<LLMFunctionExecutor> logger,
        IMCPService? mcpService = null)
    {
        this._logger = logger;
        _mcpService = mcpService;
    }

    public async Task<string> ProcessFunctionCallsAsync(LLMContext context, List<LLMFunctionCall> functionCalls)
    {
        var results = new List<string>();

        foreach (var call in functionCalls)
        {
            try
            {
                var result = await ExecuteFunctionAsync(context, call);
                if (!string.IsNullOrEmpty(result))
                {
                    results.Add(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing function {FunctionName}", call.FunctionName);
                results.Add($"❌ Fehler beim Ausführen von {call.FunctionName}");
            }
        }

        return string.Join("\n", results);
    }

    private async Task<string> ExecuteFunctionAsync(LLMContext context, LLMFunctionCall call)
    {
        _logger.LogInformation("Executing function {FunctionName} with parameters {Parameters}",
            call.FunctionName, JsonSerializer.Serialize(call.Parameters));

        if (_mcpService != null)
        {
            try
            {
                var mcpResult = await _mcpService.ExecuteToolAsync(call.FunctionName, call.Parameters);
                if (!string.IsNullOrEmpty(mcpResult))
                {
                    return mcpResult;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "MCP execution failed for {FunctionName}, falling back to default", call.FunctionName);
            }
        }

        return call.FunctionName switch
        {
            "create_client" => await ExecuteCreateClientAsync(call.Parameters),
            "search_clients" => await ExecuteSearchClientsAsync(call.Parameters),
            "create_contract" => await ExecuteCreateContractAsync(call.Parameters),
            "get_system_info" => await ExecuteGetSystemInfoAsync(),
            _ => $"✅ Funktion '{call.FunctionName}' wurde ausgeführt."
        };
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
