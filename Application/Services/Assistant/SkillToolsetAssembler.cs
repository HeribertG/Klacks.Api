// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Assembles the per-turn LLM toolset shared by the streaming (LLMStreamingOrchestrator) and
/// non-streaming (ProcessLLMMessageCommandHandler) chat paths: permission filtering, always-on
/// skills, knowledge retrieval, deterministic skill guarantees, co-required expansion into free
/// budget and provider-cap truncation. Single owner of this logic so the two paths cannot drift.
/// </summary>
/// <param name="agent">Agent whose enabled skills form the candidate set; null yields an empty toolset.</param>
/// <param name="userRights">Permissions used for skill access control and retrieval scoping.</param>
/// <param name="userMessage">Current user message driving retrieval and keyword guarantees.</param>
/// <param name="conversationId">Conversation used for history-anchored retrieval and recipe resumption.</param>
/// <param name="currentRoute">Current UI route for the page-explain guarantee and retrieval boost.</param>
/// <param name="userId">User whose pending notes and recipe state gate the respective guarantees.</param>
/// <param name="language">UI language used for recipe matching.</param>
/// <param name="maxToolsForProvider">
/// Safety cap on the tool list sent to the provider, adaptive per the model's effective input limit
/// (see ContextBudgetPolicy). AlwaysOn skills are ordered first and survive truncation; retrieved
/// skills drop first. Must exceed (enabled alwaysOn count + DefaultTopK) at the reference tier or
/// retrieved skills are squeezed out entirely — guarded by SkillToolBudgetGuardTests.
/// </param>

using System.Text.Json;
using Klacks.Api.Application.Interfaces.Assistant;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant;
using Klacks.Api.KnowledgeIndex.Application.Constants;
using Klacks.Api.KnowledgeIndex.Application.Interfaces;

namespace Klacks.Api.Application.Services.Assistant;

public class SkillToolsetAssembler : ISkillToolsetAssembler
{
    private const string CreateShiftSkillName = "create_shift";
    private const string CutShiftSkillName = "cut_shift";
    private const string ManagePendingNotesSkillName = "manage_pending_notes";

    private static readonly JsonSerializerOptions CaseInsensitiveJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly ISkillCacheService _skillCacheService;
    private readonly IKnowledgeRetrievalService _knowledgeRetrieval;
    private readonly IRetrievalQueryBuilder _retrievalQueryBuilder;
    private readonly ISkillRetrievalExpander _expander;
    private readonly IPendingUserNoteRepository _pendingUserNoteRepository;
    private readonly RecipeEngineService _recipeEngine;
    private readonly IPendingConfirmationStore _pendingConfirmationStore;
    private readonly ILogger<SkillToolsetAssembler> _logger;

    public SkillToolsetAssembler(
        ISkillCacheService skillCacheService,
        IKnowledgeRetrievalService knowledgeRetrieval,
        IRetrievalQueryBuilder retrievalQueryBuilder,
        ISkillRetrievalExpander expander,
        IPendingUserNoteRepository pendingUserNoteRepository,
        RecipeEngineService recipeEngine,
        IPendingConfirmationStore pendingConfirmationStore,
        ILogger<SkillToolsetAssembler> logger)
    {
        _skillCacheService = skillCacheService;
        _knowledgeRetrieval = knowledgeRetrieval;
        _retrievalQueryBuilder = retrievalQueryBuilder;
        _expander = expander;
        _pendingUserNoteRepository = pendingUserNoteRepository;
        _recipeEngine = recipeEngine;
        _pendingConfirmationStore = pendingConfirmationStore;
        _logger = logger;
    }

    public async Task<SkillToolsetResult> AssembleAsync(
        Agent? agent,
        List<string> userRights,
        string userMessage,
        string? conversationId,
        string? currentRoute,
        string userId,
        string? language,
        int maxToolsForProvider = KnowledgeIndexConstants.MaxToolsForProvider,
        CancellationToken cancellationToken = default)
    {
        if (agent == null)
        {
            return new SkillToolsetResult();
        }

        var skills = await _skillCacheService.GetEnabledSkillsAsync(agent.Id, cancellationToken);
        var permittedSkills = skills
            .Where(s => Permissions.HasAllRequiredPermissions(userRights, s.RequiredPermission))
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
            // History-anchored query: a bare confirmation turn ("yes, correct") must still retrieve
            // the task skill of the earlier turns.
            var retrievalQuery = await _retrievalQueryBuilder.BuildAsync(userMessage, conversationId, cancellationToken);
            var retrieval = await _knowledgeRetrieval.RetrieveAsync(
                retrievalQuery, userRights, isAdmin, KnowledgeIndexConstants.DefaultTopK, currentRoute, cancellationToken);

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
            AddPermittedSkillByName(guaranteedSkills, permittedSkills, conceptSkillName);
        }

