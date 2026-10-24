// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Computes the deterministic skill guarantees for one turn: skills that must be in the toolset
/// independent of retrieval quality, because a weak model cannot compensate for a missing tool and
/// several of these signals (a proposal reply, a draft-loop answer, a clarification's own
/// answer) carry no keyword vector retrieval could rank on. Extracted out of SkillToolsetAssembler,
/// whose own job is what happens with the RESULT of this resolution (merge, expand, exclude, truncate),
/// not how the guarantees themselves are found.
/// </summary>

using Klacks.Api.Application.Interfaces.Assistant;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Assistant.TurnIntent;
using Klacks.Api.Domain.Services.Assistant;

namespace Klacks.Api.Application.Services.Assistant;

public class SkillToolsetGuaranteeResolver : ISkillToolsetGuaranteeResolver
{
    private const string ManagePendingNotesSkillName = "manage_pending_notes";

    private readonly IPendingUserNoteRepository _pendingUserNoteRepository;
    private readonly RecipeEngineService _recipeEngine;
    private readonly IPendingConfirmationStore _pendingConfirmationStore;
    private readonly IDraftLoopToolsetPolicy _draftLoopToolsetPolicy;
    private readonly ISkillPhraseRepository _skillPhraseRepository;
    private readonly ISkillRelationRepository _skillRelationRepository;
    private readonly ITurnIntentExplainGuarantee _turnIntentExplainGuarantee;
    private readonly IScenarioActivityProbe _scenarioActivityProbe;
    private readonly ILogger<SkillToolsetGuaranteeResolver> _logger;

    public SkillToolsetGuaranteeResolver(
        IPendingUserNoteRepository pendingUserNoteRepository,
        RecipeEngineService recipeEngine,
        IPendingConfirmationStore pendingConfirmationStore,
        IDraftLoopToolsetPolicy draftLoopToolsetPolicy,
        ISkillPhraseRepository skillPhraseRepository,
        ISkillRelationRepository skillRelationRepository,
        ITurnIntentExplainGuarantee turnIntentExplainGuarantee,
        IScenarioActivityProbe scenarioActivityProbe,
        ILogger<SkillToolsetGuaranteeResolver> logger)
    {
        _pendingUserNoteRepository = pendingUserNoteRepository;
        _recipeEngine = recipeEngine;
        _pendingConfirmationStore = pendingConfirmationStore;
        _draftLoopToolsetPolicy = draftLoopToolsetPolicy;
        _skillPhraseRepository = skillPhraseRepository;
        _skillRelationRepository = skillRelationRepository;
        _turnIntentExplainGuarantee = turnIntentExplainGuarantee;
        _scenarioActivityProbe = scenarioActivityProbe;
        _logger = logger;
    }

    public Task<TurnIntentPrediction?>? StartTurnIntent(
        string userMessage, string? language, IReadOnlyCollection<string>? pinnedSkillNames, CancellationToken cancellationToken) =>
        _turnIntentExplainGuarantee.Start(userMessage, language, pinnedSkillNames, cancellationToken);

