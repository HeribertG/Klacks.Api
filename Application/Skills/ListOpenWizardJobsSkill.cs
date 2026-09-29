// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Skill that reports the status of a wizard job. A job that still runs is found in the live registries
/// (AutoWizard / Wizard 1 / Wizard 2 / Wizard 3); a finished AutoWizard job is answered from its stored
/// terminal state, so the assistant can tell the user whether the run completed, completed without the
/// holistic harmonization, failed or was cancelled - and which scenario is ready to be accepted.
/// </summary>
/// <param name="jobId">UUID of the job returned when the wizard was started.</param>

using Klacks.Api.Application.Constants;
using Klacks.Api.Application.DTOs.Schedules.AutoWizard;
using Klacks.Api.Application.Services.Schedules;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Application.Interfaces.Schedules.AutoWizard;
using Klacks.Api.Application.Interfaces.Schedules.HolisticHarmonizer;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("list_open_wizard_jobs")]
public class ListOpenWizardJobsSkill : BaseSkillImplementation
{
    private readonly IWizardJobRunner _wizard;
    private readonly IHarmonizerJobRunner _harmonizer;
    private readonly IHolisticHarmonizerJobRunner _holistic;
    private readonly IAutoWizardJobRunner _autoWizard;
    private readonly JobTerminalStateCache<AutoWizardJobResultDto> _autoWizardStates;

    public ListOpenWizardJobsSkill(
        IWizardJobRunner wizard,
        IHarmonizerJobRunner harmonizer,
        IHolisticHarmonizerJobRunner holistic,
        IAutoWizardJobRunner autoWizard,
        JobTerminalStateCache<AutoWizardJobResultDto> autoWizardStates)
    {
        _wizard = wizard;
        _harmonizer = harmonizer;
        _holistic = holistic;
        _autoWizard = autoWizard;
        _autoWizardStates = autoWizardStates;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var jobIdRaw = GetParameter<string>(parameters, "jobId");
        if (string.IsNullOrWhiteSpace(jobIdRaw))
        {
            return SkillResult.SuccessResult(
                new
                {
                    Hint = "Pass the jobId that was returned when the wizard was started. The wizard registries " +
                           "have no enumeration; finished AutoWizard jobs stay readable for a short while. Without a jobId, list the open " +
                           "scenarios: their description notes a failed run or a skipped holistic harmonization."
                },
                "A job status needs an explicit jobId — the wizard registries don't expose a full enumeration.");
        }

        if (!Guid.TryParse(jobIdRaw, out var jobId))
        {
            return SkillResult.Error($"Invalid jobId '{jobIdRaw}'. Expected UUID.");
        }

        var isAutoWizard = _autoWizard.IsRunning(jobId);
        var isWizard1Planner = _wizard.IsRunning(jobId);
        var isWizard2Harmonizer = _harmonizer.IsRunning(jobId);
        var isWizard3Holistic = _holistic.IsRunning(jobId);

        if (isAutoWizard || isWizard1Planner || isWizard2Harmonizer || isWizard3Holistic)
        {
            return SkillResult.SuccessResult(
                new
                {
                    JobId = jobId,
                    Status = WizardJobStatusValues.Running,
                    IsAutoWizard = isAutoWizard,
                    IsWizard1Planner = isWizard1Planner,
                    IsWizard2Harmonizer = isWizard2Harmonizer,
                    IsWizard3Holistic = isWizard3Holistic
                },
                $"Job {jobId} is still running.");
        }

        var state = await _autoWizardStates.TryGetAsync(jobId, cancellationToken);
        if (!state.Found)
        {
            return SkillResult.SuccessResult(
                new { JobId = jobId, Status = WizardJobStatusValues.Unknown },
                $"Job {jobId} is not running and no outcome is stored for it any more — it finished some time ago or was never started here. " +
                "Look at the open scenarios: a result waiting for acceptance is listed there, and its description notes a failed run or a skipped holistic harmonization.");
        }

        var payload = new
        {
            JobId = jobId,
            Status = state.Status,
            Reason = state.Reason,
            ScenarioId = state.Result?.FinalScenarioId,
            ScenarioToken = state.Result?.FinalScenarioToken,
            ScenarioName = state.Result?.FinalScenarioName,
            HarmonizationSkipped = state.Result?.HarmonizationSkipped ?? false,
            HarmonizationSkippedReason = state.Result?.HarmonizationSkippedReason
        };

        return SkillResult.SuccessResult(payload, BuildMessage(jobId, state.Status, state.Reason, state.Result));
    }

    private static string BuildMessage(Guid jobId, string status, string? reason, AutoWizardJobResultDto? result)
    {
        var scenarioNote = result?.FinalScenarioId is { } scenarioId
            ? $" Scenario '{result.FinalScenarioName}' (id {scenarioId}) is ready to be reviewed and accepted or rejected."
            : string.Empty;

        return status switch
        {
            WizardJobStatusValues.Completed when result is { HarmonizationSkipped: true } =>
                $"AutoWizard job {jobId} completed WITHOUT the holistic harmonization (stage 3 was skipped: {result.HarmonizationSkippedReason}). " +
                "The result is the harmonized plan of stages 1 and 2. Tell the user this honestly." + scenarioNote,
            WizardJobStatusValues.Completed =>
                $"AutoWizard job {jobId} completed all stages." + scenarioNote,
            WizardJobStatusValues.Failed =>
                $"AutoWizard job {jobId} FAILED: {reason} Tell the user the run failed." + scenarioNote,
            WizardJobStatusValues.Cancelled =>
                $"AutoWizard job {jobId} was cancelled." + scenarioNote,
            _ => $"AutoWizard job {jobId} ended with status '{status}'." + scenarioNote
        };
    }
}
