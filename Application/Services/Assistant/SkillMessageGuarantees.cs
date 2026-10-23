// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The stateless skill guarantees of SkillToolsetGuaranteeResolver: everything resolvable from the turn's message,
/// route, retrieved and permitted skills alone, without any store or service call (page/concept explain, the
/// create_shift/cut_shift workflow pair, recipe forcing, grouping intent, trigger keywords, plan candidacy and plain
/// hint lists). Split out of the resolver to keep its file within the size limit; the resolver calls these in its
/// fixed guarantee order.
/// </summary>
/// <param name="guaranteedSkills">Set the resolved skills are added to</param>
/// <param name="guaranteedSources">Source per guaranteed skill name; the strongest source wins</param>
/// <param name="permittedSkills">Skills the user may use, and the instances the truncation compares against</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant;

namespace Klacks.Api.Application.Services.Assistant;

internal static class SkillMessageGuarantees
{
    private const string CreateShiftSkillName = "create_shift";
    private const string CutShiftSkillName = "cut_shift";

    /// <summary>
    /// The guarantees resolvable from the message and permitted skills alone, without any store or
    /// service call: page/concept explain, the create_shift/cut_shift workflow pair, recipe-forcing and
    /// grouping intent. Split out of ResolveAsync because it is the stateless half of the resolution;
    /// the forcing recipe names are returned because the plan-candidate guarantee downstream needs them
    /// alongside the engine-recipe names ResolveAsync resolves itself.
    /// </summary>
    internal static List<string> AddContentMatchGuarantees(
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

        // Workflow-pair guarantee: an order is created (create_shift) and then split into parts
        // (cut_shift), usually in the same turn. cut_shift is not always-on and both vector retrieval
        // and the (probabilistic) co-required expansion can miss it on the create/confirm turn, so the
        // model navigates to the cut page or writes manual instructions instead of cutting. Guarantee
        // cut_shift deterministically whenever create_shift is in play (it survives truncation because
        // guaranteed skills are kept first).
        if (request.RetrievedSkills.Any(
                s => string.Equals(s.Name, CreateShiftSkillName, StringComparison.OrdinalIgnoreCase)))
        {
            SkillGuaranteeCollector.Add(
                guaranteedSkills, permittedSkills, CutShiftSkillName, ToolsetSkillSource.Hint, guaranteedSources);
        }

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
    /// Adds each named permitted skill as a Hint. Used for the turn-intent Explain skills and for the clarification
    /// pins: the two options the previous turn asked about. The answer ("die erste", "die Gruppe") carries no keyword
    /// of either skill, so without the pin the toolset of the turn that answers the question is the one turn that no
    /// longer contains the answer's own skill.
    /// </summary>
    /// <param name="skillNames">Names to guarantee; names that are not permitted are skipped</param>
    internal static void AddHints(
        HashSet<AgentSkill> guaranteedSkills,
        IDictionary<string, ToolsetSkillSource> guaranteedSources,
        IReadOnlyList<AgentSkill> permittedSkills,
        IEnumerable<string> skillNames)
    {
        foreach (var skillName in skillNames)
        {
            SkillGuaranteeCollector.Add(
                guaranteedSkills, permittedSkills, skillName, ToolsetSkillSource.Hint, guaranteedSources);
        }
    }

    /// <summary>
    /// Deterministic keyword guarantee: skills whose trigger keywords or synonyms literally occur in the message are
    /// always in the tool set. Weak models cannot compensate for a missing tool, so the obviously-requested skill must
    /// never depend on the embedding ranking alone (capped, longest match wins). Always-on skills are skipped.
    /// A skill that got there on broad terms only ("mitarbeiter" alone, shared by more skills than the cap holds)
    /// is still offered, but as BroadKeyword: at truncation it competes with the retrieved skills on its score
    /// instead of evicting the best-ranked one (owner decision 2026-10-24).
    /// </summary>
    /// <param name="userMessage">Raw user message the trigger keywords and synonyms are matched against</param>
    internal static void ApplyKeywordGuarantee(
        HashSet<AgentSkill> guaranteedSkills,
        IDictionary<string, ToolsetSkillSource> guaranteedSources,
        IReadOnlyList<AgentSkill> permittedSkills,
        string userMessage)
    {
        var keywordMatches = SkillMatchingEngine.TopKeywordMatches(
            permittedSkills.Where(s => !s.AlwaysOn), userMessage);

        foreach (var keywordMatch in keywordMatches)
        {
            SkillGuaranteeCollector.Add(
                guaranteedSkills, permittedSkills, keywordMatch.Name,
                keywordMatch.IsBroadOnly ? ToolsetSkillSource.BroadKeyword : ToolsetSkillSource.Keyword,
                guaranteedSources);
        }
    }

    /// <summary>
    /// Plan-candidate guarantee: a message that requests several state-changing actions at once and that no recipe
    /// covers is a candidate for create_plan. Surface create_plan so the model can PROPOSE a multi-step plan instead
    /// of running the actions one by one; LLMService adds the matching system-prompt nudge on the same turns.
    /// Non-candidate turns are unchanged.
    /// </summary>
    /// <param name="userMessage">Raw user message whose mutation segments are counted</param>
    /// <param name="recipeMatched">Whether a forcing or engine recipe already covers the message</param>
    internal static void ApplyPlanCandidateGuarantee(
        HashSet<AgentSkill> guaranteedSkills,
        IDictionary<string, ToolsetSkillSource> guaranteedSources,
        IReadOnlyList<AgentSkill> permittedSkills,
        string userMessage,
        bool recipeMatched)
    {
        if (PlanTriggerHeuristic.IsPlanCandidate(userMessage, recipeMatched))
        {
            SkillGuaranteeCollector.Add(
                guaranteedSkills, permittedSkills, PlanSkillDefaults.CreatePlanSkillName,
                ToolsetSkillSource.Hint, guaranteedSources);
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
