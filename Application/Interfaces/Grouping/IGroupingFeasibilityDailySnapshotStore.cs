// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Grouping;

namespace Klacks.Api.Application.Interfaces.Grouping;

public interface IGroupingFeasibilityDailySnapshotStore
{
    long CurrentGeneration { get; }

    GroupingFeasibilityDailySnapshot? TryGet(string dayKey, out long generation);

    bool Set(string dayKey, GroupingFeasibilityDailySnapshot snapshot, long expectedGeneration);

    void Remove(string dayKey);
}
