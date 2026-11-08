// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Skill that cancels a running wizard job by id. Tries the AutoWizard orchestrator and, for admins only, the three
/// single-stage wizard runners of the developer dropdown until one acknowledges the cancellation. A job the caller may not
/// address (IAutoWizardJobAccessGuard: a restricted caller only reaches AutoWizard jobs of visible groups and employees) is
/// answered like an unknown one.
/// </summary>

using Klacks.Api.Application.Services.Schedules;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Application.Services.Schedules.AutoWizard;
using Klacks.Api.Application.Interfaces.Schedules.AutoWizard;
using Klacks.Api.Application.Services.Schedules.HolisticHarmonizer;
using Klacks.Api.Application.Interfaces.Schedules.HolisticHarmonizer;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Constants;
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
    private readonly IAutoWizardJobAccessGuard _jobAccessGuard;

    public CancelWizardJobSkill(
        IWizardJobRunner wizard,
        IHarmonizerJobRunner harmonizer,
        IHolisticHarmonizerJobRunner holistic,
        IAutoWizardJobRunner autoWizard,
        IAutoWizardJobAccessGuard jobAccessGuard)
    {
        _wizard = wizard;
        _harmonizer = harmonizer;
        _holistic = holistic;
        _autoWizard = autoWizard;
        _jobAccessGuard = jobAccessGuard;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var jobId = GetRequiredGuid(parameters, "jobId");

        var isAdmin = context.UserPermissions.Contains(Roles.Admin);
        var cancelled =
            await _jobAccessGuard.CanAccessAsync(jobId, cancellationToken) &&
            (_autoWizard.TryCancel(jobId) ||
             (isAdmin && (_wizard.TryCancel(jobId) || _harmonizer.TryCancel(jobId) || _holistic.TryCancel(jobId))));

        if (!cancelled)
        {
            return SkillResult.Error(
                $"Job {jobId} is unknown or already finished — nothing was cancelled.");
        }

        return SkillResult.SuccessResult(
            new { JobId = jobId, Cancelled = true },
            $"Wizard job {jobId} cancelled.");
    }
}
