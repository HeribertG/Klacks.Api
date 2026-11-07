// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Enqueues a thorough recalculation onto the thorough-recalculation queue. Before queuing it counts, per pay period of
/// the window, the clients whose period is frozen (same population as the background run): without ConfirmFrozen their
/// number is returned as skipped; with ConfirmFrozen and a reason the override travels with the queued request and, when
/// the request was accepted and frozen periods exist, it is written to the period audit log. The audit follows the
/// queuing on purpose: a full queue refuses the request, and an audit row written first would then record an override
/// that never ran; the reason is validated (blank, length) before queuing, so only a database outage can leave a queued
/// override unrecorded. An oversized window is refused here (InvalidRequestException) instead of in the background.
/// </summary>
/// <param name="queue">Queue for thorough recalculations</param>
/// <param name="periodHoursService">Splits the window into pay periods and finds the frozen clients</param>
/// <param name="frozenPeriodOverrideAudit">Checks the override reason and records the override</param>
/// <param name="logger">Logger for diagnostics</param>

using Klacks.Api.Application.Commands.Works;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Works;

public class RecalculatePeriodHoursThoroughCommandHandler
    : BaseHandler, IRequestHandler<RecalculatePeriodHoursThoroughCommand, PeriodHoursRecalculationResult>
{
    private readonly IThoroughRecalculationQueue _queue;
    private readonly IPeriodHoursService _periodHoursService;
    private readonly IFrozenPeriodOverrideAudit _frozenPeriodOverrideAudit;

    public RecalculatePeriodHoursThoroughCommandHandler(
        IThoroughRecalculationQueue queue,
        IPeriodHoursService periodHoursService,
        IFrozenPeriodOverrideAudit frozenPeriodOverrideAudit,
        ILogger<RecalculatePeriodHoursThoroughCommandHandler> logger)
        : base(logger)
    {
        _queue = queue;
        _periodHoursService = periodHoursService;
        _frozenPeriodOverrideAudit = frozenPeriodOverrideAudit;
    }

    public Task<PeriodHoursRecalculationResult> Handle(RecalculatePeriodHoursThoroughCommand request, CancellationToken cancellationToken)
    {
        return ExecuteAsync(async () =>
        {
            if (request.ConfirmFrozen)
            {
                _frozenPeriodOverrideAudit.EnsureReason(request.Reason);
            }

            var frozenClientPeriods = 0;
            var frozenClients = new HashSet<Guid>();
            foreach (var (periodStart, periodEnd) in await _periodHoursService.GetPeriodsInRangeAsync(request.StartDate, request.EndDate))
            {
                var frozen = await _periodHoursService.GetFrozenClientIdsAsync(
                    periodStart, periodEnd, request.SelectedGroup, request.AnalyseToken);
                frozenClientPeriods += frozen.Count;
                frozenClients.UnionWith(frozen);
            }

            var queued = _queue.QueueRecalculation(
                request.StartDate,
                request.EndDate,
                request.SelectedGroup,
                request.AnalyseToken,
                includeFrozen: request.ConfirmFrozen);

            if (!request.ConfirmFrozen)
            {
                return new PeriodHoursRecalculationResult(queued, frozenClientPeriods);
            }

            if (queued && frozenClients.Count > 0)
            {
                await _frozenPeriodOverrideAudit.RecordAsync(
                    request.StartDate, request.EndDate, request.SelectedGroup, request.Reason!, frozenClients.Count, cancellationToken);
            }

            return new PeriodHoursRecalculationResult(queued, 0);
        },
        "queuing thorough recalculation",
        new { request.StartDate, request.EndDate, request.SelectedGroup, request.AnalyseToken, request.ConfirmFrozen });
    }
}
