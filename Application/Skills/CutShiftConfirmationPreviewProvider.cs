// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Confirmation preview of cut_shift (owner decision 2026-10-23: cutting always asks first). When the autonomy gate holds the
/// call, this provider plans it with the very planner the confirmed call uses and lists the plannable shift, its order
/// period and times, every part with times and name, and the default expenses every part carries (each work on any part is
/// booked with them). It refuses - so the gate issues no token - when the plan is refused (unknown or hidden shift, order
/// already split, no plannable shift, parts that do not tile the order, a malformed or missing parameter). It only reads.
/// The plannable shift, the parts and the expenses are returned as the preview's binding (ShiftTimeCutBinding) and stored
/// with the token: the confirmed call cuts exactly this or nothing.
/// </summary>
/// <param name="planner">Plans the cut exactly as the confirmed call will</param>

using System.Globalization;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills;

namespace Klacks.Api.Application.Skills;

public class CutShiftConfirmationPreviewProvider : ISkillConfirmationPreviewProvider
{
    private const string OpenEnd = "open-ended";

    private const string BoundNote =
        "Exactly this cut is bound to the confirmation: if the plannable shift, a part or one of the default expenses "
        + "changes before the user confirms, nothing is cut and a fresh preview is shown.";

    private const string MissingParameter = "Required parameter '{0}' is missing.";

    private readonly IShiftTimeCutPlanner _planner;

    public CutShiftConfirmationPreviewProvider(IShiftTimeCutPlanner planner)
    {
        _planner = planner;
    }

    public bool Supports(string skillName) =>
        string.Equals(skillName, CutShiftSkill.SkillName, StringComparison.OrdinalIgnoreCase);

    public async Task<SkillConfirmationPreview> BuildAsync(
        string skillName,
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var shiftIdRaw = SkillParameterReader.Read<string>(parameters, CutShiftSkill.ShiftIdParameter, null);
        if (!Guid.TryParse(shiftIdRaw, out var shiftId))
        {
            return SkillConfirmationPreview.Refuse(Missing(CutShiftSkill.ShiftIdParameter));
        }

        var partsRaw = SkillParameterReader.Read<string>(parameters, CutShiftSkill.PartsParameter, null);
        if (string.IsNullOrWhiteSpace(partsRaw))
        {
            return SkillConfirmationPreview.Refuse(Missing(CutShiftSkill.PartsParameter));
        }

        var namesRaw = SkillParameterReader.Read<string>(parameters, CutShiftSkill.PartNamesParameter, null) ?? string.Empty;
        var plan = await _planner.PlanAsync(shiftId, partsRaw, namesRaw, context.UserLanguage, cancellationToken);
        if (plan.IsRefused)
        {
            return SkillConfirmationPreview.Refuse(plan.Refusal!);
        }

        var shift = plan.OriginalShift!;
        var period = $"{shift.FromDate.ToString(DateTimeFormats.IsoDate, CultureInfo.InvariantCulture)} - " +
                     (shift.UntilDate?.ToString(DateTimeFormats.IsoDate, CultureInfo.InvariantCulture) ?? OpenEnd);
        var parts = plan.Parts.Select(part =>
            $"- {Time(part.Start)}-{Time(part.End)} {part.Name}" + (part.Converted ? " (keeps the plannable shift)" : string.Empty));

        var text =
            $"Cut the plannable shift '{shift.Name}' ({Time(shift.StartShift)}-{Time(shift.EndShift)}, {period}) into " +
            $"{plan.Parts.Count} parts:\n{string.Join("\n", parts)}\n" +
            $"Default expenses every part carries (booked with each work on it): " +
            $"{CutPieceExpenseSummary.Describe(plan.DefaultExpenses.ToList())}.\n" +
            "The order itself stays unchanged; reset_shift_cuts undoes the cut.\n" +
            BoundNote;
        return SkillConfirmationPreview.Show(text, ShiftTimeCutBinding.Of(plan));
    }

    private static string Missing(string parameter) =>
        string.Format(CultureInfo.InvariantCulture, MissingParameter, parameter);

    private static string Time(TimeOnly time) => time.ToString(DateTimeFormats.HourMinute, CultureInfo.InvariantCulture);
}
