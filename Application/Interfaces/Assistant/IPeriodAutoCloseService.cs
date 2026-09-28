// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The autonomous period close: seals, per group, the latest period whose close date has come - but only
/// under full autonomy and only when every precondition holds. Used exclusively by PeriodAutoCloseDetector
/// inside the hourly trigger tick; returns the closed and blocked events the tick then reports.
/// </summary>

using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Application.Interfaces.Assistant;

public interface IPeriodAutoCloseService
{
    Task<IReadOnlyList<IAgentTriggerEvent>> RunAsync(CancellationToken cancellationToken = default);
}