    public async Task<SkillToolsetGuaranteeResult> ResolveAsync(
        SkillToolsetGuaranteeRequest request, CancellationToken cancellationToken)
    {
        var permittedSkills = request.PermittedSkills;
        var guaranteedSkills = new HashSet<AgentSkill>();
        var guaranteedSources = new Dictionary<string, ToolsetSkillSource>(StringComparer.OrdinalIgnoreCase);

        var forcingRecipeNames = AddContentMatchGuarantees(guaranteedSkills, guaranteedSources, permittedSkills, request);

        var turnIntent = await _turnIntentExplainGuarantee.ResolveAsync(request, cancellationToken);
        foreach (var explainSkillName in turnIntent.ExplainSkillNames)
        {
            SkillGuaranteeCollector.Add(
                guaranteedSkills, permittedSkills, explainSkillName, ToolsetSkillSource.Hint, guaranteedSources);
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
        ApplyProposalConfirmationGuarantee(
            guaranteedSkills, guaranteedSources, permittedSkills, request.UserMessage, request.UserId, request.Language);

        // Deterministic keyword guarantee: skills whose trigger keywords or synonyms literally occur
        // in the message are always in the tool set. Weak models cannot compensate for a missing tool,
        // so the obviously-requested skill must never depend on the embedding ranking alone (capped,
        // longest match wins). A skill that got there on broad terms only ("mitarbeiter" alone, shared by more
        // skills than the cap holds) is still offered, but as BroadKeyword: at truncation it competes with the
        // retrieved skills on its score instead of evicting the best-ranked one (owner decision 2026-10-24).
        var keywordMatches = SkillMatchingEngine.TopKeywordMatches(
            permittedSkills.Where(s => !s.AlwaysOn), request.UserMessage);

        foreach (var keywordMatch in keywordMatches)
        {
            SkillGuaranteeCollector.Add(
                guaranteedSkills, permittedSkills, keywordMatch.Name,
                keywordMatch.IsBroadOnly ? ToolsetSkillSource.BroadKeyword : ToolsetSkillSource.Keyword,
                guaranteedSources);
        }

        // Learned-phrase guarantee: the same deterministic promise for a wording the learning loop
        // stored, which the keyword guarantee above structurally cannot see. A learned row lands in
        // skill_phrase and from there only in the embedding text; SkillMatchingEngine reads
        // AgentSkill.TriggerKeywords/Synonyms, which no learning path ever writes. So a learned wording
        // could only nudge the ranking, and the skills it is learned for are by definition the ones the
        // ranking already places badly - they kept losing their place at the provider cap, which is what
        // made phrase learning look ineffective in the end-to-end runs of 2026-08-29. Capped and
        // longest-wording-first so the loop can complement retrieval without crowding it out.
        if (request.ApplyLearnedPhraseGuarantee)
        {
            await ApplyLearnedPhraseGuaranteeAsync(
                guaranteedSkills, guaranteedSources, permittedSkills, request.UserMessage, cancellationToken);
        }

        // Data-driven recipe guarantee: the same, for an engine recipe that is engaging now (matched on
        // this message) or resuming (paused on an ask in this conversation). Its step skills — e.g.
        // search_employees and add_client_to_group, neither always-on — must be present so the forcing
        // spine can narrow to them across the multi-turn flow.
        var engineRecipeNames = (await _recipeEngine.GuaranteedSkillNamesAsync(
            request.UserId, request.ConversationId, request.UserMessage, request.Language, request.UserRights,
            cancellationToken)).ToList();
        foreach (var recipeSkillName in engineRecipeNames)
        {
            SkillGuaranteeCollector.Add(
                guaranteedSkills, permittedSkills, recipeSkillName, ToolsetSkillSource.RecipeStep, guaranteedSources);
        }

        // Plan-candidate guarantee: a message that requests several state-changing actions at once and
        // that no recipe covers is a candidate for create_plan. Surface create_plan so the model can
        // PROPOSE a multi-step plan instead of running the actions one by one; LLMService adds the
        // matching system-prompt nudge on the same turns. Non-candidate turns are unchanged.
        var recipeMatched = forcingRecipeNames.Count > 0 || engineRecipeNames.Count > 0;
        if (PlanTriggerHeuristic.IsPlanCandidate(request.UserMessage, recipeMatched))
        {
            SkillGuaranteeCollector.Add(
                guaranteedSkills, permittedSkills, PlanSkillDefaults.CreatePlanSkillName,
                ToolsetSkillSource.Hint, guaranteedSources);
        }

        // Draft-loop guarantee: while a planning-profile setup or an order review is open, keep its loop skills in
        // the tool set (the answers that continue it carry no keyword) and keep out what would bypass the loop's
        // own confirmed preview. See DraftLoopToolsetPolicy.
        var draftLoop = _draftLoopToolsetPolicy.Resolve(request.UserId, request.ConversationId);
        foreach (var loopSkillName in draftLoop.GuaranteedSkillNames)
        {
            SkillGuaranteeCollector.Add(
                guaranteedSkills, permittedSkills, loopSkillName, ToolsetSkillSource.Hint, guaranteedSources);
        }

        // Pending-notes guarantee: surface manage_pending_notes only on turns where the current user
        // actually has undelivered notes, so the proactive hint can be acted on (read + mark) without
        // permanently occupying an always-on tool slot. It survives truncation (guaranteed skills first).
        if (Guid.TryParse(request.UserId, out var pendingNotesUserId))
        {
            var pendingNotesSkill = permittedSkills.FirstOrDefault(s =>
                string.Equals(s.Name, ManagePendingNotesSkillName, StringComparison.OrdinalIgnoreCase));
            if (pendingNotesSkill != null &&
                await _pendingUserNoteRepository.CountPendingAsync(
                    request.AgentId, pendingNotesUserId, cancellationToken) > 0)
            {
                SkillGuaranteeCollector.Add(
                    guaranteedSkills, permittedSkills, pendingNotesSkill.Name, ToolsetSkillSource.Hint, guaranteedSources);
            }
        }

        // Clarification pin: the two options the previous turn asked about. The answer ("die erste",
        // "die Gruppe") carries no keyword of either skill, so without this the toolset of the turn that
        // answers the question is the one turn that no longer contains the answer's own skill.
        if (request.PinnedSkillNames != null)
        {
            foreach (var pinnedSkillName in request.PinnedSkillNames)
            {
                SkillGuaranteeCollector.Add(
                    guaranteedSkills, permittedSkills, pinnedSkillName, ToolsetSkillSource.Hint, guaranteedSources);
            }
        }

        // Follow-through guarantee: the previous turn ran a KnowHow (Explain) or Advise skill that opens an
        // advisory chain, and the user's answer to it ("80 Prozent, monatlich, Kanton Zürich ...") carries no
        // keyword of the skill that is meant to act on it. Same class of gap as the planning-profile loop above,
        // but derived from the skill graph instead of a hand-kept list. Explain skills without an advised Act
        // skill guarantee nothing. Visibility only, like every guarantee. The same-skill continuation after it
        // keeps the Mutate skill the immediately preceding turn last ran, including one a confirm_pending_action
        // replayed, ranked below the advised skills at truncation.
        await ApplyFollowThroughGuaranteeAsync(
            guaranteedSkills, guaranteedSources, permittedSkills, request, cancellationToken);

        var lowestRankedNames = ApplySameSkillContinuationGuarantee(
            guaranteedSkills, guaranteedSources, permittedSkills, request.ContinuationSkillNames);

        OrderWorkflowSkillGuarantee.ApplyGroupAnswer(guaranteedSkills, guaranteedSources, lowestRankedNames, request);

        await ApplyScenarioFollowUpGuaranteeAsync(
            guaranteedSkills, guaranteedSources, request, cancellationToken);

        ApplyScheduleReadGuarantee(guaranteedSkills, guaranteedSources, lowestRankedNames, request);
        ApplyShiftListReadGuarantee(guaranteedSkills, guaranteedSources, request);

        ApplyGridMarkingGuarantee(guaranteedSkills, guaranteedSources, request);

        ApplyBookingDiagnosisGuarantee(guaranteedSkills, guaranteedSources, lowestRankedNames, request);

        return new SkillToolsetGuaranteeResult(
            guaranteedSkills, guaranteedSources, lowestRankedNames, turnIntent.VetoesForcedToolCall,
            draftLoop.ExcludedSkillNames);
    }

    /// <summary>
    /// Keeps read_schedule_state reachable for "who has duty on Monday in Winterthur": vector retrieval never ranks it
    /// for that wording (see ScheduleReadIntentResolver) and the route boost cannot lift a skill outside the pool. The
    /// schedule page keeps read_schedule_state; a recognised dated duty question also keeps list_groups, because the
    /// skill takes a group UUID. Runs after the message and history layers so that a list_groups another layer already claimed keeps its source. A
    /// list_groups claimed only here is the weakest guarantee of its tier (LowestRankedSkillNames), so at a small
    /// provider cap it goes before read_schedule_state and the other hints. Visibility only: both skills read.
    /// </summary>
    private static void ApplyScheduleReadGuarantee(
        HashSet<AgentSkill> guaranteedSkills,
        IDictionary<string, ToolsetSkillSource> guaranteedSources,
        HashSet<string> lowestRankedNames,
        SkillToolsetGuaranteeRequest request)
    {
        foreach (var skillName in ScheduleReadIntentResolver.GuaranteedSkillNames(request.UserMessage, request.CurrentRoute))
        {
            if (string.Equals(skillName, ScheduleReadIntentResolver.ListGroupsSkillName, StringComparison.Ordinal))
            {
                SkillGuaranteeCollector.AddAsLowestRankedHint(
                    guaranteedSkills, request.PermittedSkills, skillName, guaranteedSources, lowestRankedNames);
                continue;
            }

            SkillGuaranteeCollector.Add(
                guaranteedSkills, request.PermittedSkills, skillName, ToolsetSkillSource.Hint, guaranteedSources);
        }
    }

    /// <summary>
    /// Keeps search_shifts on the shift list page (ShiftListReadIntentResolver): only it counts the rows the list holds,
    /// search_in_list reads nothing back. Visibility only: the skill reads.
    /// </summary>
    private static void ApplyShiftListReadGuarantee(
        HashSet<AgentSkill> guaranteedSkills,
        IDictionary<string, ToolsetSkillSource> guaranteedSources,
        SkillToolsetGuaranteeRequest request)
    {
        foreach (var skillName in ShiftListReadIntentResolver.GuaranteedSkillNames(request.CurrentRoute))
        {
            SkillGuaranteeCollector.Add(
                guaranteedSkills, request.PermittedSkills, skillName, ToolsetSkillSource.Hint, guaranteedSources);
        }
    }

    /// <summary>
    /// Keeps diagnose_work_booking, diagnose_planning_period and explain_why_cannot_book for a "why ... not" question on the
    /// schedule page or naming a day (BookingDiagnosisIntentResolver). The booking diagnosis is the skill that answers the
    /// concrete cell; the planning-period check and the general explanation only complement it, so one claimed only here is
    /// lowest-ranked like list_groups: on the schedule page at the provider cap of 12 (nine always-on skills, three slots) the
    /// three slots stay with explain_page_schedule, read_schedule_state and the booking diagnosis. A shading question claims
    /// diagnose_planning_period before (ApplyGridMarkingGuarantee), which keeps its full rank. Visibility only: all read.
    /// </summary>
    private static void ApplyBookingDiagnosisGuarantee(
        HashSet<AgentSkill> guaranteedSkills,
        IDictionary<string, ToolsetSkillSource> guaranteedSources,
        HashSet<string> lowestRankedNames,
        SkillToolsetGuaranteeRequest request)
    {
        foreach (var skillName in BookingDiagnosisIntentResolver.GuaranteedSkillNames(request.UserMessage, request.CurrentRoute))
        {
            if (string.Equals(skillName, SkillNames.ExplainWhyCannotBook, StringComparison.Ordinal)
                || string.Equals(skillName, SkillNames.DiagnosePlanningPeriod, StringComparison.Ordinal))
            {
                SkillGuaranteeCollector.AddAsLowestRankedHint(
                    guaranteedSkills, request.PermittedSkills, skillName, guaranteedSources, lowestRankedNames);
                continue;
            }

            SkillGuaranteeCollector.Add(
                guaranteedSkills, request.PermittedSkills, skillName, ToolsetSkillSource.Hint, guaranteedSources);
        }
    }

    /// <summary>
    /// Keeps explain_page_schedule and diagnose_planning_period for a question about the shading of the schedule cells ("Warum
    /// sind die Zellen dunkelblau", GridMarkingIntentResolver). A day outside the employment is drawn dark and a day outside
    /// the group membership blue; the question names the colour, which no skill keyword carries, and without the diagnosis the
    /// model guesses other causes. Both are claimed as plain hints: the diagnosis is the skill that gives the facts and the
    /// explanation says what each shading means. Visibility only: both skills read.
    /// </summary>
    private static void ApplyGridMarkingGuarantee(
        HashSet<AgentSkill> guaranteedSkills,
        IDictionary<string, ToolsetSkillSource> guaranteedSources,
        SkillToolsetGuaranteeRequest request)
    {
        foreach (var skillName in GridMarkingIntentResolver.GuaranteedSkillNames(request.UserMessage, request.CurrentRoute))
        {
            SkillGuaranteeCollector.Add(
                guaranteedSkills, request.PermittedSkills, skillName, ToolsetSkillSource.Hint, guaranteedSources);
        }
    }

    /// <summary>
    /// Keeps list_scenarios reachable while a proposal scenario matters to the user, because the conversation
    /// history carries no ids of earlier tool results and accept_scenario (scenarioId required) is unusable
    /// without it (the history carries message texts, not tool-result ids, unless the assistant echoed them) - observed live 2026-09-30, when "Ja, übernimm es." after start_autowizard got accept_scenario
    /// but no list_scenarios. Two signals, either is enough: a skill that consumes a scenarioId is selected
    /// (retrieved or guaranteed by any layer above, which is why this runs last), or the user may see a running
    /// AutoWizard run or a recently opened scenario (IScenarioActivityProbe). No previous-turn pin on purpose: a
    /// non-empty pinned list switches the turn-intent classifier off. list_scenarios is guaranteed unconditionally
    /// by those signals (owner decision 2026-09-30); accept_scenario only through the narrow
    /// ApplyScenarioAcceptGuarantee below (owner approval 2026-10-16), never reject_scenario. The source is Hint:
    /// the strongest of the weak sources, so at truncation it survives retrieved skills and the follow-through
    /// guarantees, but never evicts the current message's own recipe, learned-phrase or keyword guarantees.
    /// The probe runs only when list_scenarios is permitted and no cheaper signal already decided; a failing
    /// probe degrades to no guarantee. Visibility only: nothing is executed or confirmed here.
    /// </summary>
    private async Task ApplyScenarioFollowUpGuaranteeAsync(
        HashSet<AgentSkill> guaranteedSkills,
        IDictionary<string, ToolsetSkillSource> guaranteedSources,
        SkillToolsetGuaranteeRequest request,
        CancellationToken cancellationToken)
    {
        var permittedSkills = request.PermittedSkills;
        var listScenariosPermitted = permittedSkills.Any(s =>
            string.Equals(s.Name, ScenarioFollowUpSkills.ListScenariosSkillName, StringComparison.OrdinalIgnoreCase));
        if (!listScenariosPermitted)
        {
            return;
        }

        ApplyScenarioAcceptGuarantee(guaranteedSkills, guaranteedSources, request);

        var selectedNames = guaranteedSkills.Select(s => s.Name).Concat(request.RetrievedSkills.Select(s => s.Name));
        var needed = ScenarioFollowUpSkills.NeedsScenarioList(selectedNames)
            || await HasScenarioActivityAsync(request.UserId, cancellationToken);
        if (!needed)
        {
            return;
        }

        SkillGuaranteeCollector.Add(
            guaranteedSkills, permittedSkills, ScenarioFollowUpSkills.ListScenariosSkillName,
            ToolsetSkillSource.Hint, guaranteedSources);
    }

    /// <summary>
    /// Keeps accept_scenario in the toolset for the one turn that needs it and retrieval cannot rank: the previous turn
    /// showed a scenario (list_scenarios or evaluate_scenario ran, see
    /// ScenarioFollowUpSkills.PresentsScenarioProposal) and the reply OPENS with an affirmation ("Ja, übernimm es.").
    /// The retrieval query is anchored on the first message of the conversation, so with a wording such as "Plane für
    /// Winterthur die Woche ..." accept_scenario is not even in the 25-candidate pool (found 2026-10-16, demo and
    /// fresh database alike) and the model answers that it has no such tool. A negation, a question mark or a
    /// conditional never opens an affirmation (AffirmationDetector), and without a scenario-showing previous turn
    /// a stray "ja" offers nothing. A wizard start is deliberately not such a turn: its scenario does not exist yet,
    /// and a "ja" to any follow-up question right after it must not expose accept_scenario while older scenarios are
    /// open (the stale-scenario consent guards the plan changing, not the choice between scenarios). Visibility only: the autonomy gate and the model's own choice stay untouched, and
    /// reject_scenario is never guaranteed. Owner approval 2026-10-16: this lifts the 2026-09-30 decision that only
    /// the read-only list_scenarios is guaranteed, for accept_scenario in exactly this case.
    /// </summary>
    private static void ApplyScenarioAcceptGuarantee(
        HashSet<AgentSkill> guaranteedSkills,
        IDictionary<string, ToolsetSkillSource> guaranteedSources,
        SkillToolsetGuaranteeRequest request)
    {
        if (!ScenarioFollowUpSkills.PresentsScenarioProposal(request.PreviousTurnSkillNames)
            || !AffirmationDetector.LeadsWithAffirmation(request.UserMessage, request.Language))
        {
            return;
        }

        SkillGuaranteeCollector.Add(
            guaranteedSkills, request.PermittedSkills, ScenarioFollowUpSkills.AcceptScenarioSkillName,
            ToolsetSkillSource.Hint, guaranteedSources);
    }

    private async Task<bool> HasScenarioActivityAsync(string userId, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(userId, out var activityUserId))
        {
            return false;
        }

        try
        {
            return await _scenarioActivityProbe.HasVisibleActivityAsync(activityUserId, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Scenario activity probe failed; continuing without the scenario guarantee.");
            return false;
        }
    }

    /// <summary>
    /// Keeps the Mutate skill the immediately preceding turn last ran successfully, so a follow-up on the same object ("and set
    /// its holiday calendar to Zurich as well") finds the skill the user just used, whatever its wording. A skill a
    /// confirm_pending_action replayed counts as run: the caller names that call by the replayed skill. The class
    /// is the curated Effect, never IsReadOnly, so Read, Explain and Advise skills are not continued here. Always-on
    /// skills are skipped because they are in every toolset anyway; only permitted (and therefore enabled) skills
    /// qualify, and the claim is capped at MaxContinuationSkills, most recent first. Its source is FollowThrough,
    /// the weakest guarantee, and a skill that no other guarantee claimed is returned as lowest-ranked, so at
    /// truncation it is dropped before the advised follow-through skills of the same source. Only the immediately
    /// preceding turn counts: the caller passes null once that record was superseded. Visibility only: the
    /// autonomy gate and a sensitive skill's confirmation are untouched.
    /// </summary>
    /// <param name="continuationSkillNames">Skills the immediately preceding turn's successful calls ran, most recent last</param>
    private static HashSet<string> ApplySameSkillContinuationGuarantee(
        HashSet<AgentSkill> guaranteedSkills,
        IDictionary<string, ToolsetSkillSource> guaranteedSources,
        IReadOnlyList<AgentSkill> permittedSkills,
        IReadOnlyList<string>? continuationSkillNames)
    {
        var lowestRankedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (continuationSkillNames is not { Count: > 0 })
        {
            return lowestRankedNames;
        }

        var continuedSkills = Enumerable.Reverse(continuationSkillNames)
            .Where(name => !string.Equals(name, AutonomyDefaults.ConfirmPendingActionSkillName, StringComparison.OrdinalIgnoreCase))
            .Select(name => permittedSkills.FirstOrDefault(s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase)))
            .Where(skill => skill is { Effect: SkillEffect.Mutate, AlwaysOn: false })
            .Take(SkillFollowThroughDefaults.MaxContinuationSkills)
            .ToList();

        foreach (var skill in continuedSkills)
        {
            if (!guaranteedSources.ContainsKey(skill!.Name))
            {
                lowestRankedNames.Add(skill.Name);
            }

            SkillGuaranteeCollector.Add(
                guaranteedSkills, permittedSkills, skill.Name, ToolsetSkillSource.FollowThrough, guaranteedSources);
        }

        return lowestRankedNames;
    }

