// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// 409 refusal of a reset of a cut family. The code is the same for every reason, so the Ui explains all of them with one
/// text; the reason travels in the conflict details, and the date only where the caller may know it. The messages name the
/// rule only: they reach the log and the skill result, so they carry no shift name. The caller sees every row of the family
/// before a refusal with a date is raised. A refusal for a scenario work names only the start that was asked for, a refusal
/// for a row in use carries no date, because the scenario can belong to somebody else.
/// </summary>
/// <param name="message">English explanation for the log and the assistant</param>
/// <param name="reason">One of the reason constants of this class</param>
/// <param name="startDate">The start date that was asked for, or null when the refusal carries no date</param>
/// <param name="lastWorkDate">Last real work of the family, or null when the reason is not about real works</param>

using System.Globalization;
using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Application.Exceptions;

public sealed class ShiftResetRefusedException : ConflictException
{
    public const string ErrorCode = "SHIFT_RESET_REFUSED";
    public const string ReasonKey = "reason";
    public const string StartDateKey = "startDate";
    public const string LastWorkDateKey = "lastWorkDate";

    public const string WorksOnOrAfterReason = "worksOnOrAfter";
    public const string ScenarioWorksOnOrAfterReason = "scenarioWorksOnOrAfter";
    public const string NothingToResetReason = "nothingToReset";
    public const string RowsInUseReason = "rowsInUse";

    private const string WorksOnOrAfterMessageFormat =
        "Cannot reset the cuts from {0}: work is planned on or after that date, the last one on {1}. " +
        "Choose a day after the last work, or move or cancel those bookings first.";

    private const string ScenarioWorksOnOrAfterMessageFormat =
        "Cannot reset the cuts from {0}: a scenario holds work on or after that date on a copy of this shift or one of its parts. " +
        "Accept or discard the scenario, or remove that work from it, first.";

    private const string NothingToResetMessageFormat =
        "Cannot reset the cuts from {0}: there is one plannable shift already and no cut piece from that date on.";

    private const string RowsInUseMessage =
        "Cannot reset the cuts: a part of the shift that has to be removed is still used by a work, a scenario or a container.";

    private ShiftResetRefusedException(string message, string reason, DateOnly? startDate = null, DateOnly? lastWorkDate = null)
        : base(message, ErrorCode, BuildDetails(reason, startDate, lastWorkDate))
    {
    }

    public static ShiftResetRefusedException ForWorksOnOrAfter(DateOnly startDate, DateOnly lastWorkDate) =>
        new(string.Format(WorksOnOrAfterMessageFormat, Iso(startDate), Iso(lastWorkDate)), WorksOnOrAfterReason, startDate, lastWorkDate);

    public static ShiftResetRefusedException ForScenarioWorksOnOrAfter(DateOnly startDate) =>
        new(string.Format(ScenarioWorksOnOrAfterMessageFormat, Iso(startDate)), ScenarioWorksOnOrAfterReason, startDate);

    public static ShiftResetRefusedException ForNothingToReset(DateOnly startDate) =>
        new(string.Format(NothingToResetMessageFormat, Iso(startDate)), NothingToResetReason, startDate);

    public static ShiftResetRefusedException ForRowsInUse() => new(RowsInUseMessage, RowsInUseReason);

    private static Dictionary<string, object?> BuildDetails(string reason, DateOnly? startDate, DateOnly? lastWorkDate)
    {
        var details = new Dictionary<string, object?> { [ReasonKey] = reason };
        if (startDate.HasValue)
        {
            details[StartDateKey] = Iso(startDate);
        }

        if (lastWorkDate.HasValue)
        {
            details[LastWorkDateKey] = Iso(lastWorkDate);
        }

        return details;
    }

    private static string? Iso(DateOnly? date) => date?.ToString(DateTimeFormats.IsoDate, CultureInfo.InvariantCulture);
}
