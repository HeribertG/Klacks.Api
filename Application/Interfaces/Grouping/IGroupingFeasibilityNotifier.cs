// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Grouping;

namespace Klacks.Api.Application.Interfaces.Grouping;

public interface IGroupingFeasibilityNotifier
{
    long CaptureSnapshotGeneration();

    Task NotifyAsync(
        GroupingFeasibilityReport report,
        GroupingFeasibilityDailySnapshot requesterSnapshot,
        Guid requesterId,
        bool requesterIsAdmin,
        long snapshotGeneration,
        CancellationToken cancellationToken);
}
