// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Soft-deletes a draft order. Every shift that is no longer a draft order (sealed order, plannable shift, cut piece) is
/// refused by the shift delete handler with ShiftLockedException, because the edit form offers no delete for them either;
/// the actionable cut and works refusals below come first. The one exception is an administrator deleting a sealed order:
/// the whole order family (plannable copy and cut pieces included) is deleted, but only while no work, scenario copy or
/// container reference exists anywhere in the family. The administrator role is read from the skill context, not from the
/// web request, so the chat channels behave the same. Refuses when the shift has cuts (a nested-set sub-shift tree) — the
/// product has no bulk-delete for a cut group; a cut group is ended via the cut editor's Reset (sets an end
/// date), so the skill redirects there instead of inventing a cascade delete. Also refuses when the
/// shift still has works: get_schedule_entries joins the shift table without a soft-delete filter, so
/// a deleted shift with works would keep rendering against a dead definition. Both cases are reported
/// as an actionable message. Dispatches DeleteCommand&lt;ShiftResource&gt;.
/// </summary>
/// <param name="shiftId">Required. UUID of the shift to delete.</param>

using Klacks.Api.Application.Commands;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Domain.Services.Shifts;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("delete_shift")]
public class DeleteShiftSkill : BaseSkillImplementation
{
    private readonly IShiftRepository _shiftRepository;
    private readonly IMediator _mediator;
    private readonly ISealedOrderFamilyDeletion _familyDeletion;

    public DeleteShiftSkill(
        IShiftRepository shiftRepository,
        IMediator mediator,
        ISealedOrderFamilyDeletion familyDeletion)
    {
        _shiftRepository = shiftRepository;
        _mediator = mediator;
        _familyDeletion = familyDeletion;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var shiftId = GetRequiredGuid(parameters, "shiftId");
        var existing = await _shiftRepository.Get(shiftId);
        if (existing == null)
        {
            return SkillResult.Error($"Shift {shiftId} not found.");
        }

        if (LockedShiftEditPolicy.IsOrderFamilyDeletable(existing) && context.UserPermissions.Contains(Roles.Admin))
        {
            var deleted = await _familyDeletion.DeleteAsync(existing, cancellationToken);
            return SkillResult.SuccessResult(
                new { existing.Id, existing.Name, DeletedShifts = deleted },
                $"Sealed order '{existing.Name}' deleted together with its {deleted - 1} piece(s).");
        }

        if (existing.Lft.HasValue && existing.Rgt.HasValue && existing.Rgt - existing.Lft > 1)
        {
            return SkillResult.Error("This shift has cuts (a sub-shift tree) and cannot be deleted directly — the system has no bulk-delete for a cut group. Open the cut editor for this shift and end the group via 'Reset' (sets an end date) instead.");
        }

        if (await _shiftRepository.HasActiveWorksAsync(shiftId, cancellationToken))
        {
            return SkillResult.Error("This shift still has works assigned; reassign or remove them before deleting the shift.");
        }

        var result = await _mediator.Send(new DeleteCommand<ShiftResource>(shiftId), cancellationToken);
        if (result == null)
        {
            return SkillResult.Error($"Shift {shiftId} could not be deleted.");
        }

        return SkillResult.SuccessResult(
            new { result.Id, result.Name },
            $"Shift '{result.Name}' deleted.");
    }
}
