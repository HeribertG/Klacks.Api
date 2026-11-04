// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Interfaces.Schedules;

/// <summary>
/// Stages a container split on the tracked entities (no save): the container is shortened to the split time, a second
/// container is added and the moved children are re-parented to it, keeping their ids. Nothing is expanded from the
/// container template and no default expenses are added.
/// </summary>
public interface IContainerSplitWriter
{
    /// <summary>Stages the split and returns the new second part.</summary>
    /// <param name="plan">What to write</param>
    Task<Work> StageAsync(ContainerSplitPlan plan, CancellationToken cancellationToken);
}
