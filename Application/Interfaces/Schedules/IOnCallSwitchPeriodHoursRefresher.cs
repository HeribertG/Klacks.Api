// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Interfaces.Schedules;

public interface IOnCallSwitchPeriodHoursRefresher
{
    /// <summary>
    /// Recomputes the cached real-plan period-hours rows over the span of this absence type's real-plan absences, after
    /// its on-call flag was switched and committed. Frozen rows stay unchanged. A failure is logged, not thrown, because
    /// the switch is already saved; a cancellation of <paramref name="cancellationToken"/> is passed on.
    /// </summary>
    /// <param name="absenceId">The absence type whose on-call flag changed</param>
    Task RefreshAsync(Guid absenceId, CancellationToken cancellationToken = default);
}
