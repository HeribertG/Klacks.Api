// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Skill that cancels a running wizard job by id. Tries all three wizard runners + the AutoWizard
/// orchestrator until one acknowledges the cancellation.
/// </summary>

using Klacks.Api.Application.Services.Schedules;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Application.Services.Schedules.AutoWizard;
using Klacks.Api.Application.Interfaces.Schedules.AutoWizard;
using Klacks.Api.Application.Services.Schedules.HolisticHarmonizer;
using Klacks.Api.Application.Interfaces.Schedules.HolisticHarmonizer;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("cancel_wizard_job")]
public class CancelWizardJobSkill : BaseSkillImplementation
{
    private readonly IWizardJobRunner _wizard;
    private readonly IHarmonizerJobRunner _harmonizer;
    private readonly IHolisticHarmonizerJobRunner _holistic;
    private readonly IAutoWizardJobRunner _autoWizard;

    public CancelWizardJobSkill(
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
        var jobId = GetRequiredGuid(parameters, "jobId");

        var cancelled =
            _autoWizard.TryCancel(jobId) ||
            _wizard.TryCancel(jobId) ||
            _harmonizer.TryCancel(jobId) ||
            _holistic.TryCancel(jobId);

        if (!cancelled)
        {
            return Task.FromResult(SkillResult.Error(
                $"Job {jobId} is unknown or already finished — nothing was cancelled."));
        }

        return Task.FromResult(SkillResult.SuccessResult(
            new { JobId = jobId, Cancelled = true },
            $"Wizard job {jobId} cancelled."));
    }
}
