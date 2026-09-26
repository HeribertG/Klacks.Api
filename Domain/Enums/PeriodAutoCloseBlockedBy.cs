// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// The strongest brake that currently keeps Klacksy from closing a period on its own, in precedence
/// order. None is the only value under which a period may be sealed unattended.
/// </summary>

namespace Klacks.Api.Domain.Enums;

public enum PeriodAutoCloseBlockedBy
{
    /// <summary>Nothing brakes: every gate says FullyAutonomous and Execute.</summary>
    None = 0,

    /// <summary>The global proactive kill switch is set.</summary>
    KillSwitch = 1,

    /// <summary>The governance rule of period_auto_close is switched off for this group or installation-wide.</summary>
    KindDisabled = 2,

    /// <summary>The governance rule's effective MaxAction is below Execute (the seeded default is Hint).</summary>
    MaxAction = 3,

    /// <summary>The installation-wide proactive autonomy level is below FullyAutonomous.</summary>
    GlobalLevel = 4,

    /// <summary>At least one admin never stored an autonomy level, so nobody may assume their consent.</summary>
    AdminLevelMissing = 5,

    /// <summary>There is no admin at all whose consent could release the close.</summary>
    NoAdmins = 6,

    /// <summary>The minimum autonomy level over all admins is below FullyAutonomous.</summary>
    AdminLevel = 7,

    /// <summary>The deciding admin's id is not a usable Guid, so the seal would have no author.</summary>
    NoDecidingAdmin = 8
}
