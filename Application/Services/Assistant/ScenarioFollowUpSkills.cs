// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Names and rule of the scenario follow-up guarantee. A proposal scenario is produced in one turn
/// (e.g. start_autowizard) and shown, accepted or rejected in later ones, whose messages ("show me the
/// result", "yes, take it") carry no scenario vocabulary in any language. The scenario id only comes into
/// existence inside the job, and the conversation history carries message texts, not the ids of earlier tool
/// results (unless the assistant echoed them), so accept_scenario, reject_scenario and evaluate_scenario are
/// only usable when list_scenarios is in the same toolset. Only the read-only list_scenarios is ever
/// guaranteed unconditionally (owner decision 2026-09-30); the mutating accept_scenario reaches the toolset through
/// retrieval or the other guarantees, plus one narrow exception (owner approval 2026-10-16): when the previous turn presented a scenario
/// (PresentsScenarioProposal) and the reply opens with an affirmation, a bare "yes, take it" is the explicit accept
/// step and retrieval cannot rank on it (found 2026-10-16: with a first message that shares no vocabulary with the
/// wizard skills, accept_scenario fell out of the retrieval pool). reject_scenario is never guaranteed. Deliberately
/// no previous-turn pin: any non-empty pinned list switches the turn-intent classifier off for that turn.
/// </summary>

namespace Klacks.Api.Application.Services.Assistant;

public static class ScenarioFollowUpSkills
{
    public const string ListScenariosSkillName = "list_scenarios";
    public const string AcceptScenarioSkillName = "accept_scenario";
    public const string RejectScenarioSkillName = "reject_scenario";
    public const string EvaluateScenarioSkillName = "evaluate_scenario";

    private static readonly HashSet<string> ScenarioPresentingSkillNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ListScenariosSkillName,
        EvaluateScenarioSkillName
    };

    private static readonly HashSet<string> ScenarioIdConsumerSkillNames = new(StringComparer.OrdinalIgnoreCase)
    {
        AcceptScenarioSkillName,
        RejectScenarioSkillName,
        EvaluateScenarioSkillName
    };

    public static bool NeedsScenarioList(IEnumerable<string> selectedSkillNames) =>
        selectedSkillNames.Any(ScenarioIdConsumerSkillNames.Contains);

    public static bool PresentsScenarioProposal(IEnumerable<string>? previousTurnSkillNames) =>
        previousTurnSkillNames != null && previousTurnSkillNames.Any(ScenarioPresentingSkillNames.Contains);
}
