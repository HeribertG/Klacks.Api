// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Keeps the cached period hours in step when an absence type is switched to or from on-call: an on-call absence counts
/// no hours (BreakOwnHours), so every cached real-plan row over the first to the last day of the type's own absences is
/// recomputed in place (IPeriodHoursService.RefreshCachedPeriodHoursAsync, which leaves frozen rows unchanged). Rows of
/// scenarios are not touched. It runs after the switch is committed, so a failure is logged and does not fail the save:
/// the rows then stay stale until that range is recalculated.
/// </summary>
/// <param name="breakRepository">Reads the span of the type's real-plan absences</param>
/// <param name="periodHoursService">Recomputes the cached rows over that span</param>
/// <param name="logger">Records a refresh that failed after the switch was saved</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Domain.Interfaces.Schedules;

namespace Klacks.Api.Application.Services.Schedules;

public sealed class OnCallSwitchPeriodHoursRefresher : IOnCallSwitchPeriodHoursRefresher
{
    private readonly IBreakRepository _breakRepository;
    private readonly IPeriodHoursService _periodHoursService;
    private readonly ILogger<OnCallSwitchPeriodHoursRefresher> _logger;

    public OnCallSwitchPeriodHoursRefresher(
        IBreakRepository breakRepository,
        IPeriodHoursService periodHoursService,
        ILogger<OnCallSwitchPeriodHoursRefresher> logger)
    {
        _breakRepository = breakRepository;
        _periodHoursService = periodHoursService;
        _logger = logger;
    }

    public async Task RefreshAsync(Guid absenceId, CancellationToken cancellationToken = default)
    {
        try
        {
            var span = await _breakRepository.GetRealPlanSpanOfAbsenceTypeAsync(absenceId, cancellationToken);
            if (span is not { } days)
            {
                return;
            }

            await _periodHoursService.RefreshCachedPeriodHoursAsync(days.First, days.Last);
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(
                exception,
                "Refreshing the cached period hours after the on-call switch of absence type {AbsenceId} failed; the switch is saved, the cached hours over its absences stay stale until that range is recalculated",
                absenceId);
        }
    }
}
