// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Single source of the effective autonomy level for the next-period scheduling automation. Both the
/// detector (which decides whether to start the wizard chain at all) and the auto-commit watcher (which
/// re-checks the level minutes later, right before it accepts) ask this - a chain that was permitted at
/// start time must not be committed after an admin has lowered their level in the meantime, and two
/// copies of the aggregation rule would drift apart exactly there.
/// </summary>

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface INextPeriodAutonomyResolver
{
    /// <summary>Resolves the current effective level and the admin who decided it. Never returns null.</summary>
    Task<NextPeriodAutonomyDecision> ResolveAsync(CancellationToken cancellationToken = default);
}
