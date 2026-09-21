// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Deadline arithmetic for an approval chain that has no natural anchor such as a shift start: the
/// chain may take at most one approval window per roster stage, run fully serial. A caller whose kind
/// does have a natural deadline (shift start, period start) passes that instead and never calls this.
/// </summary>
/// <param name="nowUtc">The moment the chain starts.</param>
/// <param name="stageCount">Number of roster stages; an empty roster still gets one window so the chain has a well-formed deadline to exhaust against.</param>
/// <param name="windowMinutes">Minutes granted per stage (PROACTIVE_APPROVAL_WINDOW_MINUTES).</param>

namespace Klacks.Api.Domain.Services.Assistant;

public static class ProactiveApprovalDeadline
{
    private const int MinimumStageCount = 1;

    public static DateTime Compute(DateTime nowUtc, int stageCount, int windowMinutes)
    {
        var stages = Math.Max(stageCount, MinimumStageCount);
        return nowUtc.AddMinutes((double)stages * windowMinutes);
    }
}
