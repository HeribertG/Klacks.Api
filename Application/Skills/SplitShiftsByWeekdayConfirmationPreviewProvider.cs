// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Confirmation preview of split_shifts_by_weekday (owner decision 2026-10-23: cutting always asks first). When the autonomy
/// gate holds the call, this provider resolves it with the very planner the confirmed call uses (IShiftWeekdayCutPlanner,
/// which only reads) and describes the run: how many plannable shifts would be cut into how many parts, which ones with
/// their weekdays (up to a cap), how many are skipped and why. It refuses, so the gate issues no token, when the group cannot
/// be resolved or nothing would be cut. The group and every shift to cut, each with its sealed order and weekdays, are the
/// preview's binding (ShiftWeekdayCutBinding): the confirmed replay cuts exactly these shifts or nothing. A held apply=false
/// says that it cuts nothing; its replay only repeats the preview.
/// </summary>
/// <param name="planner">Resolves the call's group name into the shifts it would cut</param>

using System.Globalization;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services.Schedules;
using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Services.Assistant.Skills;

namespace Klacks.Api.Application.Skills;

public class SplitShiftsByWeekdayConfirmationPreviewProvider : ISkillConfirmationPreviewProvider
{
    private const int MaxListedShifts = 15;
    private const string WeekdaySeparator = ", ";
    private const string GroupNameMissing = "split_shifts_by_weekday needs groupName. Nothing was stored.";
    private const string NothingToCut =
        "Nothing to cut in the group '{0}': no plannable shift runs on several weekdays that is still uncut ({1} skipped). " +
        "Nothing was changed.";

    private const string ApplyHeadline = "Cut the plannable shifts of the group '{0}' by weekday now. ";
    private const string PreviewOnlyHeadline = "Preview only, this call cuts nothing. Group '{0}'. ";

    private const string Facts =
        "{0} plannable shift(s) run on several weekdays and would be cut into {1} part(s), one per weekday; {2} shift(s) are " +
        "skipped. To cut:\n{3}\n";

    private const string ApplyNote =
        "Every part keeps customer, groups, dates, times, staff count and quantity; only the weekday differs, so each part is " +
        "its own plannable shift. All shifts are cut in one transaction that is verified by re-reading and rolled back as a " +
        "whole on any mismatch; reset_shift_cuts undoes a cut, one order at a time. Exactly these shifts and weekdays are " +
        "bound to the confirmation: if a shift appears, disappears, changes its weekdays, is split or gets planned " +
        "assignments before the user confirms, nothing is cut and a fresh preview is shown.";

    private readonly IShiftWeekdayCutPlanner _planner;

    public SplitShiftsByWeekdayConfirmationPreviewProvider(IShiftWeekdayCutPlanner planner)
    {
        _planner = planner;
    }

    public bool Supports(string skillName) =>
        string.Equals(skillName, SplitShiftsByWeekdaySkill.SkillName, StringComparison.OrdinalIgnoreCase);

    public async Task<SkillConfirmationPreview> BuildAsync(
        string skillName,
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var groupName = SkillParameterReader.Read<string>(parameters, SplitShiftsByWeekdaySkill.GroupNameParameter, null);
        if (string.IsNullOrWhiteSpace(groupName))
        {
            return SkillConfirmationPreview.Refuse(GroupNameMissing);
        }

        var selection = await _planner.PlanAsync(groupName, cancellationToken);
        if (selection.IsRefused)
        {
            return SkillConfirmationPreview.Refuse(selection.Refusal!);
        }

        if (selection.ToCut.Count == 0)
        {
            return SkillConfirmationPreview.Refuse(string.Format(
                CultureInfo.InvariantCulture, NothingToCut, selection.GroupName, selection.Skipped.Count));
        }

        var apply = SkillParameterReader.Read<bool?>(parameters, PreviewApplySkillCalls.ApplyParameter) == true;
        var headline = string.Format(CultureInfo.InvariantCulture, apply ? ApplyHeadline : PreviewOnlyHeadline, selection.GroupName);
        var parts = selection.ToCut.Sum(shift => ShiftWeekdayCutBuilder.ActiveIsoWeekdays(shift).Count);
        var facts = string.Format(
            CultureInfo.InvariantCulture, Facts, selection.ToCut.Count, parts, selection.Skipped.Count, ListShifts(selection.ToCut));
        var text = headline + facts + (apply ? ApplyNote : string.Empty);
        return SkillConfirmationPreview.Show(text, ShiftWeekdayCutBinding.Of(selection));
    }

    private static string ListShifts(IReadOnlyList<Shift> shifts)
    {
        var lines = shifts.Take(MaxListedShifts).Select(shift =>
            $"- {shift.Name} ({string.Join(WeekdaySeparator, ShiftWeekdayCutBuilder.ActiveIsoWeekdays(shift).Select(WeekdayName))})");
        var more = shifts.Count > MaxListedShifts ? $"\n... and {shifts.Count - MaxListedShifts} more" : string.Empty;
        return string.Join("\n", lines) + more;
    }

    private static string WeekdayName(int isoDay) => ((DayOfWeek)IsoWeekday.ToStorage(isoDay)).ToString();
}
