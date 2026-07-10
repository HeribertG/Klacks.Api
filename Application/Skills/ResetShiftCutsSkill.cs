// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Skill that merges every split part of a shift order back into one plannable shift (OriginalShift)
/// starting at a new date: split parts still open at/after that date are closed the day before, and a
/// fresh full-span plannable shift is created from the order's own template starting on that date. Cuts
/// that already ended before the new date are left untouched. Wraps GetResetDateRangeQuery (to validate
/// the requested date against the order's own validity range) and PostResetCutsCommand (to perform the
/// merge), then re-reads the cut list to confirm the merge actually happened.
/// </summary>
/// <param name="shiftId">UUID of the order (Bestellung), its plannable shift, or one of its split parts — resolved to the order.</param>
/// <param name="newStartDate">ISO date (yyyy-MM-dd) the merged plannable shift starts from; must lie within the order's own validity range.</param>

using Klacks.Api.Application.Commands.Shifts;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Shifts;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("reset_shift_cuts")]
public class ResetShiftCutsSkill : BaseSkillImplementation
{
    private readonly IShiftRepository _shiftRepository;
    private readonly IMediator _mediator;

    public ResetShiftCutsSkill(IShiftRepository shiftRepository, IMediator mediator)
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
        var newStartDate = GetParameter<DateOnly?>(parameters, "newStartDate")
            ?? throw new ArgumentException("Required parameter 'newStartDate' is missing");

        var loadedShift = await _shiftRepository.Get(shiftId);
        if (loadedShift == null)
        {
            return SkillResult.Error($"Shift with ID {shiftId} not found.");
        }

        var sealedOrderId = loadedShift.Status switch
        {
            ShiftStatus.SealedOrder => loadedShift.Id,
            ShiftStatus.OriginalShift => loadedShift.OriginalId,
            ShiftStatus.SplitShift => loadedShift.OriginalId,
            _ => (Guid?)null
        };

        if (sealedOrderId == null)
        {
            return SkillResult.Error(
                $"Shift '{loadedShift.Name}' has status {loadedShift.Status} and has no order reference; reset needs a sealed order.");
        }

        var sealedOrder = await _shiftRepository.GetSealedOrder(sealedOrderId.Value);
        var orderName = sealedOrder?.Name ?? loadedShift.Name;

        var isSplit = await _shiftRepository.GetQuery()
            .AnyAsync(s => s.OriginalId == sealedOrderId && s.Status == ShiftStatus.SplitShift, cancellationToken);
        if (!isSplit)
        {
            return SkillResult.Error(
                $"Order '{orderName}' is not split into parts; there is nothing to reset. Use cut_shift or cut_shift_by_date first.");
        }

        var range = await _mediator.Send(new GetResetDateRangeQuery(sealedOrderId.Value), cancellationToken);

        if (newStartDate < range.EarliestResetDate)
        {
            return SkillResult.Error(
                $"newStartDate ({newStartDate}) cannot be earlier than the order's start ({range.EarliestResetDate}).");
        }

        if (range.UntilDate.HasValue && newStartDate > range.UntilDate.Value)
        {
            return SkillResult.Error(
                $"newStartDate ({newStartDate}) cannot be later than the order's end ({range.UntilDate.Value}).");
        }

        await _mediator.Send(new PostResetCutsCommand(sealedOrderId.Value, newStartDate), cancellationToken);

        var freshCuts = await _shiftRepository.CutList(sealedOrderId.Value);

        var mergedShift = freshCuts.FirstOrDefault(
            s => s.Status == ShiftStatus.OriginalShift && s.FromDate == newStartDate);
        var staleOpenSplit = freshCuts.FirstOrDefault(
            s => s.Status == ShiftStatus.SplitShift && (!s.UntilDate.HasValue || s.UntilDate.Value >= newStartDate));

        var verified = mergedShift != null && staleOpenSplit == null;

        if (!verified)
        {
            return SkillResult.Error(
                $"Database verification failed: could not confirm the merge of order '{orderName}' from {newStartDate} " +
                "in the database after the write. The write may still have been applied (this skill does not roll back); " +
                "check the cut list (list_shift_cuts) before retrying.");
        }

        return SkillResult.SuccessResult(
            new
            {
                SealedOrderId = sealedOrderId,
                OrderName = orderName,
                NewStartDate = newStartDate,
                MergedShiftId = mergedShift!.Id,
                RemainingPieceCount = freshCuts.Count,
                Verified = true
            },
            $"Order '{orderName}' merged back into one plannable shift starting {newStartDate}. " +
            "Cuts before this date are unchanged. Confirmed in the database (verified).");
    }
}
