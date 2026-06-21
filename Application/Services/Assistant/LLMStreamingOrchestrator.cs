// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Orchestrates LLM streaming by preparing the context (agent, skill filtering via knowledge index) and delegating to ILLMService.
/// Bypasses the Mediator pipeline since it does not support IAsyncEnumerable.
/// </summary>
/// <param name="request">Contains message, userId, modelId, language and user rights</param>

using System.Runtime.CompilerServices;
using System.Text.Json;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Application.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant;
using Klacks.Api.Infrastructure.KnowledgeIndex.Application.Constants;
using Klacks.Api.Infrastructure.KnowledgeIndex.Application.Interfaces;

namespace Klacks.Api.Application.Services.Assistant;

public interface ILLMStreamingOrchestrator
{
    IAsyncEnumerable<SseChunk> ProcessStreamAsync(LLMStreamRequest request, CancellationToken cancellationToken = default);
}

public class LLMStreamRequest
{
    public string Message { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string? ConversationId { get; set; }
    public string? ModelId { get; set; }
    public string? Language { get; set; }
    public List<string> UserRights { get; set; } = new();
    public AssistantPageContext? PageContext { get; set; }
}

public class LLMStreamingOrchestrator : ILLMStreamingOrchestrator
{
    private readonly ILLMService _llmService;
    private readonly ISkillCacheService _skillCacheService;
    private readonly IKnowledgeRetrievalService _knowledgeRetrieval;
    private readonly LLMConversationManager _conversationManager;
    private readonly IPlanningScopeEnricher _planningScopeEnricher;
    private readonly ISkillRetrievalExpander _expander;
    private readonly ILogger<LLMStreamingOrchestrator> _logger;

    // Number of most recent user messages prepended to the skill-retrieval query so
    // that mid-workflow turns (e.g. a bare "yes, correct") still retrieve the task skill the
    // earlier turns were about (e.g. create_employee).
    private const int RecentMessagesForRetrieval = 4;

    private const string UserRoleName = "user";

    // Safety cap on the tool list sent to the provider. AlwaysOn skills are ordered first and survive
    // truncation; retrieved skills drop first. The cap MUST exceed (enabled alwaysOn count + DefaultTopK)
    // or retrieved skills are squeezed out entirely — which is exactly what happened when alwaysOn grew
    // to 22 against the old cap of 22 (no non-alwaysOn skill could reach the LLM). Now a single shared
    // constant, guarded by SkillToolBudgetGuardTests.
    private const int MaxToolsForProvider = KnowledgeIndexConstants.MaxToolsForProvider;

    private static readonly JsonSerializerOptions CaseInsensitiveJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public LLMStreamingOrchestrator(
        ILLMService llmService,
        ISkillCacheService skillCacheService,
        IKnowledgeRetrievalService knowledgeRetrieval,
        LLMConversationManager conversationManager,
        IPlanningScopeEnricher planningScopeEnricher,
        ISkillRetrievalExpander expander,
        ILogger<LLMStreamingOrchestrator> logger)
    {
        _llmService = llmService;
        _skillCacheService = skillCacheService;
        _knowledgeRetrieval = knowledgeRetrieval;
        _conversationManager = conversationManager;
        _planningScopeEnricher = planningScopeEnricher;
        _expander = expander;
        _logger = logger;
    }

