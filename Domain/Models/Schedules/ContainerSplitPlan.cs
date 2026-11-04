// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Models.Schedules;

/// <summary>
/// What a container split writes: the first part ends at the split time, the second part starts there on its own day
/// and takes over the children listed here.
/// </summary>
/// <param name="ContainerId">The container that is split; it stays the first part</param>
/// <param name="SplitTime">End of the first part and start of the second part</param>
/// <param name="SecondPartDate">Day of the second part (the next day when the split lies after midnight)</param>
/// <param name="TargetClientId">Person of the second part</param>
/// <param name="MovedChildIds">Sub-works and sub-breaks that move to the second part</param>
public sealed record ContainerSplitPlan(
    Guid ContainerId,
    TimeOnly SplitTime,
    DateOnly SecondPartDate,
    Guid TargetClientId,
    IReadOnlySet<Guid> MovedChildIds);
