// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Interfaces.Schedules;

public interface IContainerWorkChangeGuard
{
    /// <summary>
    /// Refuses a correction-family WorkChange (correction, travel, briefing) on a container work with
    /// ContainerWorkChangeRefusalCodes.CorrectionRefused. A change of another type or on another work passes.
    /// </summary>
    /// <param name="workChange">The change about to be written</param>
    /// <param name="parentWork">The work the change is attached to</param>
    Task EnsureNoCorrectionAsync(WorkChange workChange, Work parentWork);

    /// <summary>
    /// Refuses a replacement WorkChange on a container work or on an inner service of a container with
    /// ContainerWorkChangeRefusalCodes.ReplacementRefused. A change of another type or on another work passes.
    /// </summary>
    /// <param name="workChange">The change about to be written</param>
    /// <param name="parentWork">The work the change is attached to</param>
    Task EnsureNoReplacementAsync(WorkChange workChange, Work parentWork);
}
