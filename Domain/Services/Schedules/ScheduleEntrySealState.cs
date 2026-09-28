// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Carries the server-owned seal state (LockLevel / SealedAt / SealedBy and the pre-seal state a period seal
/// recorded) of a schedule entry from the persisted row onto an entity rebuilt from a client resource.
/// ScheduleMapper deliberately does not map these fields, so a mapped entity carries the defaults; since the
/// repositories persist with a full-row update, saving it unchanged would silently unseal the entry or drop
/// the state a period unseal restores. Only the seal paths that enforce
/// <see cref="IWorkLockLevelService.CanSeal"/> may change the values, never a PUT payload.
/// </summary>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Services.Schedules;

public static class ScheduleEntrySealState
{
    /// <summary>
    /// Copies the seal state of <paramref name="stored"/> onto <paramref name="target"/>. A missing stored
    /// row means the entry is not sealed, which is also the safe answer for an unknown id.
    /// </summary>
    /// <param name="target">Entity rebuilt from the client resource, about to be persisted</param>
    /// <param name="stored">The untracked row as it currently exists in the database, or null</param>
    public static void CarryOver(ScheduleEntryBase target, ScheduleEntryBase? stored)
    {
        target.LockLevel = stored?.LockLevel ?? WorkLockLevel.None;
        target.SealedAt = stored?.SealedAt;
        target.SealedBy = stored?.SealedBy;
        target.PreSealLockLevel = stored?.PreSealLockLevel;
        target.PreSealSealedAt = stored?.PreSealSealedAt;
        target.PreSealSealedBy = stored?.PreSealSealedBy;
    }
}
