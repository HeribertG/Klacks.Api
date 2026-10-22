// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Sets (upserts) a qualification a shift requires, with a minimum level, whether it is mandatory and whether the
/// minimum level is binding. Thin wrapper around
/// <see cref="Klacks.Api.Application.Commands.Qualifications.SetShiftRequiredQualificationCommand"/>.
/// A mandatory requirement makes employees without it ineligible, and employees below the minimum level only when
/// the level is binding (otherwise a warning) — enforced by find_replacement and the pre-commit guardrail. Calling it
/// again for the same shift + qualification updates the existing entry; an omitted levelBindingOverride keeps the
/// entry's current choice (a new entry inherits).
/// </summary>
/// <param name="shiftId">Required. UUID of the shift.</param>
/// <param name="qualificationId">Required. UUID of an existing qualification.</param>
/// <param name="minLevel">Required. Minimum level 1=Low, 2=Basic, 3=Proficient, 4=Advanced, 5=Expert.</param>
/// <param name="isMandatory">Optional, default true. True = blocks ineligible employees; false = soft signal.</param>
/// <param name="levelBindingOverride">Optional. inherit = as the qualification, binding = below the level blocks, hint = below the level only warns.</param>

using Klacks.Api.Application.Commands.Qualifications;
using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

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

        var levelBindingRaw = GetParameter<string>(parameters, "levelBindingOverride");
        var keepExistingLevelBinding = string.IsNullOrWhiteSpace(levelBindingRaw);
        bool? levelBindingOverride = null;
        if (!keepExistingLevelBinding && !LevelBindingOverrideSkillValues.TryParse(levelBindingRaw!, out levelBindingOverride))
        {
            return SkillResult.Error(
                $"levelBindingOverride must be one of: {string.Join(", ", LevelBindingOverrideSkillValues.All)}.");
        }

        try
        {
            var id = await _mediator.Send(
                new SetShiftRequiredQualificationCommand(
                    shiftId, qualificationId, isMandatory, (QualificationLevel)minLevel, levelBindingOverride, keepExistingLevelBinding),
                cancellationToken);

            var levelBinding = keepExistingLevelBinding ? null : LevelBindingOverrideSkillValues.Describe(levelBindingOverride);
            var levelBindingNote = levelBinding is null ? string.Empty : $", level binding: {levelBinding}";
            return SkillResult.SuccessResult(
                new { Id = id, ShiftId = shiftId, QualificationId = qualificationId, IsMandatory = isMandatory, MinLevel = ((QualificationLevel)minLevel).ToString(), LevelBindingOverride = levelBinding },
                $"Shift {shiftId} now requires the qualification at minimum level {(QualificationLevel)minLevel} (mandatory: {isMandatory}{levelBindingNote}).");
        }
        catch (Exception ex) when (ex is KeyNotFoundException || ex is DatabaseUpdateException { IsForeignKeyViolation: true })
        {
            return SkillResult.Error("Could not set the required qualification — the shift or qualification id may not exist.");
        }
    }
}
