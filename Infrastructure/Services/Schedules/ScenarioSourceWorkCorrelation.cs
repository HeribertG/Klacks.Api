// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Infrastructure.Services.Schedules;

/// <summary>
/// Result of correlating a scenario-sourced harmonizer bitmap with the new scenario's clone.
/// </summary>
/// <param name="WorkIdMap">Bitmap or source work id -> clone work id to re-point</param>
/// <param name="SupersededBitmapWorkIds">Bitmap works whose real origin moved to another shift or date since the source
/// scenario was made; the real plan's state wins, so they are neither re-pointed nor recreated</param>
/// <param name="SupersededCloneIds">Clones of those moved real works; the re-point leaves them untouched</param>
public sealed record ScenarioSourceWorkCorrelation(
    IReadOnlyDictionary<Guid, Guid> WorkIdMap,
    IReadOnlySet<Guid> SupersededBitmapWorkIds,
    IReadOnlySet<Guid> SupersededCloneIds)
{
    public static ScenarioSourceWorkCorrelation Direct(IReadOnlyDictionary<Guid, Guid> workIdMap)
        => new(workIdMap, new HashSet<Guid>(), new HashSet<Guid>());
}
