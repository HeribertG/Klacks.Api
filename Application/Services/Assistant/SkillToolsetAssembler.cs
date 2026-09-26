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
/// <param name="applyLearnedPhraseGuarantee">
/// Whether a wording the learning loop stored in skill_phrase may claim a guarantee slot. True on every
/// chat path. The routing oracle O1 passes false: it probes with the learned wording itself, so leaving
/// the guarantee on would make every learned phrase reach its own target by definition and turn the only
/// quality gate the phrase path has into a tautology.
/// </param>

using System.Text.Json;
using Klacks.Api.Application.Interfaces.Assistant;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.KnowledgeIndex.Application.Constants;
using Klacks.Api.KnowledgeIndex.Application.Interfaces;
using Klacks.Api.KnowledgeIndex.Domain;

namespace Klacks.Api.Application.Services.Assistant;

public class SkillToolsetAssembler : ISkillToolsetAssembler
{
    private static readonly JsonSerializerOptions CaseInsensitiveJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly ISkillCacheService _skillCacheService;
    private readonly IKnowledgeRetrievalService _knowledgeRetrieval;
    private readonly IRetrievalQueryBuilder _retrievalQueryBuilder;
    private readonly ISkillRetrievalExpander _expander;
    private readonly ISkillToolsetGuaranteeResolver _guaranteeResolver;
    private readonly ILogger<SkillToolsetAssembler> _logger;

    public SkillToolsetAssembler(
        ISkillCacheService skillCacheService,
        IKnowledgeRetrievalService knowledgeRetrieval,
        IRetrievalQueryBuilder retrievalQueryBuilder,
        ISkillRetrievalExpander expander,
        ISkillToolsetGuaranteeResolver guaranteeResolver,
        ILogger<SkillToolsetAssembler> logger)
    {
        _skillCacheService = skillCacheService;
        _knowledgeRetrieval = knowledgeRetrieval;
        _retrievalQueryBuilder = retrievalQueryBuilder;
        _expander = expander;
        _guaranteeResolver = guaranteeResolver;
        _logger = logger;
    }

    public Task<SkillToolsetResult> AssembleAsync(
        Agent? agent,
        List<string> userRights,
        string userMessage,
        string? conversationId,
        string? currentRoute,
        string userId,
        string? language,
        int maxToolsForProvider = KnowledgeIndexConstants.MaxToolsForProvider,
        bool applyLearnedPhraseGuarantee = true,
        CancellationToken cancellationToken = default) =>
        AssembleAsync(
            agent, userRights, userMessage, conversationId, currentRoute, userId, language,
            maxToolsForProvider, applyLearnedPhraseGuarantee,
            excludedSkillNames: null, pinnedSkillNames: null, cancellationToken);

    public async Task<SkillToolsetResult> AssembleAsync(
        Agent? agent,
        List<string> userRights,
        string userMessage,
        string? conversationId,
        string? currentRoute,
        string userId,
        string? language,
        int maxToolsForProvider,
        bool applyLearnedPhraseGuarantee,
        IReadOnlyCollection<string>? excludedSkillNames,
        IReadOnlyCollection<string>? pinnedSkillNames,
        CancellationToken cancellationToken)
    {
        if (agent == null)
        {
            return new SkillToolsetResult();
        }

        var assemblyWatch = System.Diagnostics.Stopwatch.StartNew();

        var skills = await _skillCacheService.GetEnabledSkillsAsync(agent.Id, cancellationToken);
        var permittedSkills = skills
            .Where(s => Permissions.HasAllRequiredPermissions(userRights, s.RequiredPermission))
            .ToList();

        var alwaysOnSkills = permittedSkills.Where(s => s.AlwaysOn).ToList();
        var isAdmin = userRights.Contains(Roles.Admin);

        var (retrievedSkills, hasDomainSkillContext, retrievalScores) = await RetrieveSkillCandidatesAsync(
            permittedSkills, userRights, isAdmin, userMessage, conversationId, currentRoute, userId, cancellationToken);

        // Every deterministic skill guarantee for this turn - see ISkillToolsetGuaranteeResolver for the
        // full list. Extracted from this method because computing the guarantees and doing something
        // with the result (merge, expand, exclude, truncate, below) are two different responsibilities.
        var guaranteeResult = await _guaranteeResolver.ResolveAsync(
            new SkillToolsetGuaranteeRequest(
                agent.Id, permittedSkills, retrievedSkills, userMessage, conversationId, currentRoute, userId,
                language, userRights, pinnedSkillNames, applyLearnedPhraseGuarantee),
            cancellationToken);
        var guaranteedSkills = guaranteeResult.GuaranteedSkills;
        var guaranteedSources = guaranteeResult.GuaranteedSources;

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
            var alwaysOnProvenance = ResolveProvenance(
                alwaysOnSkills, guaranteedSources, retrievalScores, new HashSet<string>());
            return new SkillToolsetResult
            {
                Functions = ToStableOrderedFunctionsWithProvenance(alwaysOnSkills, alwaysOnProvenance),
                HasDomainSkillContext = hasDomainSkillContext,
                AssemblyMs = assemblyWatch.ElapsedMilliseconds
            };
        }

