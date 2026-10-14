// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The ids of the live container children (child Works and Breaks) of one or several container Works, as the cascade
/// delete would take them. HasSameEntriesAs compares two such sets by content, whatever the order; a Work id and a Break
/// id never stand in for each other.
/// </summary>
/// <param name="WorkIds">Ids of the live child Works</param>
/// <param name="BreakIds">Ids of the live child Breaks</param>

namespace Klacks.Api.Domain.Models.Schedules;

public sealed record ContainerChildIds(IReadOnlyList<Guid> WorkIds, IReadOnlyList<Guid> BreakIds)
{
    public static ContainerChildIds None { get; } = new([], []);

    public int Count => WorkIds.Count + BreakIds.Count;

    public bool HasSameEntriesAs(ContainerChildIds other) =>
        WorkIds.ToHashSet().SetEquals(other.WorkIds) && BreakIds.ToHashSet().SetEquals(other.BreakIds);
}
