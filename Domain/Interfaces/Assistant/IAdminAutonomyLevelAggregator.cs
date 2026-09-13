// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// The single implementation of "the most cautious admin brakes for everyone". Every unattended path -
/// the next-period scheduling automation and the goal-plan execution - folds the per-admin autonomy
/// preferences the same way; two copies of that rule would drift apart precisely where an installation
/// notices it least, in the branch that acts without being asked.
/// </summary>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IAdminAutonomyLevelAggregator
{
    /// <summary>
    /// Minimum autonomy level over all admin users. The admin ids arrive as an unordered set and are
    /// walked in ordinal order, so the reported deciding admin is stable across runs. Never returns null.
    /// </summary>
    /// <param name="missingPreferencePolicy">How an admin without a stored autonomy row is treated.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<AdminAutonomyAggregate> AggregateAsync(
        AdminAutonomyMissingPreferencePolicy missingPreferencePolicy,
        CancellationToken cancellationToken = default);
}
