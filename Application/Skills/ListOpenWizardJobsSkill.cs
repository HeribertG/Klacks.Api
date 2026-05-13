// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Skill that asks each wizard registry whether a given jobId is still running.
/// Returns the live status across the four runners (AutoWizard / Wizard 1 / Wizard 2 / Wizard 3).
/// </summary>
/// <param name="jobId">Optional UUID; if omitted, returns "no concrete job — registries don't expose enumeration".</param>

using Klacks.Api.Application.Services.Schedules;
using Klacks.Api.Application.Services.Schedules.AutoWizard;
using Klacks.Api.Application.Services.Schedules.HolisticHarmonizer;
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

    public ListOpenWizardJobsSkill(
        IWizardJobRunner wizard,
        IHarmonizerJobRunner harmonizer,
        IHolisticHarmonizerJobRunner holistic,
        IAutoWizardJobRunner autoWizard)
    {
        _wizard = wizard;
        _harmonizer = harmonizer;
        _holistic = holistic;
        _autoWizard = autoWizard;
    }

    public override Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var jobIdRaw = GetParameter<string>(parameters, "jobId");
        if (string.IsNullOrWhiteSpace(jobIdRaw))
        {
            return Task.FromResult(SkillResult.SuccessResult(
                new
                {
                    Hint = "Pass a specific jobId. The wizard registries are in-memory ConcurrentDictionaries " +
                           "without an enumeration API; once a job finishes it disappears."
                },
                "list_open_wizard_jobs needs an explicit jobId — wizard registries don't expose a full enumeration."));
        }

        if (!Guid.TryParse(jobIdRaw, out var jobId))
        {
            return Task.FromResult(SkillResult.Error($"Invalid jobId '{jobIdRaw}'. Expected UUID."));
        }

        var status = new
        {
            JobId = jobId,
            IsAutoWizard = _autoWizard.IsRunning(jobId),
            IsWizard1Planner = _wizard.IsRunning(jobId),
            IsWizard2Harmonizer = _harmonizer.IsRunning(jobId),
            IsWizard3Holistic = _holistic.IsRunning(jobId)
        };
        var anyRunning = status.IsAutoWizard || status.IsWizard1Planner || status.IsWizard2Harmonizer || status.IsWizard3Holistic;

        return Task.FromResult(SkillResult.SuccessResult(
            status,
            anyRunning
                ? $"Job {jobId} is still running."
                : $"Job {jobId} is not in any wizard registry — either it finished or was never started here."));
    }
}
