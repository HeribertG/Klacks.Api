// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// English sentences of the booking diagnosis: one per refusal reason (with placeholders for names, dates and counts),
/// one per remedy, one per channel and the notes that keep the channel truth honest (a sporadic limit only the single
/// direct entry checks, a scenario that skips the seal and the qualification check of the direct entry). The model
/// relays them in the user's language; no sentence names a code or an enum value. Every reason, remedy and outcome has
/// exactly one entry, the seal kind is a reason of its own (day approval or closed period), and no sentence, notes
/// included, names an enum value (WorkBookingDiagnosisTextsCoverageTests).
/// </summary>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.Skills;

internal static class WorkBookingDiagnosisTexts
{
    public const string EmployeeToken = "{employee}";

    public const string ShiftToken = "{shift}";

    public const string OccupyingShiftToken = "{occupyingShift}";

    public const string DateToken = "{date}";

    public const string FromToken = "{from}";

    public const string UntilToken = "{until}";

    public const string CapacityToken = "{capacity}";

    public const string RangeFromToken = "{rangeFrom}";

    public const string RangeUntilToken = "{rangeUntil}";

    public const string DayApprovalSealed = "{date} has been approved (day approval) for {employee}; approved days are fixed.";

    public const string PeriodClosedSealed = "{date} lies in a closed period for {employee}; closed days are fixed.";

    public const string OccupiedByHiddenWork =
        "The cell of {employee} on {date} already holds an entry of a group you cannot see.";

    public const string OccupiedByUnnamedWork = "The cell of {employee} on {date} already holds another shift.";

    public const string SporadicNotCheckedByAssistant =
        "This limit is only checked when a single entry is saved directly in the schedule; Klacksy's placement and " +
        "pasting several cells do not check it.";

    public const string ScenarioSkipsDirectCheck =
        "In this scenario a direct entry is not refused for this reason; it only becomes binding when the scenario is " +
        "accepted into the real plan.";

    public const string RelayInstruction =
        "Relay the reasons and remedies in plain words in the user's language, never codes or field names. These are the " +
        "verified facts: state them as they are and do not add other causes you were not given.";

    public static readonly IReadOnlyDictionary<WorkBookingRefusalReason, string> Reasons =
        new Dictionary<WorkBookingRefusalReason, string>
        {
            [WorkBookingRefusalReason.DayApproved] = DayApprovalSealed,
            [WorkBookingRefusalReason.PeriodClosed] = PeriodClosedSealed,
            [WorkBookingRefusalReason.OutsideMembershipPeriod] =
                "{date} is outside the membership of {employee} ({from} to {until}).",
            [WorkBookingRefusalReason.OutsideGroupPeriod] =
                "{date} is outside the period in which {employee} belongs to the selected group ({from} to {until}).",
            [WorkBookingRefusalReason.SporadicShiftDayFull] =
                "The sporadic shift {shift} already has all {capacity} people it needs booked on {date}.",
            [WorkBookingRefusalReason.SporadicShiftRangeExhausted] =
                "The sporadic shift {shift} has already used all {capacity} days its range allows ({rangeFrom} to " +
                "{rangeUntil}), and {date} would be one more day.",
            [WorkBookingRefusalReason.HardBlockingConflict] =
                "{employee} lacks a mandatory qualification the shift {shift} requires, or holds it below a binding " +
                "minimum level, or it has expired where expiry blocks; this can never be overridden.",
            [WorkBookingRefusalReason.AssistantOnlyConflict] =
                "Booking {shift} for {employee} on {date} would create a schedule conflict (for example a double " +
                "booking, a day wish of the employee or a working-time rule set to block).",
            [WorkBookingRefusalReason.CellOccupiedByWork] = "The cell of {employee} on {date} already holds the shift {occupyingShift}.",
            [WorkBookingRefusalReason.CellOccupiedByAbsence] = "The cell of {employee} on {date} already holds an absence.",
        };

    public static readonly IReadOnlyDictionary<WorkBookingRemedy, string> Remedies =
        new Dictionary<WorkBookingRemedy, string>
        {
            [WorkBookingRemedy.ReopenPeriodAdmin] =
                "Only an administrator can reopen the closed period (page Period closing, with a reason).",
            [WorkBookingRemedy.RevokeDayApproval] =
                "An administrator, or an authorised user of the approving group, can revoke the day approval.",
            [WorkBookingRemedy.AdjustMembership] = "Check or correct the entry and exit date of the employee.",
            [WorkBookingRemedy.AdjustGroupMembership] =
                "Adjust the validity dates of the employee's membership in this group.",
            [WorkBookingRemedy.ChooseOtherDayOrShift] = "Choose another day or another shift.",
            [WorkBookingRemedy.AddQualificationOrRelaxLevel] =
                "Record the qualification for the employee with a sufficient level and a valid period. Only when the " +
                "level is too low, setting this requirement of the shift to hint only is also possible where acceptable.",
            [WorkBookingRemedy.AdjustPlanOrEnterDirectly] =
                "Adjust the plan; a direct entry in the schedule is saved anyway and the conflict appears in the error list.",
            [WorkBookingRemedy.RemoveOrMoveExistingEntry] = "Remove or move the existing entry first, or choose another day.",
            [WorkBookingRemedy.RemoveConfirmationFirst] =
                "The existing entry is confirmed; remove the confirmation first, then change or remove it.",
            [WorkBookingRemedy.AskForGroupVisibility] =
                "Ask a planner who sees that group, or an administrator.",
        };

    public static readonly IReadOnlyDictionary<WorkBookingDiagnosisOutcome, string> Outcomes =
        new Dictionary<WorkBookingDiagnosisOutcome, string>
        {
            [WorkBookingDiagnosisOutcome.NotFound] = "The employee or the shift was not found.",
            [WorkBookingDiagnosisOutcome.Bookable] = "Nothing refuses this booking right now.",
            [WorkBookingDiagnosisOutcome.Refused] = "This booking is refused right now.",
            [WorkBookingDiagnosisOutcome.GridOnly] =
                "Only the schedule grid blocks this cell right now; the reason is below.",
        };

    public const string DirectEntryRefuses = "Saving it directly in the schedule is refused.";

    public const string AssistantRefuses = "Klacksy refuses to place it.";

    public const string GridBlocks = "The schedule grid does not accept the shift in this cell.";
}
