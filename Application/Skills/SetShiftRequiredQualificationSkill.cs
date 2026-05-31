// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Sets (upserts) a qualification a shift requires, with a minimum level and whether it is mandatory.
/// Thin wrapper around <see cref="Klacks.Api.Application.Commands.Qualifications.SetShiftRequiredQualificationCommand"/>.
/// A mandatory requirement makes employees without it (at the minimum level) ineligible — enforced by
/// find_replacement and the pre-commit guardrail. Calling it again for the same shift + qualification
/// updates the existing entry.
/// </summary>
/// <param name="shiftId">Required. UUID of the shift.</param>
/// <param name="qualificationId">Required. UUID of an existing qualification.</param>
/// <param name="minLevel">Required. Minimum level 1=Low, 2=Basic, 3=Proficient, 4=Advanced, 5=Expert.</param>
/// <param name="isMandatory">Optional, default true. True = blocks ineligible employees; false = soft signal.</param>

using Klacks.Api.Application.Commands.Qualifications;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("set_shift_required_qualification")]
public class SetShiftRequiredQualificationSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public SetShiftRequiredQualificationSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var shiftId = GetRequiredGuid(parameters, "shiftId");
        var qualificationId = GetRequiredGuid(parameters, "qualificationId");
        var minLevel = GetRequiredInt(parameters, "minLevel");
        if (!Enum.IsDefined(typeof(QualificationLevel), minLevel))
        {
            return SkillResult.Error("minLevel must be between 1 (Low) and 5 (Expert).");
        }

        var isMandatoryRaw = GetParameter<string>(parameters, "isMandatory");
        var isMandatory = string.IsNullOrWhiteSpace(isMandatoryRaw)
            || !bool.TryParse(isMandatoryRaw, out var parsed)
            || parsed;

        try
        {
            var id = await _mediator.Send(
                new SetShiftRequiredQualificationCommand(
                    shiftId, qualificationId, isMandatory, (QualificationLevel)minLevel),
                cancellationToken);

            return SkillResult.SuccessResult(
                new { Id = id, ShiftId = shiftId, QualificationId = qualificationId, IsMandatory = isMandatory, MinLevel = ((QualificationLevel)minLevel).ToString() },
                $"Shift {shiftId} now requires the qualification at minimum level {(QualificationLevel)minLevel} (mandatory: {isMandatory}).");
        }
        catch (DbUpdateException)
        {
            return SkillResult.Error("Could not set the required qualification — the shift or qualification id may not exist.");
        }
    }
}