        var selectedSkills = alwaysOnSkills.Concat(retrievedSkills).DistinctBy(s => s.Name).ToList();
        var expansionNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Silent expansion: pull in high-confidence co-required neighbours of the selected skills into
        // FREE budget only (never evict). Best-effort — a failure must never break skill selection.
        // freeBudget is computed pre-exclusion; the exclusion filter and the truncation both run after
        // this block, so nothing is cut prematurely — a correction turn only ever sees a SMALLER
        // expansion fill, because the skills it excluded still count against selectedSkills.Count here.
        var freeBudget = maxToolsForProvider - selectedSkills.Count;
        if (freeBudget > 0)
        {
            try
            {
                var expansion = await _expander.ExpandAsync(agent.Id, selectedSkills, permittedSkills, freeBudget, cancellationToken);
                if (expansion.Count > 0)
                {
                    selectedSkills = selectedSkills.Concat(expansion).DistinctBy(s => s.Name).ToList();
                    foreach (var expandedSkill in expansion)
                    {
                        expansionNames.Add(expandedSkill.Name);
                    }
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Skill retrieval expansion failed; continuing without expansion.");
            }
        }

        // Applied once, here, and after the expansion rather than before the selection: the keyword
        // guarantee, the learned-phrase guarantee and the co-required expansion can each put the
        // corrected turn's skill back, and a filter that ran earlier would be undone by any of them.
        // Accepted noise: a co-required partner the expansion pulled in for an excluded skill is not
        // itself excluded and survives in the tool set.
        var excludedSkillNameSet = ResolveExclusionSet(excludedSkillNames);
        selectedSkills = DropExcluded(selectedSkills, excludedSkillNameSet, guaranteedSources);
        var postExclusionRetrievedCount = retrievedSkills.Count(
            s => !IsExcluded(s, excludedSkillNameSet, guaranteedSources));

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
            alwaysOnSkills.Count, postExclusionRetrievedCount, selectedSkills.Count, truncated,
            maxToolsForProvider, guaranteedSkills.Where(selectedSkills.Contains).ToList());

        var selectedProvenance = ResolveProvenance(
            selectedSkills, guaranteedSources, retrievalScores, expansionNames);