    /// <summary>
    /// Keeps, for the skills the previous turn executed, what they lead to - advisory chains only. For an
    /// Explain skill K that is the Act skills A with an active ExplainedBy edge A to K that ALSO have at least one
    /// permitted Advise skill with an active AdvisesFor edge to A, plus those Advise skills. An Act skill without
    /// an adviser is not pulled in: a KnowHow answer is often just curiosity, and a guaranteed Mutate skill would
    /// outrank retrieved skills at truncation and tempt a weak model into an action nobody asked for. For an
    /// Advise skill it keeps its active AdvisesFor targets. The class of a previous skill is its curated Effect,
    /// never IsReadOnly. Only permitted skills qualify and the claim is capped. It is filled pairwise
    /// (PickFollowThrough), so the cap never drops an Act skill whose adviser was kept, and the order is by name,
    /// so the result is deterministic. Its source is FollowThrough, the weakest guarantee. The relation read
    /// happens only when the previous turn actually contains an Explain or Advise skill, and a failing read
    /// degrades to no guarantee.
    /// </summary>
    private async Task ApplyFollowThroughGuaranteeAsync(
        HashSet<AgentSkill> guaranteedSkills,
        IDictionary<string, ToolsetSkillSource> guaranteedSources,
        IReadOnlyList<AgentSkill> permittedSkills,
        SkillToolsetGuaranteeRequest request,
        CancellationToken cancellationToken)
    {
        if (request.PreviousTurnSkillNames is not { Count: > 0 } previousNames)
        {
            return;
        }

        var previousSkills = permittedSkills
            .Where(s => previousNames.Contains(s.Name, StringComparer.OrdinalIgnoreCase))
            .ToList();
        var explainNames = NamesWithEffect(previousSkills, SkillEffect.Explain);
        var adviseNames = NamesWithEffect(previousSkills, SkillEffect.Advise);

        if (explainNames.Count == 0 && adviseNames.Count == 0)
        {
            return;
        }

        try
        {
            var activeEdges = (await _skillRelationRepository.GetByAgentAsync(request.AgentId, cancellationToken))
                .Where(edge => edge.Status == SkillRelationStatus.Active)
                .ToList();

            var skillsByName = permittedSkills
                .GroupBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var explainedActNames = activeEdges
                .Where(edge => edge.Type == SkillRelationType.ExplainedBy && explainNames.Contains(edge.SkillBName))
                .Select(edge => edge.SkillAName)
                .Where(skillsByName.ContainsKey)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var advisedActNames = activeEdges
                .Where(edge => edge.Type == SkillRelationType.AdvisesFor && adviseNames.Contains(edge.SkillAName))
                .Select(edge => edge.SkillBName)
                .Where(skillsByName.ContainsKey)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var advisersByAct = explainNames.Count == 0
                ? new Dictionary<string, List<AgentSkill>>(StringComparer.OrdinalIgnoreCase)
                : AdvisersByAct(activeEdges, explainedActNames.Union(advisedActNames, StringComparer.OrdinalIgnoreCase), skillsByName);

            var actNames = explainedActNames
                .Where(advisersByAct.ContainsKey)
                .Union(advisedActNames, StringComparer.OrdinalIgnoreCase)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var skill in PickFollowThrough(actNames, advisersByAct, skillsByName))
            {
                SkillGuaranteeCollector.Add(
                    guaranteedSkills, permittedSkills, skill.Name, ToolsetSkillSource.FollowThrough, guaranteedSources);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Follow-through guarantee failed; continuing without it.");
        }
    }

