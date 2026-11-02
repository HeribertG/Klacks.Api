// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Decides whether an absence cover moves a work to the stand-in or keeps it with a replacement. A work moves only as a
/// whole: the cover spans the whole work of its current owner, the work is top-level and unsealed and carries no live
/// replacement and no sealed child. The planner dialog always moves, the planner knows the person did not start. Klacksy
/// and the messenger move only when the absence was reported before the work starts; a later report keeps the work with
/// a replacement. A replacement of someone who left early comes from the context menu.
/// </summary>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Services.Schedules;

public static class CoverMoveDecision
{
    /// <summary>A work nothing holds in place: top-level, unsealed, no live replacement, no sealed child.</summary>
    /// <param name="facts">The work as it stands</param>
    public static bool IsUntouched(CoverMoveFacts facts)
        => facts.ParentWorkId is null
            && facts.LockLevel == WorkLockLevel.None
            && !facts.HasReplacement
            && !facts.HasLockedChild;

    /// <summary>A work this report may move: nothing holds it in place and its path allows it.</summary>
    /// <param name="facts">The work as it stands</param>
    /// <param name="report">How the absence reached the run</param>
    /// <param name="companyTimeZone">Zone the workday and its start time are written in</param>
    public static bool MayMove(CoverMoveFacts facts, CoverReport report, TimeZoneInfo companyTimeZone)
        => IsUntouched(facts)
            && (report.Source == ReplacementRequestSource.PlannerDialog
                || report.ReportedAtUtc < CompanyWallClockToUtcConverter.ConvertToUtc(
                    facts.CurrentDate.ToDateTime(facts.StartTime), companyTimeZone));

    /// <param name="facts">The work as it stands in the scenario</param>
    /// <param name="fromClientId">Person the cover takes the work from</param>
    /// <param name="coverStart">Start of the covered span</param>
    /// <param name="coverEnd">End of the covered span</param>
    public static bool CoversWholeWork(CoverMoveFacts facts, Guid fromClientId, TimeOnly coverStart, TimeOnly coverEnd)
        => facts.ClientId == fromClientId
            && IsUntouched(facts)
            && facts.StartTime == coverStart
            && facts.EndTime == coverEnd;
}