        return new SkillToolsetResult
        {
            Functions = ToStableOrderedFunctionsWithProvenance(selectedSkills, selectedProvenance),
            HasDomainSkillContext = true,
            AssemblyMs = assemblyWatch.ElapsedMilliseconds
        };
    }

    /// <summary>
    /// Retrieves the candidate skills for this turn and the domain-context signal for the world-model
    /// ontology gate. hasDomainSkillContext is true when retrieval surfaced ANY relevant skill above the
    /// score cutoff - including always-on domain skills (create_employee, list_contracts, …) that
    /// retrievedSkills itself drops via the !AlwaysOn filter, and stays true on a retrieval failure so
    /// the world model is never lost on an error path.
    /// </summary>
    private async Task<(List<AgentSkill> RetrievedSkills, bool HasDomainSkillContext, Dictionary<string, double> RetrievalScores)>
        RetrieveSkillCandidatesAsync(
            List<AgentSkill> permittedSkills, List<string> userRights, bool isAdmin, string userMessage,
            string? conversationId, string? currentRoute, string userId, CancellationToken cancellationToken)
    {
        var retrievalScores = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

        try
        {
            // History-anchored query: a bare confirmation turn ("yes, correct") must still retrieve
            // the task skill of the earlier turns.
            var retrievalQuery = await _retrievalQueryBuilder.BuildAsync(userMessage, conversationId, userId, cancellationToken);

            // Skills only, the other half of the kind filter the recipe pass already uses. A toolset
            // can never contain anything else: the mapping below matches candidates against skill
            // names, so a recipe among the candidates could only ever take a KNN slot away from a
            // skill and then be dropped. The index holds 460 skills against 24 recipes and no
            // endpoint entries at all (verified 2026-08-06), so the effect is small but strictly
            // one-directional.
            var retrieval = await _knowledgeRetrieval.RetrieveAsync(
                retrievalQuery, userRights, isAdmin, KnowledgeIndexConstants.DefaultTopK, currentRoute,
                cancellationToken, KnowledgeEntryKind.Skill);

            if (retrieval.IsEmpty)
            {
                return ([], false, retrievalScores);
            }

            foreach (var candidate in retrieval.Candidates)
            {
                retrievalScores.TryAdd(candidate.Entry.SourceId, candidate.Score);
            }

            var retrievedNames = retrieval.Candidates
                .Select(c => c.Entry.SourceId)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var retrievedSkills = permittedSkills
                .Where(s => !s.AlwaysOn && retrievedNames.Contains(s.Name))
                .ToList();

            return (retrievedSkills, true, retrievalScores);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Skill retrieval failed; falling back to always-on skills only");
            return ([], true, retrievalScores);
        }
    }

    // The tool array is part of the prompt prefix every provider caches (explicitly or implicitly).
    // Retrieval scores and guarantee insertions arrive in a turn-dependent order, so without this
    // sort two turns with the SAME skill set would still produce different request payloads and
    // invalidate the provider's prompt cache. Ordering is presentation only — which skills are in
    // the set (including the truncation priorities above) is decided before this point.
    internal static List<LLMFunction> ToStableOrderedFunctions(IEnumerable<AgentSkill> skills) =>
        skills
            .OrderByDescending(s => s.AlwaysOn)
            .ThenBy(s => s.Name, StringComparer.Ordinal)
            .Select(ConvertToLLMFunction)
            .ToList();

    private static List<LLMFunction> ToStableOrderedFunctionsWithProvenance(
        IEnumerable<AgentSkill> skills,
        IReadOnlyDictionary<string, SkillToolsetProvenance> provenance) =>
        skills
            .OrderByDescending(s => s.AlwaysOn)
            .ThenBy(s => s.Name, StringComparer.Ordinal)
            .Select(s => ConvertToLLMFunction(s, provenance))
            .ToList();

    /// <summary>
    /// Resolves the single provenance label for every selected skill. A skill can be in the toolset for
    /// several reasons at once; the label reflects the strongest deterministic one (see SourcePriority).
    /// Since 2026-09-16 the retrieval score is attached to a GUARANTEED skill too, not only to a
    /// retrieved one: the correction path ranks the deterministic candidates against each other and
    /// cannot do that on a null. This widens what lands in
    /// skill_selection_trajectories.knowledge_index_candidates_json - the same rows now carry a score
    /// where they previously carried none. The provenance DISTRIBUTION is unaffected, because it groups
    /// by Source; a query that treated "has a score" as "was retrieved" has to read Source instead.
    /// </summary>
    private static Dictionary<string, SkillToolsetProvenance> ResolveProvenance(
        IEnumerable<AgentSkill> skills,
        IReadOnlyDictionary<string, ToolsetSkillSource> guaranteedSources,
        IReadOnlyDictionary<string, double> retrievalScores,
        IReadOnlySet<string> expansionNames)
    {
        var provenance = new Dictionary<string, SkillToolsetProvenance>(StringComparer.OrdinalIgnoreCase);
        foreach (var skill in skills)
        {
            if (skill.AlwaysOn)
            {
                provenance[skill.Name] = new SkillToolsetProvenance(ToolsetSkillSource.AlwaysOn, null);
            }
            else if (guaranteedSources.TryGetValue(skill.Name, out var source))
            {
                provenance[skill.Name] = new SkillToolsetProvenance(
                    source, retrievalScores.TryGetValue(skill.Name, out var guaranteedScore) ? guaranteedScore : null);
            }
            else if (retrievalScores.TryGetValue(skill.Name, out var score))
            {
                provenance[skill.Name] = new SkillToolsetProvenance(ToolsetSkillSource.Retrieved, score);
            }
            else if (expansionNames.Contains(skill.Name))
            {
                provenance[skill.Name] = new SkillToolsetProvenance(ToolsetSkillSource.Expansion, null);
            }
        }

        return provenance;
    }

    private sealed record SkillToolsetProvenance(ToolsetSkillSource Source, double? RetrievalScore);

    /// <summary>
    /// Normalizes a correction turn's excluded-skill input into a case-insensitive set, with
    /// confirm_pending_action removed unconditionally: it is the user's only way to redeem a held
    /// pending action and must never leave the tool set, even if a caller lists it by name. A null or
    /// empty input yields an empty set.
    /// </summary>
    private static HashSet<string> ResolveExclusionSet(IReadOnlyCollection<string>? excludedSkillNames)
    {
        var excluded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (excludedSkillNames is { Count: > 0 })
        {
            excluded.UnionWith(excludedSkillNames);
        }

        excluded.Remove(AutonomyDefaults.ConfirmPendingActionSkillName);
        return excluded;
    }

    /// <summary>
    /// Whether a skill is dropped by the correction turn's exclusion: it must be in
    /// <paramref name="excluded"/> and must not be exempt. Always-on skills are exempt by definition
    /// (they are in every toolset regardless of this turn), and a RecipeStep-guaranteed skill is exempt
    /// too — the recipe engine's step decision on a composite recipe is more specific than the
    /// turn-level exclusion, and the forcing spine must never be pointed at a step skill missing from
    /// its own tool set. This is the single predicate both DropExcluded and the retrieved-count
    /// bookkeeping in AssembleAsync use, so the two can never disagree about what "excluded" means.
    /// </summary>
    private static bool IsExcluded(
        AgentSkill skill,
        IReadOnlySet<string> excluded,
        IReadOnlyDictionary<string, ToolsetSkillSource> guaranteedSources) =>
        !skill.AlwaysOn &&
        excluded.Contains(skill.Name) &&
        !(guaranteedSources.TryGetValue(skill.Name, out var source) && source == ToolsetSkillSource.RecipeStep);

    /// <summary>
    /// Drops the skills a correction turn must not offer again (see IsExcluded for the exemptions) and
    /// logs the dropped names. A no-op, without a log line, when nothing in the selection is excluded.
    /// </summary>
    private List<AgentSkill> DropExcluded(
        List<AgentSkill> selectedSkills,
        IReadOnlySet<string> excluded,
        IReadOnlyDictionary<string, ToolsetSkillSource> guaranteedSources)
    {
        if (excluded.Count == 0)
        {
            return selectedSkills;
        }

        var dropped = selectedSkills
            .Where(s => IsExcluded(s, excluded, guaranteedSources))
            .Select(s => s.Name)
            .ToList();

        if (dropped.Count == 0)
        {
            return selectedSkills;
        }

        _logger.LogInformation(
            "Correction turn: dropped {Count} skill(s) the corrected turn had called: {Skills}",
            dropped.Count, string.Join(", ", dropped));

        return selectedSkills.Where(s => !IsExcluded(s, excluded, guaranteedSources)).ToList();
    }

    // The guaranteed names are logged at Information, not Debug: "the skill was not in the tool set" is a
    // recurring failure class, and the counters alone never showed WHICH skills the deterministic layers
    // forced in. Debug is switched off for this namespace in every environment, so a Debug line here would
    // be invisible exactly when it is needed. One short line per chat turn, only when a layer forced
    // something. Since 2026-09-16 both retrievedCount and guaranteedSkills are the caller's responsibility
    // to pass POST-exclusion: a correction turn's dropped skill must not be counted as retrieved or
    // named as guaranteed here, or the log would claim the model got a tool it never received.
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

    private static LLMFunction ConvertToLLMFunction(AgentSkill skill) =>
        ConvertToLLMFunction(skill, null);

    private static LLMFunction ConvertToLLMFunction(
        AgentSkill skill, IReadOnlyDictionary<string, SkillToolsetProvenance>? provenance)
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

        var function = new LLMFunction
        {
            Name = skill.Name,
            Description = skill.Description,
            Parameters = parameters,
            RequiredParameters = requiredParameters,
            Labels = skill.Labels,
            Effect = skill.Effect
        };

        if (provenance != null && provenance.TryGetValue(skill.Name, out var skillProvenance))
        {
            function.ToolsetSource = skillProvenance.Source;
            function.RetrievalScore = skillProvenance.RetrievalScore;
        }

        return function;
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