        // Workflow-pair guarantee: an order is created (create_shift) and then split into parts
        // (cut_shift), usually in the same turn. cut_shift is not always-on and both vector retrieval
        // and the (probabilistic) co-required expansion can miss it on the create/confirm turn, so the
        // model navigates to the cut page or writes manual instructions instead of cutting. Guarantee
        // cut_shift deterministically whenever create_shift is in play (it survives truncation because
        // guaranteed skills are kept first).
        if (retrievedSkills.Any(s => string.Equals(s.Name, CreateShiftSkillName, StringComparison.OrdinalIgnoreCase)))
        {
            AddPermittedSkillByName(guaranteedSkills, permittedSkills, CutShiftSkillName);
        }

        // Recipe skill guarantee: when an operator-authored recipe engages, ALL of its step skills must
        // be in the tool set so the forcing spine can narrow the iteration to them. find_customer_candidates
        // in particular is not otherwise surfaced for a "create an order for customer X and cut it" request,
        // so without this the spine cannot force the lookup step and the model calls unrelated skills.
        var forcingRecipeNames = RecipeForcingResolver.GuaranteedSkillNames(userMessage).ToList();
        foreach (var recipeSkillName in forcingRecipeNames)
        {
            AddPermittedSkillByName(guaranteedSkills, permittedSkills, recipeSkillName);
        }

        // Grouping-intent guarantee: when the user asks to group or assign clients/employees
        // geographically (by address/region/nearest), the real grouping skills must be in the tool set so
        // the model calls one instead of inventing a non-existent tool name (which never executes).
        foreach (var groupingSkillName in GroupingIntentResolver.GuaranteedSkillNames(userMessage))
        {
            AddPermittedSkillByName(guaranteedSkills, permittedSkills, groupingSkillName);
        }

        // Proposal-confirmation guarantee: a propose_* skill produces a read-only dry run and asks the
        // user to confirm. The reply carries no domain word, so retrieval and every keyword rule above
        // miss the paired apply_* skill and the model can only improvise prose instead of executing what
        // it just offered. The reply is NOT reliably a yes/no either: the model routinely asks a
        // follow-up question first ("from which date?") and the user answers with the bare value — a
        // date like "2026-06-01" contains no word token at all, so gating this on an affirmation closed
        // the tool set exactly on the turn that needed it. The live pending hint alone is therefore the
        // signal; only an explicit negation discards it. Visibility only: the apply call still runs
        // through the autonomy gate like any other invocation.
        ApplyProposalConfirmationGuarantee(guaranteedSkills, permittedSkills, userMessage, userId);

        // Deterministic keyword guarantee: skills whose trigger keywords or synonyms literally occur
        // in the message are always in the tool set. Weak models cannot compensate for a missing tool,
        // so the obviously-requested skill must never depend on the embedding ranking alone (capped,
        // longest match wins).
        foreach (var keywordSkillName in SkillMatchingEngine.TopKeywordMatchedSkillNames(
            permittedSkills.Where(s => !s.AlwaysOn), userMessage))
        {
            AddPermittedSkillByName(guaranteedSkills, permittedSkills, keywordSkillName);
        }

        // Data-driven recipe guarantee: the same, for an engine recipe that is engaging now (matched on
        // this message) or resuming (paused on an ask in this conversation). Its step skills — e.g.
        // search_employees and add_client_to_group, neither always-on — must be present so the forcing
        // spine can narrow to them across the multi-turn flow.
        var engineRecipeNames = (await _recipeEngine.GuaranteedSkillNamesAsync(
            userId, conversationId, userMessage, language, userRights, cancellationToken)).ToList();
        foreach (var recipeSkillName in engineRecipeNames)
        {
            AddPermittedSkillByName(guaranteedSkills, permittedSkills, recipeSkillName);
        }

        // Plan-candidate guarantee: a message that requests several state-changing actions at once and
        // that no recipe covers is a candidate for create_plan. Surface create_plan so the model can
        // PROPOSE a multi-step plan instead of running the actions one by one; LLMService adds the
        // matching system-prompt nudge on the same turns. Non-candidate turns are unchanged.
        var recipeMatched = forcingRecipeNames.Count > 0 || engineRecipeNames.Count > 0;
        if (Klacks.Api.Domain.Services.Assistant.PlanTriggerHeuristic.IsPlanCandidate(userMessage, recipeMatched))
        {
            AddPermittedSkillByName(guaranteedSkills, permittedSkills, Klacks.Api.Domain.Constants.PlanSkillDefaults.CreatePlanSkillName);
        }

