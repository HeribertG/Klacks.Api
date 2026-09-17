// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Computes the deterministic skill guarantees for one turn: skills that must be in the toolset
/// independent of retrieval quality, because a weak model cannot compensate for a missing tool and
/// several of these signals (a proposal reply, a planning-profile draft answer, a clarification's own
/// answer) carry no keyword vector retrieval could rank on. Extracted out of SkillToolsetAssembler,
/// whose own job is what happens with the RESULT of this resolution (merge, expand, exclude, truncate),
/// not how the guarantees themselves are found.
/// </summary>

using Klacks.Api.Application.Interfaces.Assistant;
using Klacks.Api.Application.Skills.PlanningProfile;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant;

namespace Klacks.Api.Application.Services.Assistant;

public class SkillToolsetGuaranteeResolver : ISkillToolsetGuaranteeResolver
{
    private const string CreateShiftSkillName = "create_shift";
    private const string CutShiftSkillName = "cut_shift";
    private const string ManagePendingNotesSkillName = "manage_pending_notes";

    private static readonly string[] PlanningProfileLoopSkillNames =
    [
        "set_planning_profile_parameters",
        "preview_planning_profile",
        "apply_planning_profile",
        "cancel_planning_profile_setup"
    ];

    private readonly IPendingUserNoteRepository _pendingUserNoteRepository;
    private readonly RecipeEngineService _recipeEngine;
    private readonly IPendingConfirmationStore _pendingConfirmationStore;
    private readonly IPendingPlanningProfileDraftStore _planningProfileDraftStore;
    private readonly ISkillPhraseRepository _skillPhraseRepository;
    private readonly ILogger<SkillToolsetGuaranteeResolver> _logger;

    public SkillToolsetGuaranteeResolver(
        IPendingUserNoteRepository pendingUserNoteRepository,
        RecipeEngineService recipeEngine,
        IPendingConfirmationStore pendingConfirmationStore,
        IPendingPlanningProfileDraftStore planningProfileDraftStore,
        ISkillPhraseRepository skillPhraseRepository,
        ILogger<SkillToolsetGuaranteeResolver> logger)
    {
        _pendingUserNoteRepository = pendingUserNoteRepository;
        _recipeEngine = recipeEngine;
        _pendingConfirmationStore = pendingConfirmationStore;
        _planningProfileDraftStore = planningProfileDraftStore;
        _skillPhraseRepository = skillPhraseRepository;
        _logger = logger;
    }

    public async Task<SkillToolsetGuaranteeResult> ResolveAsync(
        SkillToolsetGuaranteeRequest request, CancellationToken cancellationToken)
    {
        var permittedSkills = request.PermittedSkills;
        var guaranteedSkills = new HashSet<AgentSkill>();
        var guaranteedSources = new Dictionary<string, ToolsetSkillSource>(StringComparer.OrdinalIgnoreCase);

        var forcingRecipeNames = AddContentMatchGuarantees(guaranteedSkills, guaranteedSources, permittedSkills, request);

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
            guaranteedSkills, guaranteedSources, permittedSkills, request.UserMessage, request.UserId);

        // Deterministic keyword guarantee: skills whose trigger keywords or synonyms literally occur
        // in the message are always in the tool set. Weak models cannot compensate for a missing tool,
        // so the obviously-requested skill must never depend on the embedding ranking alone (capped,
        // longest match wins).
        var keywordMatchedSkills = SkillMatchingEngine.TopKeywordMatchedSkillNames(
            permittedSkills.Where(s => !s.AlwaysOn), request.UserMessage);

        foreach (var keywordSkillName in keywordMatchedSkills)
        {
            AddPermittedSkillByName(
                guaranteedSkills, permittedSkills, keywordSkillName, ToolsetSkillSource.Keyword, guaranteedSources);
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
            AddPermittedSkillByName(
                guaranteedSkills, permittedSkills, recipeSkillName, ToolsetSkillSource.RecipeStep, guaranteedSources);
        }

        // Plan-candidate guarantee: a message that requests several state-changing actions at once and
        // that no recipe covers is a candidate for create_plan. Surface create_plan so the model can
        // PROPOSE a multi-step plan instead of running the actions one by one; LLMService adds the
        // matching system-prompt nudge on the same turns. Non-candidate turns are unchanged.
        var recipeMatched = forcingRecipeNames.Count > 0 || engineRecipeNames.Count > 0;
        if (PlanTriggerHeuristic.IsPlanCandidate(request.UserMessage, recipeMatched))
        {
            AddPermittedSkillByName(
                guaranteedSkills, permittedSkills, PlanSkillDefaults.CreatePlanSkillName,
                ToolsetSkillSource.Hint, guaranteedSources);
        }

