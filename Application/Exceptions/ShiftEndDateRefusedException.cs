// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// 409 refusal of an end date on a real locked shift. The code is the same for every reason, so the Ui explains all of
/// them with one text; the reason and, where the caller may know them, the dates travel in the conflict details. The
/// messages name the rule only: they reach the log and the skill result, so they carry no shift name. A refusal for a
/// hidden row carries no date, and a refusal for a scenario work does not say when that work lies, because the scenario
/// can belong to somebody else.
/// </summary>
/// <param name="message">English explanation for the log and the assistant</param>
/// <param name="reason">One of the reason constants of this class</param>
/// <param name="untilDate">The end date that was asked for, or null when the refusal carries no dates</param>
/// <param name="lastWorkDate">Last real work after the end date, or null when the reason is not about real works</param>

using System.Globalization;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Exceptions;

public sealed class ShiftEndDateRefusedException : ConflictException
{
    public const string ErrorCode = "SHIFT_END_DATE_REFUSED";
    public const string ReasonKey = "reason";
    public const string UntilDateKey = "untilDate";
    public const string LastWorkDateKey = "lastWorkDate";

    public const string HiddenRowsReason = "hiddenRows";
    public const string WorksAfterReason = "worksAfter";
    public const string ScenarioWorksAfterReason = "scenarioWorksAfter";
    public const string BeforeStartReason = "beforeStart";
    public const string StoredEndReason = "storedEnd";
    public const string RowsInUseReason = "rowsInUse";
    public const string OrderMissingReason = "orderMissing";

    private const string HiddenRowsMessage =
        "Cannot set the end date: it ends every part of the shift, and not all of them are visible to you.";

    private const string WorksAfterMessageFormat =
        "Cannot set the end date to {0}: work is planned after that date on this shift or one of its parts, the last one on {1}. " +
        "Move or cancel those bookings first, or choose a later end date.";

    private const string ScenarioWorksAfterMessageFormat =
        "Cannot set the end date to {0}: a scenario holds work after that date on a copy of this shift or one of its parts. " +
        "Accept or discard the scenario, remove that work from it, or choose a later end date.";

    private const string BeforeStartMessageFormat =
        "Cannot set the end date to {0}: it must not be before the start of the shift ({1}).";

    private const string StoredEndMessageFormat =
        "Cannot set the end date to {0}: the shift already ends on {1}. A stored end date cannot be shortened or extended.";

    private const string RowsInUseMessageFormat =
        "Cannot set the end date to {0}: a part of the shift that starts later must be removed, " +
        "but it is still used by a work, a scenario or a container.";

    private const string OrderMissingMessage = "Cannot set the end date: the shift has no sealed order.";

    private ShiftEndDateRefusedException(string message, string reason, DateOnly? untilDate = null, DateOnly? lastWorkDate = null)
        : base(message, ErrorCode, BuildDetails(reason, untilDate, lastWorkDate))
    {
    }

    public static ShiftEndDateRefusedException ForHiddenRows() => new(HiddenRowsMessage, HiddenRowsReason);

    public static ShiftEndDateRefusedException ForWorksAfter(DateOnly untilDate, DateOnly lastWorkDate) =>
        new(string.Format(WorksAfterMessageFormat, Iso(untilDate), Iso(lastWorkDate)), WorksAfterReason, untilDate, lastWorkDate);

    public static ShiftEndDateRefusedException ForScenarioWorksAfter(DateOnly untilDate) =>
        new(string.Format(ScenarioWorksAfterMessageFormat, Iso(untilDate)), ScenarioWorksAfterReason, untilDate);

    public static ShiftEndDateRefusedException ForBeforeStart(DateOnly untilDate, DateOnly startDate) =>
        new(string.Format(BeforeStartMessageFormat, Iso(untilDate), Iso(startDate)), BeforeStartReason, untilDate);

    public static ShiftEndDateRefusedException ForStoredEnd(DateOnly untilDate, DateOnly storedEnd) =>
        new(string.Format(StoredEndMessageFormat, Iso(untilDate), Iso(storedEnd)), StoredEndReason, untilDate);

    public static ShiftEndDateRefusedException ForRowsInUse(DateOnly untilDate) =>
        new(string.Format(RowsInUseMessageFormat, Iso(untilDate)), RowsInUseReason, untilDate);

    public static ShiftEndDateRefusedException ForMissingOrder() => new(OrderMissingMessage, OrderMissingReason);

    private static Dictionary<string, object?> BuildDetails(string reason, DateOnly? untilDate, DateOnly? lastWorkDate)
    {
        var details = new Dictionary<string, object?> { [ReasonKey] = reason };
        if (untilDate.HasValue)
        {
            details[UntilDateKey] = Iso(untilDate);
        }

        if (lastWorkDate.HasValue)
        {
            details[LastWorkDateKey] = Iso(lastWorkDate);
        }

        return details;
    }

    private static string? Iso(DateOnly? date) => date?.ToString(DateTimeFormats.IsoDate, CultureInfo.InvariantCulture);
}
