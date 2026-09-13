// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Single source of the autonomy gates for the next-period scheduling automation. Both the detector
/// (which decides whether to start the wizard chain at all) and the auto-commit watcher (which re-checks
/// minutes later, right before it accepts) ask this - a chain that was permitted at start time must not
/// be committed after an admin lowered their level or after the kill switch was flipped in the meantime,
/// and two copies of the aggregation rule would drift apart exactly there. Callers read the gates off
/// the decision and never re-check the kill switch, the global level or the governance row themselves.
/// </summary>

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface INextPeriodAutonomyResolver
{
    /// <summary>
    /// Resolves the current gates, the effective level and the admin who decided it. Never returns null.
    /// </summary>
    Task<NextPeriodAutonomyDecision> ResolveAsync(CancellationToken cancellationToken = default);
}
