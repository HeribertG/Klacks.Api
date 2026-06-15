// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Executes LLM function calls from the chat loop: dispatches by execution type
/// (FrontendOnly, UiPassthrough, UiAction, Skill) and feeds results back to the model.
/// When navigate_to lands on a page with an explain_page_* skill, the executor runs that
/// knowledge happen (level=elements) itself and appends it to the navigation result, so
/// how-to questions get curated page knowledge without relying on the model calling it.
/// </summary>
/// <param name="skillBridge">Executes skills by name; also used for the server-side knowledge injection</param>
/// <param name="agentSkillRepository">Resolves the execution type of called skills</param>

using System.Text.Json;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Services.Assistant.Providers;
using Klacks.Api.Domain.Services.Assistant.Skills;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Services.Assistant;

public class LLMFunctionExecutor
{
    private readonly ILogger<LLMFunctionExecutor> _logger;
    private readonly ILLMSkillBridge? _skillBridge;
    private readonly IAgentSkillRepository _agentSkillRepository;
    private readonly IAgentRepository _agentRepository;

    private Dictionary<string, AgentSkill>? _skillCache;

    public LLMFunctionExecutor(
        ILogger<LLMFunctionExecutor> logger,
        IAgentSkillRepository agentSkillRepository,
        IAgentRepository agentRepository,
        ILLMSkillBridge? skillBridge = null)
    {
        _logger = logger;
        _agentSkillRepository = agentSkillRepository;
        _agentRepository = agentRepository;
        _skillBridge = skillBridge;
    }

    private async Task<AgentSkill?> GetSkillAsync(string functionName)
    {
        if (_skillCache == null)
        {
            var agent = await _agentRepository.GetDefaultAgentAsync();
            if (agent != null)
            {
                var skills = await _agentSkillRepository.GetEnabledAsync(agent.Id);
                _skillCache = skills.ToDictionary(d => d.Name);
            }
            else
            {
                _skillCache = new Dictionary<string, AgentSkill>();
            }
        }

        return _skillCache.GetValueOrDefault(functionName);
    }

    private async Task<string> GetExecutionTypeAsync(string functionName)
    {
        var skill = await GetSkillAsync(functionName);
        return skill?.ExecutionType ?? LlmExecutionTypes.Skill;
    }

    public bool HasOnlyUiPassthroughCalls { get; private set; }
    public string? NavigationRoute { get; private set; }

    public async Task<string> ProcessFunctionCallsAsync(LLMContext context, List<LLMFunctionCall> functionCalls)
    {
        var results = new List<string>();
        var allUiPassthrough = true;
        NavigationRoute = null;

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

    private static readonly Dictionary<string, string> FunctionNameAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        { "get_current_user", "get_user_context" },
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

        if (executionType == LlmExecutionTypes.UiAction)
        {
            _logger.LogInformation("UI action for {FunctionName} - frontend will execute declarative steps", call.FunctionName);
            var skill = await GetSkillAsync(call.FunctionName);
            call.UiActionSteps = skill?.HandlerConfig ?? "{}";
            var paramsJson = JsonSerializer.Serialize(call.Parameters);
            return $"Function '{call.FunctionName}' will be executed as UI action. Parameters: {paramsJson}";
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
            UserPermissions = context.UserRights,
            CurrentPage = context.PageContext?.CurrentRoute,
            SessionId = context.ConversationId
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
                var dataJson = JsonSerializer.Serialize(result.Data, SerializerOptions);
                var response = $"{result.Message}\nData: {dataJson}";

                if (result.ResultType == nameof(Klacks.Api.Domain.Enums.SkillResultType.Navigation))
                {
                    string? route = null;
                    try
                    {
                        using var doc = System.Text.Json.JsonDocument.Parse(dataJson);
                        if (doc.RootElement.TryGetProperty("Route", out var routeProp))
                            route = routeProp.GetString();
                    }
                    catch { }

                    if (route != null)
                    {
                        NavigationRoute = route;
                    }

                    if (call.FunctionName == SkillNames.NavigateTo)
                    {
                        var knowledgeAppendix = await BuildPageKnowledgeAppendixAsync(skillContext, route);
                        if (knowledgeAppendix != null)
                        {
                            response = $"{response}\n{knowledgeAppendix}";
                        }
                    }
                }

                return response;
            }

            return result.Message;
        }

        if (result.ResultType == nameof(Klacks.Api.Domain.Enums.SkillResultType.Confirmation))
        {
            return $"Confirmation required: {result.Message}";
        }

        return $"Error: {result.Message}";
    }

    private const string PageKnowledgeIntroFormat =
        "[Server-included page knowledge ({0}, level={1}) for the destination page. " +
        "Curated {0} knowledge is the only authoritative source about this page: when the user asks what it is, " +
        "how it works, or how to create/edit something here, ground every detail in {0} knowledge and never " +
        "invent fields, elements, or steps. All depth levels (short, elements, effects) come from this same " +
        "curated source — switching level only changes which slice you see, never whether you may invent. " +
        "The block below is level={1} and you already have it, so do not re-fetch level={1}. For a short overview " +
        "call {0} with level=short, for data sources and the interplay with other pages call level=effects, then " +
        "answer from that result. If neither this block nor a fetched level covers the question, say the page " +
        "knowledge does not cover it rather than guessing. " +
        "If the user only asked to navigate, briefly confirm the navigation.]";

    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = false };

    private async Task<string?> BuildPageKnowledgeAppendixAsync(SkillExecutionContext skillContext, string? route)
    {
        var explainSkillName = PageExplainSkillRoutes.ResolveSkillName(route);
        if (explainSkillName == null || _skillBridge == null)
        {
            return null;
        }

        try
        {
            var explainCall = new Providers.LLMFunctionCall
            {
                FunctionName = explainSkillName,
                Parameters = new Dictionary<string, object>
                {
                    [KnowledgeHappenLevels.ParameterName] = KnowledgeHappenLevels.Elements
                }
            };

            var result = await _skillBridge.ExecuteSkillFromLLMCallAsync(explainCall, skillContext);
            if (!result.Success)
            {
                _logger.LogWarning("Page knowledge injection for {SkillName} failed: {Message}",
                    explainSkillName, result.Message);
                return null;
            }

            var intro = string.Format(PageKnowledgeIntroFormat, explainSkillName, KnowledgeHappenLevels.Elements);
            if (result.Data == null)
            {
                return $"{intro}\n{result.Message}";
            }

            var dataJson = JsonSerializer.Serialize(result.Data, SerializerOptions);
            return $"{intro}\n{result.Message}\nData: {dataJson}";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Page knowledge injection for route {Route} failed", route);
            return null;
        }
    }
}
