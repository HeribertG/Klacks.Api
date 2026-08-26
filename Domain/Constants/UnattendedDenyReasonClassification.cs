// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Splits every <see cref="UnattendedDenyReason"/> into the two follow-ups a refused scheduled task can
/// get. The dividing line is NOT how severe the refusal is but whether the OWNER can still fix its cause
/// from the outside: a missing opt-in and a too-low autonomy level are both a single setting away, so
/// destroying the task for them would punish a user for something one click repairs. Everything else
/// needs the task itself to be authored differently, which is a recreation either way. The two sets are
/// curated rather than derived so that adding a new deny reason forces a decision - the guard test
/// UnattendedDenyReasonClassificationGuardTests fails until the new value is put in one of them - while
/// the runtime lookup stays fail-closed: an unlisted value is treated as terminal, never as recoverable.
/// </summary>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Constants;

public static class UnattendedDenyReasonClassification
{
    /// <summary>
    /// Refusals whose cause the owner can lift without authoring the task again; these pause it.
    /// </summary>
    public static readonly IReadOnlySet<UnattendedDenyReason> OwnerFixable =
        new HashSet<UnattendedDenyReason>
        {
            UnattendedDenyReason.IrreversibleWithoutOptIn,
            UnattendedDenyReason.AutonomyLevelTooLow
        };

    /// <summary>
    /// Refusals the owner cannot lift from the outside; these disable the task.
    /// </summary>
    public static readonly IReadOnlySet<UnattendedDenyReason> Terminal =
        new HashSet<UnattendedDenyReason>
        {
            UnattendedDenyReason.NoPermissions,
            UnattendedDenyReason.UnknownSkill,
            UnattendedDenyReason.SensitiveSkill,
            UnattendedDenyReason.UnknownRiskClass
        };

    /// <summary>
    /// True when the owner can repair the cause of this refusal and the task should therefore be paused
    /// instead of disabled. <see cref="UnattendedDenyReason.None"/> and any value not curated above
    /// answer false, so an unclassified reason can never keep a task alive by accident.
    /// </summary>
    /// <param name="reason">Machine-readable cause reported by the unattended policy.</param>
    public static bool IsOwnerFixable(UnattendedDenyReason reason) => OwnerFixable.Contains(reason);
}
