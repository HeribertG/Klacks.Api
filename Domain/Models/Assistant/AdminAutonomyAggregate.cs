// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// The minimum autonomy level over all admin users and the admin whose stored preference produced it.
/// MinimumLevel is null when the aggregation produced no level at all - either there is no admin, or
/// AdminAutonomyMissingPreferencePolicy.Block hit an admin without a stored row. Both cases mean the
/// same thing to a caller: nobody has consented, so nothing may run unattended.
/// </summary>
/// <param name="MinimumLevel">Minimum over all admins, or null when no level could be established.</param>
/// <param name="DecidingAdminUserId">
/// The admin whose preference set <see cref="MinimumLevel"/>; ties go to the ordinally first admin id.
/// Null when there is no level, and null when the admin id is not a Guid - never Guid.Empty.
/// </param>
/// <param name="AdminWithoutStoredLevel">
/// Under AdminAutonomyMissingPreferencePolicy.Block, the admin id that blocked the aggregation, so the
/// caller can log WHICH admin is missing a level. Null in every other case.
/// </param>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Assistant;

public sealed record AdminAutonomyAggregate(
    AutonomyLevel? MinimumLevel,
    Guid? DecidingAdminUserId,
    string? AdminWithoutStoredLevel);
