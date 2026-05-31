// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Soft-deletes a shift. Refuses when the shift has cuts (a nested-set sub-shift tree) — the product
/// has no bulk-delete for a cut group; a cut group is ended via the cut editor's Reset (sets an end
/// date), so the skill redirects there instead of inventing a cascade delete. Also refuses when the
/// shift still has works: get_schedule_entries joins the shift table without a soft-delete filter, so
/// a deleted shift with works would keep rendering against a dead definition. Both cases are reported
/// as an actionable message. Dispatches DeleteCommand&lt;ShiftResource&gt;.
/// </summary>
/// <param name="shiftId">Required. UUID of the shift to delete.</param>

using Klacks.Api.Application.Commands;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("delete_shift")]
public class DeleteShiftSkill : BaseSkillImplementation
{
    private readonly IShiftRepository _shiftRepository;
    private readonly IMediator _mediator;

    public DeleteShiftSkill(
        IShiftRepository shiftRepository,
        IMediator mediator)
    {
        _shiftRepository = shiftRepository;
        _mediator = mediator;
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
