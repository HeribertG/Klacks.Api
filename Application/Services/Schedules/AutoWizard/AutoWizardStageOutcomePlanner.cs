// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Schedules.AutoWizard;
using Klacks.Api.Application.Services.Schedules.HolisticHarmonizer;

namespace Klacks.Api.Application.Services.Schedules.AutoWizard;

/// <summary>
/// Decides what happens to the intermediate scenarios of an AutoWizard chain. Pure and side-effect free
/// so the rules stay testable without a job runner: on success only the final scenario survives, on
/// failure the last one produced is kept as a partial result and everything before it is removed.
/// </summary>
public static class AutoWizardStageOutcomePlanner
{
    /// <summary>
    /// Scenarios to delete after a successful run: everything except the last one.
    /// </summary>
    /// <param name="produced">Scenarios in the order the stages produced them.</param>
    public static IReadOnlyList<Guid> ScenariosToDeleteOnSuccess(IReadOnlyList<AutoWizardStageScenario> produced)
        => produced.Count <= 1
            ? []
            : produced.Take(produced.Count - 1).Select(s => s.ScenarioId).ToList();

    /// <summary>
    /// Scenarios to delete after a failed run: everything except the last one, which is handed to the
    /// operator as a partial result.
    /// </summary>
    /// <param name="produced">Scenarios in the order the stages produced them.</param>
    public static IReadOnlyList<Guid> ScenariosToDeleteOnFailure(IReadOnlyList<AutoWizardStageScenario> produced)
        => ScenariosToDeleteOnSuccess(produced);

    /// <summary>
    /// Builds the failure report, naming the stage that stopped the chain and the partial result if one exists.
    /// </summary>
    /// <param name="jobId">The orchestrator job.</param>
    /// <param name="produced">Scenarios in the order the stages produced them.</param>
    /// <param name="allStages">All stage names in chain order.</param>
    /// <param name="reason">Why the chain stopped.</param>
    public static AutoWizardJobFailureDto BuildFailure(
        Guid jobId,
        IReadOnlyList<AutoWizardStageScenario> produced,
        IReadOnlyList<string> allStages,
        string reason)
    {
        var failedStage = produced.Count < allStages.Count
            ? allStages[produced.Count]
            : allStages[^1];

        var partial = produced.Count > 0 ? produced[^1] : null;

        return new AutoWizardJobFailureDto(
            jobId,
            failedStage,
            reason,
            partial?.ScenarioId,
            partial?.Token,
            partial?.Name);
    }

    /// <summary>
    /// Decides whether a failed Holistic Harmonizer stage degrades to "completed without harmonization"
    /// instead of failing the chain. Only a missing prerequisite (no model, or a model now known to be
    /// text-only) degrades: the Harmonizer result is then a complete plan and the third stage could never
    /// have improved it. Any other stage failure stays a failure of the chain.
    /// </summary>
    /// <param name="readinessAfterFailure">Holistic Harmonizer readiness re-checked after the stage failed.</param>
    public static bool ShouldSkipHolisticStageAfterFailure(HolisticHarmonizerReadiness readinessAfterFailure)
        => !readinessAfterFailure.IsReady;

    /// <summary>
    /// The partial result stored next to a failure so a status poll can name the scenario the operator may
    /// still accept. Null when the chain failed before any stage produced a scenario.
    /// </summary>
    /// <param name="failure">The failure report.</param>
    /// <param name="elapsedMs">Wall-clock duration of the chain until it failed.</param>
    public static AutoWizardJobResultDto? BuildPartialResult(AutoWizardJobFailureDto failure, long elapsedMs)
        => failure.PartialScenarioId is null
            ? null
            : new AutoWizardJobResultDto(
                JobId: failure.JobId,
                FinalScenarioId: failure.PartialScenarioId,
                FinalScenarioToken: failure.PartialScenarioToken,
                FinalScenarioName: failure.PartialScenarioName,
                ElapsedMs: elapsedMs,
                QualificationGaps: [],
                ComplianceViolations: [],
                ComplianceSkippedPlacements: []);

    /// <summary>
    /// Note written onto the final scenario when the holistic harmonization was skipped. The chat history
    /// replays no tool results, so the job id of a run is usually gone on a later turn; the scenario itself
    /// is what the assistant finds again, and it has to carry the outcome.
    /// </summary>
    /// <param name="skippedReason">Why the holistic harmonization was skipped.</param>
    public static string BuildHarmonizationSkippedNote(string skippedReason)
        => $"AutoWizard completed without the holistic harmonization (stage 3 skipped: {skippedReason})";

    /// <summary>
    /// Human-readable failure text for the status endpoint, which stays string-based.
    /// </summary>
    /// <param name="failure">The failure report.</param>
    public static string BuildStatusReason(AutoWizardJobFailureDto failure)
        => failure.PartialScenarioName is null
            ? $"AutoWizard failed in the {failure.FailedStage} stage: {failure.Reason}"
            : $"AutoWizard failed in the {failure.FailedStage} stage: {failure.Reason} The partial result '{failure.PartialScenarioName}' was kept.";
}
