// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Read-only skill that lists the cut tree (split parts) of a shift order in a compact projection.
/// Accepts the order id (Bestellung), its plannable shift id (OriginalShift), or any split part id
/// (SplitShift) — the repository's CutList resolves all three to the same order before building the
/// flat cut list, so the caller does not need to know which kind of id it has.
/// </summary>
/// <param name="shiftId">UUID of the order, its plannable shift, or one of its split parts.</param>

using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("list_shift_cuts")]
public class ListShiftCutsSkill : BaseSkillImplementation
{
    private readonly IShiftRepository _shiftRepository;

    public ListShiftCutsSkill(IShiftRepository shiftRepository)
    {
        _shiftRepository = shiftRepository;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var shiftId = GetRequiredGuid(parameters, "shiftId");

        var loadedShift = await _shiftRepository.Get(shiftId);
        if (loadedShift == null)
        {
            return SkillResult.Error($"Shift with ID {shiftId} not found.");
        }

        var orderName = await ResolveOrderName(loadedShift, cancellationToken);

        var cuts = await _shiftRepository.CutList(shiftId);
        var pieces = cuts.Select(ToPieceSummary).ToList();

        if (pieces.Count == 0)
        {
            return SkillResult.SuccessResult(
                new { OrderReferenceId = shiftId, PieceCount = 0, Pieces = pieces },
                $"Order '{orderName}' has no split parts yet (still one plannable shift, or an unsealed draft). " +
                "Use cut_shift or cut_shift_by_date to split it.");
        }

        return SkillResult.SuccessResult(
            new { OrderReferenceId = shiftId, PieceCount = pieces.Count, Pieces = pieces },
            $"Order '{orderName}' has {pieces.Count} piece(s) in its cut tree.");
    }

    private async Task<string> ResolveOrderName(Shift loadedShift, CancellationToken cancellationToken)
    {
        var sealedOrderId = loadedShift.Status switch
        {
            ShiftStatus.SealedOrder => loadedShift.Id,
            ShiftStatus.OriginalShift => loadedShift.OriginalId,
            ShiftStatus.SplitShift => loadedShift.OriginalId,
            _ => (Guid?)null
        };

        if (sealedOrderId == null)
        {
            return loadedShift.Name;
        }

        var sealedOrder = await _shiftRepository.GetSealedOrder(sealedOrderId.Value);
        return sealedOrder?.Name ?? loadedShift.Name;
    }

    private static object ToPieceSummary(Shift shift)
    {
        return new
        {
            shift.Id,
            shift.Name,
            shift.Abbreviation,
            Status = ToStatusLabel(shift.Status),
            shift.FromDate,
            shift.UntilDate,
            StartShift = shift.StartShift.ToString("HH\\:mm"),
            EndShift = shift.EndShift.ToString("HH\\:mm"),
            Weekdays = ToWeekdayLabels(shift),
            shift.ParentId,
            shift.RootId,
            shift.OriginalId
        };
    }

    private static string ToStatusLabel(ShiftStatus status) => status switch
    {
        ShiftStatus.OriginalOrder => "order",
        ShiftStatus.SealedOrder => "sealed",
        ShiftStatus.OriginalShift => "plannable",
        ShiftStatus.SplitShift => "split",
        _ => status.ToString()
    };

    private static List<string> ToWeekdayLabels(Shift shift)
    {
        var labels = new List<string>();
        if (shift.IsMonday) labels.Add("Mon");
        if (shift.IsTuesday) labels.Add("Tue");
        if (shift.IsWednesday) labels.Add("Wed");
        if (shift.IsThursday) labels.Add("Thu");
        if (shift.IsFriday) labels.Add("Fri");
        if (shift.IsSaturday) labels.Add("Sat");
        if (shift.IsSunday) labels.Add("Sun");
        return labels;
    }
}
