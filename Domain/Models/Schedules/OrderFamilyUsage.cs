// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// What a sealed order's family (the order itself, its plannable copy and every cut piece) is still used by, as the
/// administrator delete of a whole order family needs it.
/// </summary>
/// <param name="ShiftIds">Ids of every live row of the family, the sealed order included</param>
/// <param name="HasWorks">True when any work, real or scenario, that is not deleted sits on a family shift</param>
/// <param name="HasScenarioClones">True when an active scenario holds a clone of a family shift</param>
/// <param name="HasContainerReferences">True when a container template or container override points at a family shift</param>

namespace Klacks.Api.Domain.Models.Schedules;

public sealed record OrderFamilyUsage(
    IReadOnlyList<Guid> ShiftIds,
    bool HasWorks,
    bool HasScenarioClones,
    bool HasContainerReferences)
{
    public bool IsInUse => HasWorks || HasScenarioClones || HasContainerReferences;
}