    private static Dictionary<string, List<AgentSkill>> AdvisersByAct(
        IReadOnlyList<SkillRelation> activeEdges,
        IEnumerable<string> candidateActNames,
        IReadOnlyDictionary<string, AgentSkill> skillsByName)
    {
        var candidates = candidateActNames.ToHashSet(StringComparer.OrdinalIgnoreCase);

        return activeEdges
            .Where(edge => edge.Type == SkillRelationType.AdvisesFor
                && candidates.Contains(edge.SkillBName)
                && skillsByName.ContainsKey(edge.SkillAName))
            .GroupBy(edge => edge.SkillBName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => g.Select(edge => skillsByName[edge.SkillAName])
                    .DistinctBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
                    .OrderBy(s => s.SortOrder)
                    .ThenBy(s => s.Name, StringComparer.Ordinal)
                    .ToList(),
                StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Fills the follow-through claim pairwise so the cap can never separate an Act skill from the Advise
    /// skills in front of it: Act skills that have an adviser come first (then by name); each Act is added,
    /// then its advisers, while the claim is under its cap. An adviser is only ever added together with a
    /// picked Act, never on its own.
    /// </summary>
    private static List<AgentSkill> PickFollowThrough(
        IReadOnlySet<string> actNames,
        IReadOnlyDictionary<string, List<AgentSkill>> advisersByAct,
        IReadOnlyDictionary<string, AgentSkill> skillsByName)
    {
        var picked = new List<AgentSkill>();
        var acts = actNames
            .Select(name => skillsByName[name])
            .OrderBy(act => advisersByAct.ContainsKey(act.Name) ? 0 : 1)
            .ThenBy(act => act.Name, StringComparer.Ordinal);

        foreach (var act in acts)
        {
            if (picked.Count >= SkillFollowThroughDefaults.MaxGuaranteedSkills)
            {
                break;
            }

            if (picked.Contains(act))
            {
                continue;
            }

            picked.Add(act);

            if (!advisersByAct.TryGetValue(act.Name, out var advisers))
            {
                continue;
            }

            foreach (var adviser in advisers)
            {
                if (picked.Count >= SkillFollowThroughDefaults.MaxGuaranteedSkills)
                {
                    break;
                }

                if (!picked.Contains(adviser))
                {
                    picked.Add(adviser);
                }
            }
        }

        return picked;
    }

    private static HashSet<string> NamesWithEffect(IEnumerable<AgentSkill> skills, SkillEffect effect) =>
        skills.Where(s => s.Effect == effect).Select(s => s.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// The guarantees resolvable from the request alone (message, permitted, retrieved and continuation skills), without
    /// any store or service call: page/concept explain, the order workflow (create, seal, cut), recipe-forcing and
    /// grouping intent. Split out of ResolveAsync because it is the stateless half of the resolution;
    /// the forcing recipe names are returned because the plan-candidate guarantee downstream needs them
    /// alongside the engine-recipe names ResolveAsync resolves itself.
    /// </summary>
    private static List<string> AddContentMatchGuarantees(
        HashSet<AgentSkill> guaranteedSkills,
        Dictionary<string, ToolsetSkillSource> guaranteedSources,
        IReadOnlyList<AgentSkill> permittedSkills,
        SkillToolsetGuaranteeRequest request)
    {
        // Guarantee the explain skill of the page the user is on, independent of retrieval quality:
        // follow-up questions about page sections otherwise dilute the retrieval query and the LLM
        // hallucinates UI descriptions because the explain_page_* function is missing from its tools.
        var pageExplainSkill = ResolvePageExplainSkill(permittedSkills, request.CurrentRoute);
        if (pageExplainSkill != null)
        {
            SkillGuaranteeCollector.Add(
                guaranteedSkills, permittedSkills, pageExplainSkill.Name, ToolsetSkillSource.Hint, guaranteedSources);
        }

        // Same guarantee for concept explain skills triggered by keywords in the current message
        // (e.g. orders/sealing): vector retrieval misses these phrasings, so the LLM answers thin
        // instead of calling the curated concept skill.
        foreach (var conceptSkillName in ConceptExplainSkillKeywords.ResolveSkillNames(request.UserMessage))
        {
            SkillGuaranteeCollector.Add(
                guaranteedSkills, permittedSkills, conceptSkillName, ToolsetSkillSource.Hint, guaranteedSources);
        }

        // Order-workflow guarantee: seal_shift and cut_shift follow create_shift (OrderWorkflowSkillGuarantee).
        OrderWorkflowSkillGuarantee.ApplyFollowUps(guaranteedSkills, guaranteedSources, permittedSkills, request);

        // Recipe skill guarantee: when an operator-authored recipe engages, ALL of its step skills must
        // be in the tool set so the forcing spine can narrow the iteration to them. find_customer_candidates
        // in particular is not otherwise surfaced for a "create an order for customer X and cut it" request,
        // so without this the spine cannot force the lookup step and the model calls unrelated skills.
        var forcingRecipeNames = RecipeForcingResolver.GuaranteedSkillNames(request.UserMessage).ToList();
        foreach (var recipeSkillName in forcingRecipeNames)
        {
            SkillGuaranteeCollector.Add(
                guaranteedSkills, permittedSkills, recipeSkillName, ToolsetSkillSource.RecipeStep, guaranteedSources);
        }

        // Grouping-intent guarantee: when the user asks to group or assign clients/employees
        // geographically (by address/region/nearest), the real grouping skills must be in the tool set so
        // the model calls one instead of inventing a non-existent tool name (which never executes).
        foreach (var groupingSkillName in GroupingIntentResolver.GuaranteedSkillNames(request.UserMessage, request.Language))
        {
            SkillGuaranteeCollector.Add(
                guaranteedSkills, permittedSkills, groupingSkillName, ToolsetSkillSource.Hint, guaranteedSources);
        }

        return forcingRecipeNames;
    }

    /// <summary>
    /// Adds the skills whose learned wording literally occurs in this message. Best-effort like every
    /// other guarantee: a failing phrase read degrades to no guarantee instead of taking the turn down.
    /// One indexed-free read of skill_phrase per turn, the same query trajectory capture already runs;
    /// while nothing has been learned it returns an empty list.
    /// </summary>
    /// <param name="guaranteedSkills">Set the resolved skills are added to</param>
    /// <param name="permittedSkills">Skills the user may use, and the instances the truncation compares against</param>
    /// <param name="userMessage">Raw user message the learned wordings are matched against</param>
    private async Task ApplyLearnedPhraseGuaranteeAsync(
        HashSet<AgentSkill> guaranteedSkills,
        IDictionary<string, ToolsetSkillSource> guaranteedSources,
        IReadOnlyList<AgentSkill> permittedSkills,
        string userMessage,
        CancellationToken cancellationToken)
    {
        try
        {
            var learned = await _skillPhraseRepository.GetActiveBySourceAsync(
                SkillPhraseSources.Learned, LearnedPhraseMatcher.MatchLimit, cancellationToken);

            if (learned.Count == 0)
            {
                return;
            }

            var owners = LearnedPhraseMatcher.MatchingOwnerNames(
                learned, userMessage, SkillPhraseOwnerKinds.Skill, LearnedPhraseMatcher.GuaranteeCap);

            foreach (var ownerName in owners)
            {
                SkillGuaranteeCollector.Add(
                    guaranteedSkills, permittedSkills, ownerName, ToolsetSkillSource.LearnedPhrase, guaranteedSources);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Learned-phrase guarantee failed; continuing without it.");
        }
    }

    private void ApplyProposalConfirmationGuarantee(
        HashSet<AgentSkill> guaranteedSkills,
        IDictionary<string, ToolsetSkillSource> guaranteedSources,
        IReadOnlyList<AgentSkill> permittedSkills,
        string userMessage,
        string userId,
        string? language)
    {
        if (!Guid.TryParse(userId, out var proposalUserId))
        {
            return;
        }

        try
        {
            if (DeclineDetector.LeadsWithNegation(userMessage, language))
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
                SkillGuaranteeCollector.Add(
                    guaranteedSkills, permittedSkills, hint.SkillName, ToolsetSkillSource.Hint, guaranteedSources);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Proposal-confirmation guarantee failed; continuing without it.");
        }
    }

    private static AgentSkill? ResolvePageExplainSkill(IReadOnlyList<AgentSkill> permittedSkills, string? currentRoute)
    {
        var skillName = PageExplainSkillRoutes.ResolveSkillName(currentRoute);
        if (skillName == null)
        {
            return null;
        }

        return permittedSkills.FirstOrDefault(s => string.Equals(s.Name, skillName, StringComparison.OrdinalIgnoreCase));
    }
}
