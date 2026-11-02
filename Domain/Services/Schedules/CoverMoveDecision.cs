// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Decides whether an absence cover moves a work to the stand-in or keeps it with a replacement. A work moves only as a
/// whole: the cover spans the whole work of its current owner, the work is top-level and unsealed and carries no live
/// replacement and no sealed child. The path decides, not the clock: an absence cover always means the person did not
/// start the service, a replacement of someone who left early comes from the context menu.
/// </summary>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Services.Schedules;

public static class CoverMoveDecision
{
    /// <param name="facts">The work as it stands in the scenario</param>
    /// <param name="fromClientId">Person the cover takes the work from</param>
    /// <param name="coverStart">Start of the covered span</param>
    /// <param name="coverEnd">End of the covered span</param>
    public static bool CoversWholeWork(CoverMoveFacts facts, Guid fromClientId, TimeOnly coverStart, TimeOnly coverEnd)
        => facts.ClientId == fromClientId
            && facts.ParentWorkId is null
            && facts.LockLevel == WorkLockLevel.None
            && !facts.HasReplacement
            && !facts.HasLockedChild
            && facts.StartTime == coverStart
            && facts.EndTime == coverEnd;
}