    public async IAsyncEnumerable<SseChunk> ProcessStreamAsync(
        LLMStreamRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        Agent? agent = null;
        string? agentLoadError = null;
        try
        {
            agent = await _skillCacheService.GetDefaultAgentAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading agent for streaming");
            agentLoadError = "Failed to load agent.";
        }

        if (agentLoadError != null)
        {
            yield return SseChunk.Error(agentLoadError);
            yield break;
        }

        List<LLMFunction> functions;
        bool hasDomainSkillContext;
        try
        {
            (functions, hasDomainSkillContext) = await GetFilteredFunctionsAsync(
                agent, request.UserRights, request.Message, request.ConversationId,
                request.PageContext?.CurrentRoute, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error filtering functions for streaming");
            functions = new List<LLMFunction>();
            hasDomainSkillContext = false;
        }

        var context = new LLMContext
        {
            Message = request.Message,
            UserId = request.UserId,
            ConversationId = request.ConversationId,
            ModelId = request.ModelId,
            Language = request.Language,
            UserRights = request.UserRights,
            PageContext = request.PageContext,
            AvailableFunctions = functions,
            HasDomainSkillContext = hasDomainSkillContext
        };

        await _planningScopeEnricher.EnrichAsync(context, cancellationToken);

        await foreach (var chunk in _llmService.ProcessStreamAsync(context, cancellationToken))
        {
            yield return chunk;
        }
    }

    private async Task<(List<LLMFunction> Functions, bool HasRetrievedDomainSkill)> GetFilteredFunctionsAsync(
        Agent? agent, List<string> userRights, string userMessage, string? conversationId,
        string? currentRoute, CancellationToken ct)
    {
        if (agent == null) return ([], false);

        var skills = await _skillCacheService.GetEnabledSkillsAsync(agent.Id, ct);
        var permittedSkills = skills
            .Where(s => s.RequiredPermission == null ||
                        userRights.Contains(s.RequiredPermission) ||
                        userRights.Contains(Roles.Admin))
            .ToList();

        var alwaysOnSkills = permittedSkills.Where(s => s.AlwaysOn).ToList();

        var isAdmin = userRights.Contains(Roles.Admin);

        List<AgentSkill> retrievedSkills;

        // Domain-context signal for the world-model ontology gate: true when retrieval surfaced ANY relevant
        // skill above the score cutoff — including always-on domain skills (create_employee, list_contracts, …)
        // which are dropped from retrievedSkills below by the !AlwaysOn filter. Keying off retrievedSkills here
        // would wrongly classify a CRUD turn as conversational and strip the ontology. On retrieval failure we
        // keep it true so the world model is never lost on an error path.
        bool hasDomainSkillContext;
        try
        {
            var retrievalQuery = await BuildRetrievalQueryAsync(userMessage, conversationId, ct);
            var retrieval = await _knowledgeRetrieval.RetrieveAsync(
                retrievalQuery, userRights, isAdmin, KnowledgeIndexConstants.DefaultTopK, ct);

            hasDomainSkillContext = !retrieval.IsEmpty;

            if (!retrieval.IsEmpty)
            {
                var retrievedNames = retrieval.Candidates
                    .Select(c => c.Entry.SourceId)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                retrievedSkills = permittedSkills
                    .Where(s => !s.AlwaysOn && retrievedNames.Contains(s.Name))
                    .ToList();
            }
            else
            {
                retrievedSkills = [];
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Skill retrieval failed; falling back to always-on skills only");
            retrievedSkills = [];
            hasDomainSkillContext = true;
        }

        // Guarantee the explain skill of the page the user is on, independent of retrieval quality:
        // follow-up questions about page sections otherwise dilute the retrieval query and the LLM
        // hallucinates UI descriptions because the explain_page_* function is missing from its tools.
        var guaranteedSkills = new HashSet<AgentSkill>();
        var pageExplainSkill = ResolvePageExplainSkill(permittedSkills, currentRoute);
        if (pageExplainSkill != null)
        {
            guaranteedSkills.Add(pageExplainSkill);
        }

        // Same guarantee for concept explain skills triggered by keywords in the current message
        // (e.g. orders/sealing): vector retrieval misses these phrasings, so the LLM answers thin
        // instead of calling the curated concept skill.
        foreach (var conceptSkillName in ConceptExplainSkillKeywords.ResolveSkillNames(userMessage))
        {
            var conceptSkill = permittedSkills.FirstOrDefault(s =>
                string.Equals(s.Name, conceptSkillName, StringComparison.OrdinalIgnoreCase));
            if (conceptSkill != null)
            {
                guaranteedSkills.Add(conceptSkill);
            }
        }

        // Workflow-pair guarantee: an order is created (create_shift) and then split into parts
        // (cut_shift), usually in the same turn. cut_shift is not always-on and both vector retrieval
        // and the (probabilistic) co-required expansion can miss it on the create/confirm turn, so the
        // model navigates to the cut page or writes manual instructions instead of cutting. Guarantee
        // cut_shift deterministically whenever create_shift is in play (it survives truncation because
        // guaranteed skills are kept first).
        if (retrievedSkills.Any(s => string.Equals(s.Name, "create_shift", StringComparison.OrdinalIgnoreCase)))
        {
            var cutShiftSkill = permittedSkills.FirstOrDefault(s =>
                string.Equals(s.Name, "cut_shift", StringComparison.OrdinalIgnoreCase));
            if (cutShiftSkill != null)
            {
                guaranteedSkills.Add(cutShiftSkill);
            }
        }

        // Recipe skill guarantee: when an operator-authored recipe engages, ALL of its step skills must
        // be in the tool set so the forcing spine can narrow the iteration to them. find_customer_candidates
        // in particular is not otherwise surfaced for a "create an order for customer X and cut it" request,
        // so without this the spine cannot force the lookup step and the model calls unrelated skills.
        foreach (var recipeSkillName in RecipeForcingResolver.GuaranteedSkillNames(userMessage))
        {
            var recipeSkill = permittedSkills.FirstOrDefault(s =>
                string.Equals(s.Name, recipeSkillName, StringComparison.OrdinalIgnoreCase));
            if (recipeSkill != null)
            {
                guaranteedSkills.Add(recipeSkill);
            }
        }

        foreach (var guaranteed in guaranteedSkills)
        {
            if (!guaranteed.AlwaysOn &&
                !retrievedSkills.Any(s => string.Equals(s.Name, guaranteed.Name, StringComparison.OrdinalIgnoreCase)))
            {
                retrievedSkills.Insert(0, guaranteed);
            }
        }

        if (retrievedSkills.Count == 0)
        {
            LogToolBudget(alwaysOnSkills.Count, 0, alwaysOnSkills.Count, false);
            return (alwaysOnSkills.Select(ConvertToLLMFunction).ToList(), hasDomainSkillContext);
        }

        var selectedSkills = alwaysOnSkills.Concat(retrievedSkills).DistinctBy(s => s.Name).ToList();

        // Silent expansion: pull in high-confidence co-required neighbours of the selected skills into
        // FREE budget only (never evict). Best-effort — a failure must never break skill selection.
        var freeBudget = MaxToolsForProvider - selectedSkills.Count;
        if (freeBudget > 0)
        {
            try
            {
                var expansion = await _expander.ExpandAsync(agent.Id, selectedSkills, permittedSkills, freeBudget, ct);
                if (expansion.Count > 0)
                {
                    selectedSkills = selectedSkills.Concat(expansion).DistinctBy(s => s.Name).ToList();
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Skill retrieval expansion failed; continuing without expansion.");
            }
        }

        var preCapCount = selectedSkills.Count;
        var truncated = preCapCount > MaxToolsForProvider;

        if (truncated)
        {
            selectedSkills = selectedSkills
                .OrderByDescending(s => s.AlwaysOn)
                .ThenByDescending(s => guaranteedSkills.Contains(s))
                .ThenBy(s => s.SortOrder)
                .Take(MaxToolsForProvider)
                .ToList();
        }

        LogToolBudget(alwaysOnSkills.Count, retrievedSkills.Count, selectedSkills.Count, truncated);

        return (selectedSkills.Select(ConvertToLLMFunction).ToList(), true);
    }

    private async Task<string> BuildRetrievalQueryAsync(string userMessage, string? conversationId, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(conversationId))
        {
            return userMessage;
        }

        try
        {
            var history = await _conversationManager.GetConversationHistoryAsync(conversationId);
            if (history.Count == 0)
            {
                return userMessage;
            }

            // Only user messages enter the retrieval query: long assistant answers dilute the
            // embedding so badly that follow-up questions stop retrieving the relevant skill.
            var userMessages = history
                .Where(m => string.Equals(m.Role, UserRoleName, StringComparison.OrdinalIgnoreCase)
                            && !string.IsNullOrWhiteSpace(m.Content))
                .ToList();

            var parts = new List<string>();

            // Anchor with the first user message: it carries the workflow intent
            // (e.g. "create a new employee") which must keep the task skill retrievable
            // through long multi-turn flows, even when recent turns only discuss sub-details.
            var firstUserMessage = userMessages.FirstOrDefault();
            if (firstUserMessage != null)
            {
                parts.Add(firstUserMessage.Content);
            }

            parts.AddRange(userMessages
                .Skip(1)
                .TakeLast(RecentMessagesForRetrieval)
                .Select(m => m.Content));

            parts.Add(userMessage);

            return string.Join("\n", parts);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to enrich retrieval query with conversation history; using current message only");
            return userMessage;
        }
    }

    private static AgentSkill? ResolvePageExplainSkill(IReadOnlyList<AgentSkill> permittedSkills, string? currentRoute)
    {
        var skillName = PageExplainSkillRoutes.ResolveSkillName(currentRoute);
        if (skillName == null)
        {
            return null;
        }

        return permittedSkills.FirstOrDefault(s =>
            string.Equals(s.Name, skillName, StringComparison.OrdinalIgnoreCase));
    }

    private void LogToolBudget(int alwaysOnCount, int retrievedCount, int sentCount, bool truncated)
    {
        if (truncated)
        {
            _logger.LogWarning(
                "LLM tool budget hit cap: alwaysOn={AlwaysOn} retrieved={Retrieved} sent={Sent} cap={Cap}",
                alwaysOnCount, retrievedCount, sentCount, MaxToolsForProvider);
        }
        else if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug(
                "LLM tool budget: alwaysOn={AlwaysOn} retrieved={Retrieved} sent={Sent}",
                alwaysOnCount, retrievedCount, sentCount);
        }
    }

    private static LLMFunction ConvertToLLMFunction(AgentSkill skill)
    {
        var parameters = new Dictionary<string, object>();
        var requiredParameters = new List<string>();
        var paramDefs = JsonSerializer.Deserialize<List<ParameterDefinition>>(
            skill.ParametersJson, CaseInsensitiveJsonOptions) ?? [];

        foreach (var param in paramDefs)
        {
            var paramDict = new Dictionary<string, object>
            {
                ["type"] = NormalizeToJsonSchemaType(param.Type),
                ["description"] = param.Description
            };
            if (param.EnumValues is { Count: > 0 }) paramDict["enum"] = param.EnumValues;
            if (param.DefaultValue != null) paramDict["default"] = param.DefaultValue;
            parameters[param.Name] = paramDict;
            if (param.Required) requiredParameters.Add(param.Name);
        }

        return new LLMFunction
        {
            Name = skill.Name,
            Description = skill.Description,
            Parameters = parameters,
            RequiredParameters = requiredParameters
        };
    }

    private static string NormalizeToJsonSchemaType(string type) => type.ToLowerInvariant() switch
    {
        "string" => "string",
        "integer" => "integer",
        "decimal" or "number" => "number",
        "boolean" => "boolean",
        "date" or "time" or "datetime" => "string",
        "array" => "array",
        "object" => "object",
        "enum" => "string",
        _ => "string"
    };

    private class ParameterDefinition
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = "string";
        public string Description { get; set; } = string.Empty;
        public bool Required { get; set; }
        public object? DefaultValue { get; set; }
        public List<string>? EnumValues { get; set; }
    }
}