        // Planning-profile guarantee: while a profile draft is open, keep its loop skills in the tool set.
        // The recipe guarantee only covers the turn a recipe engages or resumes on, and the answers that
        // follow ("security", "8.5") carry no keyword at all — so without this the loop skills drop out
        // exactly on the turns that continue the dialogue, and the model reaches for create_scheduling_rule
        // instead (observed live 2026-08-10). Keyed by the same scope the skills write the draft under.
        AddPlanningProfileDraftGuarantee(
            guaranteedSkills, guaranteedSources, permittedSkills, request.UserId, request.ConversationId);

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
                AddPermittedSkillByName(
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
                AddPermittedSkillByName(
                    guaranteedSkills, permittedSkills, pinnedSkillName, ToolsetSkillSource.Hint, guaranteedSources);
            }
        }

        return new SkillToolsetGuaranteeResult(guaranteedSkills, guaranteedSources);
    }

    /// <summary>
    /// The guarantees resolvable from the message and permitted skills alone, without any store or
    /// service call: page/concept explain, the create_shift/cut_shift workflow pair, recipe-forcing and
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
            AddPermittedSkillByName(
                guaranteedSkills, permittedSkills, pageExplainSkill.Name, ToolsetSkillSource.Hint, guaranteedSources);
        }

        // Same guarantee for concept explain skills triggered by keywords in the current message
        // (e.g. orders/sealing): vector retrieval misses these phrasings, so the LLM answers thin
        // instead of calling the curated concept skill.
        foreach (var conceptSkillName in ConceptExplainSkillKeywords.ResolveSkillNames(request.UserMessage))
        {
            AddPermittedSkillByName(
                guaranteedSkills, permittedSkills, conceptSkillName, ToolsetSkillSource.Hint, guaranteedSources);
        }

        // Workflow-pair guarantee: an order is created (create_shift) and then split into parts
        // (cut_shift), usually in the same turn. cut_shift is not always-on and both vector retrieval
        // and the (probabilistic) co-required expansion can miss it on the create/confirm turn, so the
        // model navigates to the cut page or writes manual instructions instead of cutting. Guarantee
        // cut_shift deterministically whenever create_shift is in play (it survives truncation because
        // guaranteed skills are kept first).
        if (request.RetrievedSkills.Any(
                s => string.Equals(s.Name, CreateShiftSkillName, StringComparison.OrdinalIgnoreCase)))
        {
            AddPermittedSkillByName(
                guaranteedSkills, permittedSkills, CutShiftSkillName, ToolsetSkillSource.Hint, guaranteedSources);
        }

        // Recipe skill guarantee: when an operator-authored recipe engages, ALL of its step skills must
        // be in the tool set so the forcing spine can narrow the iteration to them. find_customer_candidates
        // in particular is not otherwise surfaced for a "create an order for customer X and cut it" request,
        // so without this the spine cannot force the lookup step and the model calls unrelated skills.
        var forcingRecipeNames = RecipeForcingResolver.GuaranteedSkillNames(request.UserMessage).ToList();
        foreach (var recipeSkillName in forcingRecipeNames)
        {
            AddPermittedSkillByName(
                guaranteedSkills, permittedSkills, recipeSkillName, ToolsetSkillSource.RecipeStep, guaranteedSources);
        }

        // Grouping-intent guarantee: when the user asks to group or assign clients/employees
        // geographically (by address/region/nearest), the real grouping skills must be in the tool set so
        // the model calls one instead of inventing a non-existent tool name (which never executes).
        foreach (var groupingSkillName in GroupingIntentResolver.GuaranteedSkillNames(request.UserMessage))
        {
            AddPermittedSkillByName(
                guaranteedSkills, permittedSkills, groupingSkillName, ToolsetSkillSource.Hint, guaranteedSources);
        }

        return forcingRecipeNames;
    }

    /// <summary>
    /// Adds the planning-profile loop skills when the user has an open draft in this conversation.
    /// A store failure degrades to no guarantee rather than taking toolset assembly down with it.
    /// </summary>
    private void AddPlanningProfileDraftGuarantee(
        HashSet<AgentSkill> guaranteedSkills,
        IDictionary<string, ToolsetSkillSource> guaranteedSources,
        IReadOnlyList<AgentSkill> permittedSkills,
        string userId,
        string? conversationId)
    {
        if (!Guid.TryParse(userId, out var draftUserId))
        {
            return;
        }

        try
        {
            var conversationKey = PlanningProfileDraftScope.ConversationKey(conversationId);
            if (_planningProfileDraftStore.Get(draftUserId, conversationKey) is null)
            {
                return;
            }

            foreach (var skillName in PlanningProfileLoopSkillNames)
            {
                AddPermittedSkillByName(
                    guaranteedSkills, permittedSkills, skillName, ToolsetSkillSource.Hint, guaranteedSources);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Planning-profile draft guarantee failed; continuing without it.");
        }
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
                AddPermittedSkillByName(
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
                AddPermittedSkillByName(
                    guaranteedSkills, permittedSkills, hint.SkillName, ToolsetSkillSource.Hint, guaranteedSources);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Proposal-confirmation guarantee failed; continuing without it.");
        }
    }

    private static void AddPermittedSkillByName(
        HashSet<AgentSkill> guaranteedSkills,
        IReadOnlyList<AgentSkill> permittedSkills,
        string skillName,
        ToolsetSkillSource source,
        IDictionary<string, ToolsetSkillSource> guaranteedSources)
    {
        var skill = permittedSkills.FirstOrDefault(s =>
            string.Equals(s.Name, skillName, StringComparison.OrdinalIgnoreCase));
        if (skill == null)
        {
            return;
        }

        // A skill can be guaranteed by several layers at once (keyword + learned phrase + recipe).
        // One source per candidate keeps the W5 provenance distribution additive, so the strongest
        // deterministic reason wins: RecipeStep > LearnedPhrase > Keyword > Hint.
        if (guaranteedSources.TryGetValue(skill.Name, out var existing) &&
            SourcePriority(existing) >= SourcePriority(source))
        {
            guaranteedSkills.Add(skill);
            return;
        }

        guaranteedSources[skill.Name] = source;
        guaranteedSkills.Add(skill);
    }

    private static int SourcePriority(ToolsetSkillSource source) => source switch
    {
        ToolsetSkillSource.RecipeStep => 5,
        ToolsetSkillSource.LearnedPhrase => 4,
        ToolsetSkillSource.Keyword => 3,
        ToolsetSkillSource.Hint => 2,
        _ => 1
    };

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
