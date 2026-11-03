// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Skill that puts a split shift order back to its state at creation: every cut piece is removed and one plannable shift
/// (OriginalShift) with the content of the order remains. Without planned work it starts on the first day of the order,
/// whatever date was asked for. With real work the date must lie after the last work: a piece with such work stays, ending the
/// day before, and the plannable shift starts on the date. Wraps GetResetDateRangeQuery (to validate the requested date) and
/// PostResetCutsCommand (to perform the reset) and relays a refusal as a skill error, then re-reads the cut list to confirm
/// exactly one open plannable shift and no piece reaching into it. A shift with an analyse token is answered like a
/// missing one: skills work on the real plan only, and a variant (key) has no skills.
/// </summary>
/// <param name="shiftId">UUID of the order (Bestellung), its plannable shift, or one of its split parts — resolved to the order.</param>
/// <param name="newStartDate">ISO date (yyyy-MM-dd) from which the shift is whole again; it counts only when the order has real work, and must lie after the last work and inside the order.</param>

using Klacks.Api.Application.Commands.Shifts;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Exceptions;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Shifts;
using Klacks.Api.Application.Services.Shifts;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("reset_shift_cuts")]
public class ResetShiftCutsSkill : BaseSkillImplementation
{
    private readonly IShiftRepository _shiftRepository;
    private readonly IShiftReader _shiftReader;
    private readonly IMediator _mediator;

    private readonly IShiftVisibilityGuard _shiftVisibilityGuard;

    public ResetShiftCutsSkill(
        IShiftRepository shiftRepository,
        IShiftReader shiftReader,
        IMediator mediator,
        IShiftVisibilityGuard shiftVisibilityGuard)
    {
        _shiftRepository = shiftRepository;
        _shiftReader = shiftReader;
        _mediator = mediator;
        _shiftVisibilityGuard = shiftVisibilityGuard;
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
        if (loadedShift == null
            || loadedShift.AnalyseToken != null
            || !await _shiftVisibilityGuard.IsVisibleAsync(loadedShift, cancellationToken))
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

        var isSplit = await _shiftReader.AnyByOriginalIdAsync(
            sealedOrderId.Value, ShiftStatus.SplitShift, cancellationToken);
        if (!isSplit)
        {
            return SkillResult.Error(
                $"Order '{orderName}' is not split into parts; there is nothing to reset. Use cut_shift or cut_shift_by_date first.");
        }

        ResetDateRangeResponse range;
        try
        {
            range = await _mediator.Send(new GetResetDateRangeQuery(sealedOrderId.Value), cancellationToken);
        }
        catch (KeyNotFoundException)
        {
            return SkillResult.Error($"Shift with ID {shiftId} not found.");
        }

        if (newStartDate < range.EarliestResetDate)
        {
            return SkillResult.Error(
                $"newStartDate ({newStartDate}) cannot be earlier than the earliest allowed day ({range.EarliestResetDate}): the start of the order, or the day after the last planned work.");
        }

        if (range.UntilDate.HasValue && newStartDate > range.UntilDate.Value)
        {
            return SkillResult.Error(
                $"newStartDate ({newStartDate}) cannot be later than the order's end ({range.UntilDate.Value}).");
        }

        try
        {
            await _mediator.Send(new PostResetCutsCommand(sealedOrderId.Value, newStartDate), cancellationToken);
        }
        catch (ShiftResetRefusedException ex)
        {
            return SkillResult.Error(ex.Message);
        }
        catch (BadRequestException ex)
        {
            return SkillResult.Error(ex.Message);
        }
        catch (InvalidOperationException ex)
            when (ex.Message == string.Format(ShiftVisibilityMessages.NoSealedOrderFound, sealedOrderId.Value))
        {
            return SkillResult.Error($"Shift with ID {shiftId} not found.");
        }

        var freshCuts = await _shiftRepository.CutList(sealedOrderId.Value);

        var openPlannable = freshCuts
            .Where(s => s.Status == ShiftStatus.OriginalShift && (!s.UntilDate.HasValue || s.UntilDate.Value >= newStartDate))
            .ToList();
        var mergedShift = openPlannable.Count == 1 ? openPlannable[0] : null;
        var keptPieces = freshCuts.Where(s => s.Status == ShiftStatus.SplitShift).ToList();
        var staleOpenSplit = mergedShift != null
            && keptPieces.Any(s => !s.UntilDate.HasValue || s.UntilDate.Value >= mergedShift.FromDate);

        if (mergedShift == null || staleOpenSplit)
        {
            return SkillResult.Error(
                $"Database verification failed: could not confirm the reset of order '{orderName}' " +
                "in the database after the write. The write may still have been applied (this skill does not roll back); " +
                "check the cut list (list_shift_cuts) before retrying.");
        }

        return SkillResult.SuccessResult(
            new
            {
                SealedOrderId = sealedOrderId,
                OrderName = orderName,
                NewStartDate = mergedShift.FromDate,
                MergedShiftId = mergedShift.Id,
                RemainingPieceCount = keptPieces.Count,
                Verified = true
            },
            $"Order '{orderName}' is back to one plannable shift starting {mergedShift.FromDate}, with the content of the order. " +
            $"All cut pieces were removed except {keptPieces.Count} with planned work before that date, which end the day before. " +
            "Confirmed in the database (verified).");
    }
}
