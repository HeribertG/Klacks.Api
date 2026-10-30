// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Bulk skill with a preview and an apply (apply=false by default, see PreviewApplySkillCalls): cuts every plannable shift of
/// a group that runs on at least two weekdays into one part per weekday, so each part is its own plannable shift that can be
/// used in one container only. Which shifts are cut or skipped is decided by IShiftWeekdayCutPlanner (plannable shifts,
/// status OriginalShift, ShiftType IsTask, real plan, not sporadic, linked directly to the group; shifts with a single weekday,
/// with planned assignments, without a sealed order and orders that already have split parts are skipped and reported; shifts
/// the caller may not see are left out and neither cut nor counted). The cut goes through the same machinery as every cut (ShiftCutFacade, ShiftWeekdayCutBuilder): the plannable
/// shift is reduced to its first weekday and becomes a SplitShift, each further weekday becomes a new SplitShift cut from the
/// sealed order, and every part keeps customer, macro, groups with their validity, dates, times, time-range flag, staff count
/// and quantity; only the weekday flags differ. A cut always needs the user's confirmation (owner decision 2026-10-23): the
/// skill is Sensitive, so the autonomy gate holds every call, with or without apply, and shows the server preview of
/// SplitShiftsByWeekdayConfirmationPreviewProvider. apply=true runs only as the confirmed replay of that preview: without a
/// confirmed binding of kind WeekdayCut it refuses with BindingRequired before it reads anything, and when the fresh selection
/// no longer matches the binding it cuts nothing. With apply=true the whole run is ONE database transaction:
/// ShiftWeekdayCutVerifier re-reads every part untracked and any mismatch rolls the whole batch back. The result carries added
/// (new parts), verified (parts confirmed) and skipped.
/// </summary>
/// <param name="groupName">Required. Name of the group whose plannable shifts are cut, resolved by BundleGroupFilterResolver (an
/// exact name wins, an ambiguous or unknown name is refused with the candidates).</param>
/// <param name="apply">Optional. False (default) only reports what would be cut; true performs the cut.</param>
/// <param name="planner">Resolves the group into the shifts to cut and the ones to skip</param>
/// <param name="shiftReader">Reads the group links of the shifts and re-reads the parts for the verification</param>
/// <param name="shiftCutFacade">Writes the cut operations</param>
/// <param name="unitOfWork">Runs the whole cut in one transaction</param>

using System.Globalization;
using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Application.Exceptions;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services.Schedules;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation(SkillName)]
public class SplitShiftsByWeekdaySkill : BaseSkillImplementation
{
    public const string SkillName = "split_shifts_by_weekday";
    public const string GroupNameParameter = "groupName";

    private const int MaxListedShifts = 60;
    private const int MaxListedMismatches = 10;

    private readonly IShiftWeekdayCutPlanner _planner;
    private readonly IShiftReader _shiftReader;
    private readonly IShiftCutFacade _shiftCutFacade;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ShiftWeekdayCutVerifier _verifier;

    public SplitShiftsByWeekdaySkill(
        IShiftWeekdayCutPlanner planner,
        IShiftReader shiftReader,
        IShiftCutFacade shiftCutFacade,
        IUnitOfWork unitOfWork)
    {
        _planner = planner;
        _shiftReader = shiftReader;
        _shiftCutFacade = shiftCutFacade;
        _unitOfWork = unitOfWork;
        _verifier = new ShiftWeekdayCutVerifier(shiftReader);
    }

    internal sealed record CutOutcome(int OrdersCut, int PartsTotal, int PartsAdded, int PartsVerified);

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var groupName = GetRequiredString(parameters, GroupNameParameter);
        var apply = GetParameter<bool?>(parameters, PreviewApplySkillCalls.ApplyParameter) ?? false;

        if (apply && context.ConfirmedActionBinding is not { Kind: ConfirmationBindingKinds.WeekdayCut })
        {
            return SkillResult.Error(
                string.Format(CultureInfo.InvariantCulture, ConfirmationBindingMessages.BindingRequired, SkillName));
        }

        var selection = await _planner.PlanAsync(groupName, cancellationToken);
        if (selection.IsRefused)
        {
            return SkillResult.Error(selection.Refusal!);
        }

        if (!apply)
        {
            return BuildPreviewResult(selection);
        }

        var confirmed = context.ConfirmedActionBinding!;
        var current = ShiftWeekdayCutBinding.Of(selection);
        if (!confirmed.Matches(current))
        {
            return SkillResult.Error(ConfirmationBindingMessages.Describe(confirmed, current));
        }

        if (selection.ToCut.Count == 0)
        {
            return BuildNothingToCutResult(selection);
        }

        CutOutcome outcome;
        try
        {
            outcome = await _unitOfWork.ExecuteInTransactionAsync(() => CutAsync(selection.ToCut, cancellationToken));
        }
        catch (SkillVerificationException ex)
        {
            return SkillResult.Error(ex.Message);
        }
        catch (Exception ex) when (ex is InvalidOperationException or KeyNotFoundException or BadRequestException)
        {
            return SkillResult.Error($"The cut was refused and rolled back, nothing was changed: {ex.Message}");
        }

