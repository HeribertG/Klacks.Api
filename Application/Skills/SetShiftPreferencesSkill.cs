// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Sets (upserts) a single employee's preference for a shift — Preferred (the planner favours this
/// employee for the shift) or Blacklist (the employee is excluded from it). Thin wrapper around
/// <see cref="Klacks.Api.Application.Commands.ClientShiftPreferences.SetShiftPreferenceCommand"/>.
/// Only this one preference is changed; the employee's other preferences are left untouched. Consumed
/// by Wizard 1 (Stage-1 "preferred" soft rule) and find_replacement (preferred ranking / blacklist
/// exclusion). Calling it again for the same employee + shift updates the existing entry.
/// </summary>
/// <param name="clientId">Required. UUID of the employee.</param>
/// <param name="shiftId">Required. UUID of the shift.</param>
/// <param name="preference">Required. "preferred" or "blacklist".</param>

using Klacks.Api.Application.Commands.ClientShiftPreferences;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("set_shift_preferences")]
public class SetShiftPreferencesSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public SetShiftPreferencesSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var clientId = GetRequiredGuid(parameters, "clientId");
        var shiftId = GetRequiredGuid(parameters, "shiftId");
        var preferenceRaw = GetRequiredString(parameters, "preference");

        if (!Enum.TryParse<ShiftPreferenceType>(preferenceRaw, ignoreCase: true, out var preferenceType)
            || !Enum.IsDefined(typeof(ShiftPreferenceType), preferenceType))
        {
            return SkillResult.Error("preference must be 'preferred' or 'blacklist'.");
        }

        try
        {
            var id = await _mediator.Send(
                new SetShiftPreferenceCommand(clientId, shiftId, preferenceType), cancellationToken);

            return SkillResult.SuccessResult(
                new { Id = id, ClientId = clientId, ShiftId = shiftId, Preference = preferenceType.ToString() },
                $"Employee {clientId} is now {preferenceType} for shift {shiftId}.");
        }
        catch (DbUpdateException)
        {
            return SkillResult.Error("Could not set the preference — the employee or shift id may not exist.");
        }
    }
}
