// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// How the MIN-over-admins aggregation treats an admin who has never stored an autonomy level. The two
/// members are not a preference, they are two different questions: a path that still asks a human may
/// read the shared default, a path that acts unattended may not, because nobody ever chose that default.
/// </summary>

namespace Klacks.Api.Domain.Enums;

public enum AdminAutonomyMissingPreferencePolicy
{
    /// <summary>An admin without a stored row counts as AutonomyDefaults.DefaultLevel.</summary>
    FallBackToDefault = 0,

    /// <summary>An admin without a stored row blocks the aggregation: no level is produced at all.</summary>
    Block = 1
}
