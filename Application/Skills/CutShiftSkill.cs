// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Skill that cuts ONE existing plannable shift (OriginalShift, e.g. a 24h order 07:00-07:00)
/// into several time parts (split shifts).
/// SPLIT RULE: the plannable shift itself is REDUCED to the first part — its end becomes the start
/// of the next part — and its status changes OriginalShift(2) -> SplitShift(3). The remaining parts
/// are created. So no duplicate plannable shift is left over next to its parts.
/// The order (Bestellung / SealedOrder, status 1) is IMMUTABLE and is NEVER changed by a cut.
/// This is the correct way to model an early/late/night rotation: create ONE order, seal it, then cut its plannable shift here.
/// The plan (parsing, target, tiling, operations) comes from IShiftTimeCutPlanner. A cut always needs the user's
/// confirmation (owner decision 2026-10-23): it runs only as the confirmed replay of its server preview
/// (CutShiftConfirmationPreviewProvider). The binding of that preview (ShiftTimeCutBinding: plannable shift, parts, default
/// expenses) travels with the token, confirm_pending_action hands it over, and the skill cuts only when its fresh plan still
/// matches it; without a binding it refuses before it reads anything. The result names the default expenses every part
/// carries (each part owns its copy, ShiftCutFacade), so the user is told what every part is booked with.
/// </summary>
/// <param name="shiftId">UUID of the plannable shift (OriginalShift) or its order to cut.</param>
/// <param name="parts">Comma-separated time ranges "HH:mm-HH:mm" that tile the order span (e.g. "07:00-15:00,15:00-23:00,23:00-07:00").</param>
/// <param name="partNames">Optional comma-separated names for the parts, aligned by index (e.g. "Frühdienst,Spätdienst,Nachtdienst").</param>

using System.Globalization;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation(SkillName)]
public class CutShiftSkill : BaseSkillImplementation
{
    public const string SkillName = "cut_shift";
    public const string ShiftIdParameter = "shiftId";
    public const string PartsParameter = "parts";
    public const string PartNamesParameter = "partNames";

    private readonly IShiftTimeCutPlanner _planner;
    private readonly IShiftCutFacade _shiftCutFacade;

    public CutShiftSkill(IShiftTimeCutPlanner planner, IShiftCutFacade shiftCutFacade)
    {
        _planner = planner;
        _shiftCutFacade = shiftCutFacade;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        if (context.ConfirmedActionBinding is not { Kind: ConfirmationBindingKinds.ShiftCut } confirmed)
        {
            return SkillResult.Error(string.Format(CultureInfo.InvariantCulture, ConfirmationBindingMessages.BindingRequired, SkillName));
        }

        var shiftId = GetRequiredGuid(parameters, ShiftIdParameter);
        var partsRaw = GetRequiredString(parameters, PartsParameter);
        var namesRaw = GetParameter<string>(parameters, PartNamesParameter) ?? string.Empty;

        var plan = await _planner.PlanAsync(shiftId, partsRaw, namesRaw, context.UserLanguage, cancellationToken);
        if (plan.IsRefused)
        {
            return SkillResult.Error(plan.Refusal!);
        }

        var current = ShiftTimeCutBinding.Of(plan);
        if (!confirmed.Matches(current))
        {
            return SkillResult.Error(ConfirmationBindingMessages.Describe(confirmed, current));
        }

        var results = await _shiftCutFacade.ProcessBatchCutsAsync(plan.Operations.ToList());

        return BuildSuccessResult(plan, results);
    }

    private static SkillResult BuildSuccessResult(ShiftTimeCutPlan plan, List<Shift> results)
    {
        var summaries = plan.Parts.Select(part => new
        {
            part.Id,
            part.Name,
            StartTime = FormatTime(part.Start),
            EndTime = FormatTime(part.End),
            part.WorkTime,
            part.CuttingAfterMidnight,
            part.Converted
        }).ToList();

        var resultData = new
        {
            SealedOrderId = (Guid?)plan.SealedOrderId,
            OrderName = plan.OriginalShift!.Name,
            PartCount = results.Count,
            GroupCount = plan.Groups.Count,
            Parts = summaries
        };

        return SkillResult.SuccessResult(
            resultData,
            $"Cut the plannable shift '{plan.OriginalShift.Name}' into {results.Count} part(s): " +
            string.Join(", ", plan.Parts.Select(part => part.Name)) +
            ". The first part reuses the plannable shift (reduced, status -> SplitShift); " +
            "the order (Bestellung) itself stays unchanged. Default expenses booked with every work of a part - " +
            string.Join("; ", plan.Parts.Select(part =>
                $"'{part.Name}': {CutPieceExpenseSummary.Describe(DefaultExpensesOf(results, part.Id))}")) +
            ". Tell the user these expenses per part.");
    }

    private static List<ShiftExpenses> DefaultExpensesOf(IEnumerable<Shift> results, Guid partId)
        => results.FirstOrDefault(shift => shift.Id == partId)?.ShiftExpenses ?? [];

    private static string FormatTime(TimeOnly time) => time.ToString(DateTimeFormats.HourMinute, CultureInfo.InvariantCulture);
}