        // Pending-notes guarantee: surface manage_pending_notes only on turns where the current user
        // actually has undelivered notes, so the proactive hint can be acted on (read + mark) without
        // permanently occupying an always-on tool slot. It survives truncation (guaranteed skills first).
        if (Guid.TryParse(userId, out var pendingNotesUserId))
        {
            var pendingNotesSkill = permittedSkills.FirstOrDefault(s =>
                string.Equals(s.Name, ManagePendingNotesSkillName, StringComparison.OrdinalIgnoreCase));
            if (pendingNotesSkill != null &&
                await _pendingUserNoteRepository.CountPendingAsync(agent.Id, pendingNotesUserId, cancellationToken) > 0)
            {
                guaranteedSkills.Add(pendingNotesSkill);
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
            LogToolBudget(alwaysOnSkills.Count, 0, alwaysOnSkills.Count, false, maxToolsForProvider, guaranteedSkills);
            return new SkillToolsetResult
            {
                Functions = alwaysOnSkills.Select(ConvertToLLMFunction).ToList(),
                HasDomainSkillContext = hasDomainSkillContext
            };
        }

        var selectedSkills = alwaysOnSkills.Concat(retrievedSkills).DistinctBy(s => s.Name).ToList();

        // Silent expansion: pull in high-confidence co-required neighbours of the selected skills into
        // FREE budget only (never evict). Best-effort — a failure must never break skill selection.
        var freeBudget = maxToolsForProvider - selectedSkills.Count;
        if (freeBudget > 0)
        {
            try
            {
                var expansion = await _expander.ExpandAsync(agent.Id, selectedSkills, permittedSkills, freeBudget, cancellationToken);
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
        var truncated = preCapCount > maxToolsForProvider;

        if (truncated)
        {
            selectedSkills = selectedSkills
                .OrderByDescending(s => s.AlwaysOn)
                .ThenByDescending(s => guaranteedSkills.Contains(s))
                .ThenBy(s => s.SortOrder)
                .Take(maxToolsForProvider)
                .ToList();
        }

        LogToolBudget(
            alwaysOnSkills.Count, retrievedSkills.Count, selectedSkills.Count, truncated,
            maxToolsForProvider, guaranteedSkills);

        return new SkillToolsetResult
        {
            Functions = selectedSkills.Select(ConvertToLLMFunction).ToList(),
            HasDomainSkillContext = true
        };
    }

    private void ApplyProposalConfirmationGuarantee(
        HashSet<AgentSkill> guaranteedSkills,
        IReadOnlyList<AgentSkill> permittedSkills,
        string userMessage,
        string userId)
    {
        if (!Guid.TryParse(userId, out var proposalUserId))
        {
            return;
        }

        try
        {
            if (DeclineDetector.LeadsWithNegation(userMessage))
            {
                _pendingConfirmationStore.DiscardProposalHints(proposalUserId);
                return;
            }

            var hint = _pendingConfirmationStore.PeekLatestForUser(
                proposalUserId,
                TimeSpan.FromSeconds(AutonomyDefaults.ProposalHintWindowSeconds),
                PendingConfirmationPurposes.ProposalHint);

            if (hint != null)
            {
                AddPermittedSkillByName(guaranteedSkills, permittedSkills, hint.SkillName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Proposal-confirmation guarantee failed; continuing without it.");
        }
    }

    private static void AddPermittedSkillByName(
        HashSet<AgentSkill> guaranteedSkills, IReadOnlyList<AgentSkill> permittedSkills, string skillName)
    {
        var skill = permittedSkills.FirstOrDefault(s =>
            string.Equals(s.Name, skillName, StringComparison.OrdinalIgnoreCase));
        if (skill != null)
        {
            guaranteedSkills.Add(skill);
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

    // The guaranteed names are logged at Information, not Debug: "the skill was not in the tool set" is a
    // recurring failure class, and the counters alone never showed WHICH skills the deterministic layers
    // forced in. Debug is switched off for this namespace in every environment, so a Debug line here would
    // be invisible exactly when it is needed. One short line per chat turn, only when a layer forced
    // something.
    private void LogToolBudget(
        int alwaysOnCount,
        int retrievedCount,
        int sentCount,
        bool truncated,
        int maxToolsForProvider,
        IReadOnlyCollection<AgentSkill> guaranteedSkills)
    {
        if (guaranteedSkills.Count > 0)
        {
            _logger.LogInformation(
                "LLM tool set guaranteed skills: {GuaranteedSkills}",
                string.Join(", ", guaranteedSkills.Select(s => s.Name)));
        }

        if (truncated)
        {
            _logger.LogWarning(
                "LLM tool budget hit cap: alwaysOn={AlwaysOn} retrieved={Retrieved} sent={Sent} cap={Cap}",
                alwaysOnCount, retrievedCount, sentCount, maxToolsForProvider);
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