        return BuildAppliedResult(selection, outcome);
    }

    private async Task<CutOutcome> CutAsync(IReadOnlyList<Shift> toCut, CancellationToken cancellationToken)
    {
        var sourceLinks = (await _shiftReader.GetGroupLinksByShiftIdsAsync(toCut.Select(s => s.Id).ToList(), cancellationToken))
            .GroupBy(link => link.ShiftId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<ShiftGroupLink>)g.ToList());

        var plans = toCut
            .Select(shift => ShiftWeekdayCutBuilder.Build(
                shift,
                shift.OriginalId!.Value,
                ToGroupResources(sourceLinks.GetValueOrDefault(shift.Id) ?? [])))
            .ToList();

        await _shiftCutFacade.ProcessBatchCutsAsync(plans.SelectMany(plan => plan.Operations).ToList());

        var mismatches = await _verifier.FindMismatchesAsync(
            plans,
            toCut.ToDictionary(s => s.Id, s => sourceLinks.GetValueOrDefault(s.Id) ?? (IReadOnlyList<ShiftGroupLink>)[]),
            cancellationToken);
        if (mismatches.Count > 0)
        {
            throw new SkillVerificationException(
                SkillName,
                "Database verification failed after the cut — the whole batch was rolled back and nothing was changed: " +
                string.Join(" ", mismatches.Take(MaxListedMismatches)));
        }

        var partsTotal = plans.Sum(plan => plan.Parts.Count);
        return new CutOutcome(plans.Count, partsTotal, plans.Sum(plan => plan.Parts.Count(part => !part.Converted)), partsTotal);
    }

    private static List<SimpleGroupResource> ToGroupResources(IReadOnlyList<ShiftGroupLink> links) =>
        links.Select(link => new SimpleGroupResource
        {
            Id = link.GroupId,
            ValidFrom = link.ValidFrom ?? DateTime.MinValue,
            ValidUntil = link.ValidUntil
        }).ToList();

    private static object ListShifts(IEnumerable<Shift> shifts) =>
        shifts.Take(MaxListedShifts).Select(shift => new
        {
            ShiftId = shift.Id,
            shift.Name,
            Weekdays = ShiftWeekdayCutBuilder.ActiveIsoWeekdays(shift)
        }).ToList();

    private static object ListSkipped(IEnumerable<ShiftWeekdayCutSkip> skipped) =>
        skipped.Take(MaxListedShifts).Select(entry => new { entry.ShiftId, entry.Name, entry.Reason }).ToList();

    private static int PartCount(IEnumerable<Shift> shifts) =>
        shifts.Sum(shift => ShiftWeekdayCutBuilder.ActiveIsoWeekdays(shift).Count);

    private static SkillResult BuildPreviewResult(ShiftWeekdayCutSelection selection)
    {
        var parts = PartCount(selection.ToCut);
        var data = new
        {
            Applied = false,
            selection.GroupName,
            OrdersToCut = selection.ToCut.Count,
            PartsAfterCut = parts,
            SkippedCount = selection.Skipped.Count,
            ToCut = ListShifts(selection.ToCut),
            Skipped = ListSkipped(selection.Skipped)
        };

        return SkillResult.SuccessResult(
            data,
            $"Preview for the group '{selection.GroupName}'. Nothing was changed yet: {selection.ToCut.Count} plannable " +
            $"shift(s) run on several weekdays and would be cut into {parts} part(s), one per weekday; " +
            $"{selection.Skipped.Count} shift(s) are skipped (single weekday, already split, sporadic, with planned " +
            "assignments). Tell the user these counts. A cut always needs the user's confirmation: when the user " +
            "wants the cut, call this skill again with apply=true - the server then holds the call, shows the exact preview " +
            "and the cut runs only after the user confirmed it.");
    }

    private static SkillResult BuildNothingToCutResult(ShiftWeekdayCutSelection selection) =>
        SkillResult.SuccessResult(
            new
            {
                Applied = true,
                selection.GroupName,
                Added = 0,
                Verified = 0,
                SkippedCount = selection.Skipped.Count,
                Skipped = ListSkipped(selection.Skipped)
            },
            $"Nothing to cut in the group '{selection.GroupName}': no plannable shift runs on several weekdays that is " +
            $"still uncut. {selection.Skipped.Count} shift(s) were skipped. Nothing was changed.");

    private static SkillResult BuildAppliedResult(ShiftWeekdayCutSelection selection, CutOutcome outcome) =>
        SkillResult.SuccessResult(
            new
            {
                Applied = true,
                selection.GroupName,
                OrdersCut = outcome.OrdersCut,
                Added = outcome.PartsAdded,
                Verified = outcome.PartsVerified,
                PartsTotal = outcome.PartsTotal,
                SkippedCount = selection.Skipped.Count,
                Skipped = ListSkipped(selection.Skipped)
            },
            $"Cut {outcome.OrdersCut} plannable shift(s) of the group '{selection.GroupName}' into {outcome.PartsTotal} part(s), one " +
            $"per weekday ({outcome.PartsAdded} added, {outcome.PartsVerified} confirmed in the database, verified); " +
            $"{selection.Skipped.Count} shift(s) were skipped. Tell the user added, verified and skipped.");
}
