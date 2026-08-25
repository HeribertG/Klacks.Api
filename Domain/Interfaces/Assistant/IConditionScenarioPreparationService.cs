// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Turns a reported finding into a ready-made proposal: an AnalyseScenario a planner accepts or rejects
/// with one click. This is the Prepare rung of the proactive ladder - Klacksy does the work, a human
/// still decides. Nothing here executes anything; the scenario's accept path is the ordinary one every
/// human-authored scenario already uses.
/// </summary>

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IConditionScenarioPreparationService
{
    /// <summary>
    /// Creates the scenario, links it to the ledger row and moves that row to Prepared, then tells the
    /// planners it is waiting. Every way of not preparing THIS finding comes back as an outcome rather
    /// than an exception - a status that does not admit a proposal, a lost compare-and-swap, an
    /// undeliverable notification. Infrastructure failures are NOT swallowed: a failed commit or a
    /// dropped connection still propagates. A caller working through several findings in one tick must
    /// therefore wrap each call itself, exactly as ScheduledTaskRunner.RunDueAsync and
    /// AgentTriggerBackgroundService already do per task and per detector - otherwise one broken row
    /// costs every later row its turn, the failure mode Etappe 3h already paid for once.
    ///
    /// ORDER IS PART OF THE CONTRACT. The scenario is created and COMMITTED before the ledger
    /// transition, because AgentConditionRepository.TryTransitionAsync opens its own database
    /// transaction and refuses to nest inside one. The reverse order would be worse still: a row saying
    /// Prepared with no ScenarioId points a planner at a proposal that does not exist, whereas a
    /// scenario whose transition then loses the compare-and-swap is discarded again right here.
    /// </summary>
    /// <param name="condition">The ledger row to prepare for; its Status decides whether anything happens.</param>
    /// <param name="request">Scope of the scenario - window, and optionally name and group.</param>
    Task<ConditionScenarioPreparationResult> PrepareScenarioForConditionAsync(
        AgentCondition condition,
        ConditionScenarioRequest request,
        CancellationToken cancellationToken = default);
}
