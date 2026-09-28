// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IScheduledTaskRunner
{
    /// <summary>
    /// Processes every scheduled task that is due now: claims it, runs the resolved action (or skips a
    /// stale occurrence), delivers the result to the owner and records the outcome.
    /// </summary>
    Task RunDueAsync(CancellationToken cancellationToken = default);
}
