// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Shared definition of a break entry with a directly recorded duration, so the break macro processing
/// (BreakMacroService) and the macro dry run (MacroDryRunService) leave exactly the same entries alone
/// instead of one service reaching into the other.
/// </summary>

using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Services.Schedules;

public static class BreakDurationPredicate
{
    /// <summary>
    /// True when the entry carries a directly recorded duration instead of a time span.
    /// </summary>
    /// <remarks>
    /// Absence details can be recorded either as a time range or as a plain duration. In the
    /// duration case no times exist, so both bounds are equal ("no times recorded") and the hours
    /// live in WorkTime. Deriving a duration from those bounds would discard what the user entered,
    /// which is why the macro must not overwrite it. Equal bounds with a WorkTime of zero are a
    /// genuinely empty entry and still go through the macro.
    /// </remarks>
    /// <param name="breakEntry">The break entry to evaluate.</param>
    public static bool HasDirectlyRecordedDuration(Break breakEntry)
        => breakEntry.StartTime == breakEntry.EndTime && breakEntry.WorkTime > 0;
}
