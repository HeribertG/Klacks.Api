// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using System.Globalization;
using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.Exceptions;

/// <summary>
/// An accept was requested for a scenario that does not carry a real absence the accept would delete (no scenario absence
/// of the same person, day and type). Refused as a 409 with its own code for every scenario; neither overrideStale nor
/// overrideBlock nor a Klacksy confirmation lifts it. The body carries the total number, a capped list of the absences
/// (client id and name, day, absence type) and overridable false. The message carries the number only, never names: the
/// error middleware logs it.
/// </summary>
/// <param name="droppedAbsenceCount">How many real absences the accept would delete</param>
/// <param name="droppedAbsences">The first of them, ordered by day and name</param>
public sealed class ScenarioDropsRealAbsenceException : ConflictException
{
    public const string ErrorCode = "SCENARIO_DROPS_REAL_ABSENCE";

    public const string DroppedAbsenceCountField = "droppedAbsenceCount";

    public const string DroppedAbsencesField = "droppedAbsences";

    public const string OverridableField = ScenarioStaleException.OverridableField;

    private const string MessageFormat =
        "Accepting this scenario would delete {0} real absence(s) it does not contain. Recalculate the scenario on the current plan; no override accepts it.";

    public ScenarioDropsRealAbsenceException(int droppedAbsenceCount, IReadOnlyList<DroppedRealAbsence> droppedAbsences)
        : base(string.Format(CultureInfo.InvariantCulture, MessageFormat, droppedAbsenceCount), ErrorCode, BuildDetails(droppedAbsenceCount, droppedAbsences))
    {
        DroppedAbsenceCount = droppedAbsenceCount;
        DroppedAbsences = droppedAbsences;
    }

    public int DroppedAbsenceCount { get; }

    public IReadOnlyList<DroppedRealAbsence> DroppedAbsences { get; }

    private static IReadOnlyDictionary<string, object?> BuildDetails(int count, IReadOnlyList<DroppedRealAbsence> absences)
        => new Dictionary<string, object?>
        {
            [DroppedAbsenceCountField] = count,
            [DroppedAbsencesField] = absences,
            [OverridableField] = false,
        };
}
