// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

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
/// skills drop first, the lowest retrieval score before the higher ones. Must exceed
/// (enabled alwaysOn count + DefaultTopK) at the reference tier or
/// retrieved skills are squeezed out entirely — guarded by SkillToolBudgetGuardTests.
/// </param>
/// <param name="previousTurnSkillNames">
/// Skills the previous turn executed; a KnowHow or Advise skill among them keeps the Act skill it leads to in
/// this turn's toolset (follow-through guarantee). Ordered, most recent last. Null when the caller has no previous
/// turn to offer.
/// </param>
/// <param name="continuationSkillNames">
/// Skills the immediately preceding turn ran, a confirmed held action named by the skill it replayed, most recent
/// last; null when that record was superseded or the turn is a correction. Its last Mutate skill is kept
/// (same-skill continuation) and is the first guaranteed skill dropped when the provider cap truncates.
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
            excludedSkillNames: null, pinnedSkillNames: null, previousTurnSkillNames: null,
            continuationSkillNames: null, cancellationToken);

    /// <summary>
    /// Every failure inside the assembly degrades to the permitted always-on skills instead of reaching the
    /// caller. The caller's own catch (CorrectionTurnPreparer) can only fall back to an EMPTY toolset, and a
    /// turn without a single tool answers as if it had acted - the always-on set (navigation, search, user
    /// context, confirm_pending_action) at least keeps the assistant honest about what it can do. Only when
    /// even the skill cache fails does the exception travel on.
    /// </summary>
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
        IReadOnlyList<string>? previousTurnSkillNames,
        IReadOnlyList<string>? continuationSkillNames,
        CancellationToken cancellationToken)
    {
        if (agent == null)
        {
            return new SkillToolsetResult();
        }

        try
        {
            return await AssembleCoreAsync(
                agent, userRights, userMessage, conversationId, currentRoute, userId, language,
                maxToolsForProvider, applyLearnedPhraseGuarantee, excludedSkillNames, pinnedSkillNames,
                previousTurnSkillNames, continuationSkillNames, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex,
                "Skill toolset assembly failed for user {UserId}; this turn runs with the always-on skills only.",
                userId);
            return await AssembleAlwaysOnOnlyAsync(agent, userRights, cancellationToken);
        }
    }

    private async Task<SkillToolsetResult> AssembleAlwaysOnOnlyAsync(
        Agent agent, List<string> userRights, CancellationToken cancellationToken)
    {
        var skills = await _skillCacheService.GetEnabledSkillsAsync(agent.Id, cancellationToken);
        var alwaysOnSkills = skills
            .Where(s => s.AlwaysOn && Permissions.HasAllRequiredPermissions(userRights, s.RequiredPermission))
            .ToList();

        return new SkillToolsetResult
        {
            Functions = ToStableOrderedFunctions(alwaysOnSkills),
            HasDomainSkillContext = true
        };
    }

    private async Task<SkillToolsetResult> AssembleCoreAsync(
        Agent agent,
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
        IReadOnlyList<string>? previousTurnSkillNames,
        IReadOnlyList<string>? continuationSkillNames,
        CancellationToken cancellationToken)
    {

        var assemblyWatch = System.Diagnostics.Stopwatch.StartNew();

        var skills = await _skillCacheService.GetEnabledSkillsAsync(agent.Id, cancellationToken);
        var permittedSkills = skills
            .Where(s => Permissions.HasAllRequiredPermissions(userRights, s.RequiredPermission))
            .ToList();

        var alwaysOnSkills = permittedSkills.Where(s => s.AlwaysOn).ToList();
        var isAdmin = userRights.Contains(Roles.Admin);

        var pendingTurnIntent = _guaranteeResolver.StartTurnIntent(userMessage, language, pinnedSkillNames, cancellationToken);

        var (retrievedSkills, hasDomainSkillContext, retrievalScores, skillRerank) = await RetrieveSkillCandidatesAsync(
            permittedSkills, userRights, isAdmin, userMessage, conversationId, currentRoute, userId, cancellationToken);

        // Every deterministic skill guarantee for this turn - see ISkillToolsetGuaranteeResolver for the
        // full list. Extracted from this method because computing the guarantees and doing something
        // with the result (merge, expand, exclude, truncate, below) are two different responsibilities.
        var guaranteeResult = await _guaranteeResolver.ResolveAsync(
            new SkillToolsetGuaranteeRequest(
                agent.Id, permittedSkills, retrievedSkills, userMessage, conversationId, currentRoute, userId,
                language, userRights, pinnedSkillNames, previousTurnSkillNames, continuationSkillNames,
                applyLearnedPhraseGuarantee, retrievalScores, pendingTurnIntent),
            cancellationToken);
        var guaranteedSkills = guaranteeResult.GuaranteedSkills;
        var guaranteedSources = guaranteeResult.GuaranteedSources;

        PrependUnretrievedGuarantees(guaranteedSkills, retrievedSkills);

        if (retrievedSkills.Count == 0)
        {
            return BuildAlwaysOnOnlyResult(
                alwaysOnSkills, guaranteeResult, retrievalScores, hasDomainSkillContext, skillRerank, maxToolsForProvider,
                assemblyWatch);
        }

        var selectedSkills = alwaysOnSkills.Concat(retrievedSkills).DistinctBy(s => s.Name).ToList();
        var expansionNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        selectedSkills = await ExpandIntoFreeBudgetAsync(
            agent.Id, selectedSkills, permittedSkills, maxToolsForProvider, expansionNames, cancellationToken);

        (selectedSkills, var postExclusionRetrieved) = ApplyCorrectionExclusion(
            selectedSkills, retrievedSkills, excludedSkillNames, guaranteeResult.ExcludedSkillNames, guaranteedSources);

        return BuildCappedResult(
            selectedSkills, alwaysOnSkills.Count, postExclusionRetrieved, guaranteeResult, retrievalScores,
            expansionNames, skillRerank, maxToolsForProvider, assemblyWatch);
    }

    /// <summary>
    /// Drops the skills a correction turn must not offer again and counts the retrieved skills that survive the
    /// exclusion. Applied once, after the expansion rather than before the selection: the keyword guarantee, the
    /// learned-phrase guarantee and the co-required expansion can each put the corrected turn's skill back, and a
    /// filter that ran earlier would be undone by any of them. Accepted noise: a co-required partner the expansion
    /// pulled in for an excluded skill is not itself excluded and survives in the tool set.
    /// </summary>
    /// <param name="selectedSkills">The selection after expansion</param>
    /// <param name="retrievedSkills">The retrieved candidates including the prepended guarantees</param>
    /// <param name="excludedSkillNames">The correction turn's excluded skill names; null outside a correction</param>
    /// <param name="draftLoopExcludedSkillNames">The skills an open draft loop keeps out (DraftLoopToolsetPolicy)</param>
    /// <param name="guaranteedSources">This turn's guarantee sources, which exempt RecipeStep skills</param>
    private (List<AgentSkill> SelectedSkills, List<AgentSkill> RetrievedSkills) ApplyCorrectionExclusion(
        List<AgentSkill> selectedSkills,
        List<AgentSkill> retrievedSkills,
        IReadOnlyCollection<string>? excludedSkillNames,
        IReadOnlySet<string>? draftLoopExcludedSkillNames,
        IReadOnlyDictionary<string, ToolsetSkillSource> guaranteedSources)
    {
        var excludedSkillNameSet = ResolveExclusionSet(excludedSkillNames, draftLoopExcludedSkillNames);
        selectedSkills = DropExcluded(selectedSkills, excludedSkillNameSet, guaranteedSources);
        var postExclusionRetrieved = retrievedSkills
            .Where(s => !IsExcluded(s, excludedSkillNameSet, guaranteedSources))
            .ToList();

        return (selectedSkills, postExclusionRetrieved);
    }

    /// <summary>
    /// Cuts the selection to the provider cap when it exceeds it, logs the tool budget with the guaranteed skills that
    /// were actually sent and builds the toolset with the provenance of every selected skill.
    /// </summary>
    /// <param name="selectedSkills">The selection after expansion and exclusion</param>
    /// <param name="alwaysOnCount">Number of permitted always-on skills, for the budget log</param>
    /// <param name="postExclusionRetrieved">Retrieved skills left after the exclusion, for the budget log and the trace</param>
    /// <param name="guaranteeResult">This turn's guarantees, their sources and the veto</param>
    /// <param name="retrievalScores">The retrieval score per surfaced skill name</param>
    /// <param name="expansionNames">Names the co-required expansion added</param>
    /// <param name="skillRerank">The retrieval's skill-rerank decision for this turn, if any</param>
    /// <param name="maxToolsForProvider">The provider cap</param>
    /// <param name="assemblyWatch">Started when the assembly began; read last for the assembly time</param>
    private SkillToolsetResult BuildCappedResult(
        List<AgentSkill> selectedSkills,
        int alwaysOnCount,
        List<AgentSkill> postExclusionRetrieved,
        SkillToolsetGuaranteeResult guaranteeResult,
        Dictionary<string, double> retrievalScores,
        HashSet<string> expansionNames,
        SkillRerankObservation? skillRerank,
        int maxToolsForProvider,
        System.Diagnostics.Stopwatch assemblyWatch)
    {
        var preCapSkills = selectedSkills;
        var truncated = preCapSkills.Count > maxToolsForProvider;

        if (truncated)
        {
            selectedSkills = TruncateToBudget(selectedSkills, guaranteeResult, retrievalScores, maxToolsForProvider);
        }

        var trace = SkillToolsetTraceBuilder.Build(postExclusionRetrieved, retrievalScores, preCapSkills, selectedSkills);

        LogToolBudget(
            alwaysOnCount, postExclusionRetrieved.Count, selectedSkills.Count, truncated,
            maxToolsForProvider, guaranteeResult.GuaranteedSkills.Where(selectedSkills.Contains).ToList());

        var selectedProvenance = ResolveProvenance(
            selectedSkills, guaranteeResult.GuaranteedSources, retrievalScores, expansionNames);

        return new SkillToolsetResult
        {
            Functions = ToStableOrderedFunctionsWithProvenance(selectedSkills, selectedProvenance),
            HasDomainSkillContext = true,
            AssemblyMs = assemblyWatch.ElapsedMilliseconds,
            TurnIntentVetoesForcedToolCall = guaranteeResult.TurnIntentVetoesForcedToolCall,
            DraftLoopOpen = guaranteeResult.DraftLoopOpen,
            SkillRerank = skillRerank,
            Trace = trace
        };
    }

    /// <summary>
    /// Puts every guaranteed skill that is not always-on and that retrieval did not surface at the front of the
    /// retrieved list, so the guarantees join the selection whatever the retrieval returned.
    /// </summary>
    /// <param name="guaranteedSkills">This turn's guaranteed skills</param>
    /// <param name="retrievedSkills">The retrieved candidates; extended in place</param>
    private static void PrependUnretrievedGuarantees(
        IEnumerable<AgentSkill> guaranteedSkills, List<AgentSkill> retrievedSkills)
    {
        foreach (var guaranteed in guaranteedSkills)
        {
            if (!guaranteed.AlwaysOn &&
                !retrievedSkills.Any(s => string.Equals(s.Name, guaranteed.Name, StringComparison.OrdinalIgnoreCase)))
            {
                retrievedSkills.Insert(0, guaranteed);
            }
        }
    }

    /// <summary>
    /// The toolset of a turn without any retrieved or guaranteed non-always-on skill: the always-on skills only, with
    /// the domain-context signal of the retrieval (false when it surfaced nothing, true after a retrieval failure).
    /// </summary>
    /// <param name="alwaysOnSkills">The permitted always-on skills</param>
    /// <param name="guaranteeResult">This turn's guarantees, logged and used for the provenance and the veto</param>
    /// <param name="retrievalScores">The retrieval score per surfaced skill name</param>
    /// <param name="hasDomainSkillContext">The domain-context signal of the retrieval</param>
    /// <param name="skillRerank">The retrieval's skill-rerank decision for this turn, if any</param>
    /// <param name="maxToolsForProvider">The provider cap, for the budget log</param>
    /// <param name="assemblyWatch">Started when the assembly began; read last for the assembly time</param>
    private SkillToolsetResult BuildAlwaysOnOnlyResult(
        List<AgentSkill> alwaysOnSkills,
        SkillToolsetGuaranteeResult guaranteeResult,
        Dictionary<string, double> retrievalScores,
        bool hasDomainSkillContext,
        SkillRerankObservation? skillRerank,
        int maxToolsForProvider,
        System.Diagnostics.Stopwatch assemblyWatch)
    {
        LogToolBudget(
            alwaysOnSkills.Count, 0, alwaysOnSkills.Count, false, maxToolsForProvider, guaranteeResult.GuaranteedSkills);
        var alwaysOnProvenance = ResolveProvenance(
            alwaysOnSkills, guaranteeResult.GuaranteedSources, retrievalScores, new HashSet<string>());
        return new SkillToolsetResult
        {
            Functions = ToStableOrderedFunctionsWithProvenance(alwaysOnSkills, alwaysOnProvenance),
            HasDomainSkillContext = hasDomainSkillContext,
            AssemblyMs = assemblyWatch.ElapsedMilliseconds,
            TurnIntentVetoesForcedToolCall = guaranteeResult.TurnIntentVetoesForcedToolCall,
            DraftLoopOpen = guaranteeResult.DraftLoopOpen,
            SkillRerank = skillRerank,
            Trace = SkillToolsetTraceBuilder.Build([], retrievalScores, alwaysOnSkills, alwaysOnSkills)
        };
    }

    /// <summary>
    /// Silent expansion: pulls in high-confidence co-required neighbours of the selected skills into FREE budget only
    /// (never evict). Best-effort — a failure must never break skill selection. The free budget is computed
    /// pre-exclusion; the exclusion filter and the truncation both run after this, so nothing is cut prematurely — a
    /// correction turn only ever sees a SMALLER expansion fill, because the skills it excluded still count against the
    /// selection here.
    /// </summary>
    /// <param name="agentId">Agent whose skill relations drive the expansion</param>
    /// <param name="selectedSkills">The always-on and retrieved selection</param>
    /// <param name="permittedSkills">Skills the user may use; the expansion only picks among them</param>
    /// <param name="maxToolsForProvider">The provider cap the free budget is measured against</param>
    /// <param name="expansionNames">Receives the names of the skills the expansion added</param>
    /// <returns>The selection with the expansion appended, or the unchanged selection</returns>
    private async Task<List<AgentSkill>> ExpandIntoFreeBudgetAsync(
        Guid agentId,
        List<AgentSkill> selectedSkills,
        List<AgentSkill> permittedSkills,
        int maxToolsForProvider,
        HashSet<string> expansionNames,
        CancellationToken cancellationToken)
    {
        var freeBudget = maxToolsForProvider - selectedSkills.Count;
        if (freeBudget > 0)
        {
            try
            {
                var expansion = await _expander.ExpandAsync(agentId, selectedSkills, permittedSkills, freeBudget, cancellationToken);
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

        return selectedSkills;
    }

    /// <summary>
    /// Cuts the selection to the provider cap in survival order: always-on skills, then guaranteed skills by the
    /// strength of their source, with the same-skill continuation last within its source, then by retrieval score,
    /// best first. The score only ranks inside a tier the guarantees left open, so it never lifts a retrieved skill
    /// over a guaranteed one nor a weaker guarantee over a stronger one: the continuation a bare "yes" needs is a
    /// guarantee and survives every retrieved skill however well that one scored. A skill without a score (a
    /// guarantee retrieval never surfaced, a co-required expansion) ranks below every scored one. SortOrder and
    /// finally the name break exact ties, so the cut is deterministic; SortOrder alone decided before, and it is 0
    /// for practically every skill, which left the cut to the order of the skill cache.
    /// </summary>
    /// <param name="selectedSkills">The selection after expansion and exclusion</param>
    /// <param name="guarantees">This turn's guaranteed skills, their sources and the lowest-ranked among them</param>
    /// <param name="retrievalScores">The retrieval score of every candidate the knowledge index surfaced this turn</param>
    /// <param name="maxToolsForProvider">The provider cap</param>
    private static List<AgentSkill> TruncateToBudget(
        List<AgentSkill> selectedSkills,
        SkillToolsetGuaranteeResult guarantees,
        IReadOnlyDictionary<string, double> retrievalScores,
        int maxToolsForProvider) =>
        selectedSkills
            .OrderByDescending(s => s.AlwaysOn)
            .ThenByDescending(s => HoldsGuaranteeTier(s, guarantees))
            .ThenByDescending(s => guarantees.GuaranteedSources.TryGetValue(s.Name, out var source) ? source.Priority() : 0)
            .ThenBy(s => guarantees.LowestRankedSkillNames.Contains(s.Name))
            .ThenByDescending(s => retrievalScores.TryGetValue(s.Name, out var score) ? score : double.NegativeInfinity)
            .ThenBy(s => s.SortOrder)
            .ThenBy(s => s.Name, StringComparer.Ordinal)
            .Take(maxToolsForProvider)
            .ToList();

    /// <summary>
    /// Whether a skill survives the cap ahead of every retrieved skill. True for a guaranteed skill unless its
    /// only claim is a BroadKeyword match: that one stays in the retrieval tier and is ordered there by its
    /// retrieval score (BroadKeyword's priority of 0 equals an unguaranteed skill's), so a generic noun can no
    /// longer push the best-ranked retrieved skill out of a 12-tool budget.
    /// </summary>
    private static bool HoldsGuaranteeTier(AgentSkill skill, SkillToolsetGuaranteeResult guarantees) =>
        guarantees.GuaranteedSkills.Contains(skill)
        && (!guarantees.GuaranteedSources.TryGetValue(skill.Name, out var source) || source.HoldsAboveRetrieval());

    /// <summary>
    /// Retrieves the candidate skills for this turn and the domain-context signal for the world-model
    /// ontology gate. hasDomainSkillContext is true when retrieval surfaced ANY relevant skill above the
    /// score cutoff - including always-on domain skills (create_employee, list_contracts, …) that
    /// retrievedSkills itself drops via the !AlwaysOn filter, and stays true on a retrieval failure so
    /// the world model is never lost on an error path.
    /// </summary>
    private async Task<(List<AgentSkill> RetrievedSkills, bool HasDomainSkillContext, Dictionary<string, double> RetrievalScores, SkillRerankObservation? SkillRerank)>
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
                return ([], false, retrievalScores, ToObservation(retrieval.SkillRerank));
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

            return (retrievedSkills, true, retrievalScores, ToObservation(retrieval.SkillRerank));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Skill retrieval failed; falling back to always-on skills only");
            return ([], true, retrievalScores, null);
        }
    }

    private static SkillRerankObservation? ToObservation(SkillRerankDecision? decision) =>
        decision == null ? null : new SkillRerankObservation(decision.Margin, decision.Reranked, decision.Explored);

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
    /// Normalizes a correction turn's excluded-skill input, together with the skills an open draft loop keeps out
    /// (DraftLoopToolsetPolicy), into a case-insensitive set, with
    /// confirm_pending_action removed unconditionally: it is the user's only way to redeem a held
    /// pending action and must never leave the tool set, even if a caller lists it by name. A null or
    /// empty input yields an empty set.
    /// </summary>
    private static HashSet<string> ResolveExclusionSet(
        IReadOnlyCollection<string>? excludedSkillNames, IReadOnlySet<string>? draftLoopExcludedSkillNames)
    {
        var excluded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (excludedSkillNames is { Count: > 0 })
        {
            excluded.UnionWith(excludedSkillNames);
        }

        if (draftLoopExcludedSkillNames is { Count: > 0 })
        {
            excluded.UnionWith(draftLoopExcludedSkillNames);
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
    /// bookkeeping in ApplyCorrectionExclusion use, so the two can never disagree about what "excluded" means.
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
            if (param.EnumValues is { Count: > 0 }) paramDict[SkillParameterSchemaKeys.Enum] = param.EnumValues;
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
