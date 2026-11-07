// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Reprocesses the breaks of the requested window and recalculates the cached period hours of all clients (of the
/// selected group) once per real pay period the window touches, then tells the schedules to reload them. Frozen client
/// periods are skipped and their number is returned; with ConfirmFrozen and a reason they are recalculated too. When
/// there are any, the override is written to the period audit log BEFORE anything is recalculated, so a failing audit
/// write stops the override instead of leaving it unrecorded.
/// </summary>
/// <param name="periodHoursService">Splits the window into pay periods and stores one row per client and period</param>
/// <param name="frozenPeriodOverrideAudit">Checks the override reason and records the override</param>

using Klacks.Api.Application.Commands.Works;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces.Macros;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Works;

public class RecalculatePeriodHoursCommandHandler
    : BaseHandler, IRequestHandler<RecalculatePeriodHoursCommand, PeriodHoursRecalculationResult>
{
    private readonly IPeriodHoursService _periodHoursService;
    private readonly IWorkNotificationService _notificationService;
    private readonly IBreakMacroService _breakMacroService;
    private readonly IFrozenPeriodOverrideAudit _frozenPeriodOverrideAudit;

    public RecalculatePeriodHoursCommandHandler(
        IPeriodHoursService periodHoursService,
        IWorkNotificationService notificationService,
        IBreakMacroService breakMacroService,
        IFrozenPeriodOverrideAudit frozenPeriodOverrideAudit,
        ILogger<RecalculatePeriodHoursCommandHandler> logger)
        : base(logger)
    {
        _periodHoursService = periodHoursService;
        _notificationService = notificationService;
        _breakMacroService = breakMacroService;
        _frozenPeriodOverrideAudit = frozenPeriodOverrideAudit;
    }

    public async Task<PeriodHoursRecalculationResult> Handle(RecalculatePeriodHoursCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            if (request.ConfirmFrozen)
            {
                _frozenPeriodOverrideAudit.EnsureReason(request.Reason);
            }

            var payPeriods = await _periodHoursService.GetPeriodsInRangeAsync(request.StartDate, request.EndDate);

            if (request.ConfirmFrozen)
            {
                await RecordOverrideBeforeRecalculationAsync(request, payPeriods, cancellationToken);
            }

            await _breakMacroService.ReprocessAllBreaksAsync(request.StartDate, request.EndDate);

            var frozenClientPeriods = 0;
            foreach (var (periodStart, periodEnd) in payPeriods)
            {
                var frozen = await _periodHoursService.RecalculateAllClientsAsync(
                    periodStart, periodEnd, request.SelectedGroup, analyseToken: null, includeFrozen: request.ConfirmFrozen);
                frozenClientPeriods += frozen.Count;
            }

            // System-wide group-scoped recalculation: no entity-level AnalyseToken association possible.
            await _notificationService.NotifyPeriodHoursRecalculated(request.StartDate, request.EndDate, analyseToken: null);

            return new PeriodHoursRecalculationResult(true, request.ConfirmFrozen ? 0 : frozenClientPeriods);
        },
        "recalculating period hours",
        new { request.StartDate, request.EndDate, request.SelectedGroup, request.ConfirmFrozen });
    }

    private async Task RecordOverrideBeforeRecalculationAsync(
        RecalculatePeriodHoursCommand request,
        IReadOnlyList<(DateOnly StartDate, DateOnly EndDate)> payPeriods,
        CancellationToken cancellationToken)
    {
        var frozenClients = new HashSet<Guid>();
        foreach (var (periodStart, periodEnd) in payPeriods)
        {
            frozenClients.UnionWith(await _periodHoursService.GetFrozenClientIdsAsync(
                periodStart, periodEnd, request.SelectedGroup, analyseToken: null));
        }

        if (frozenClients.Count > 0)
        {
            await _frozenPeriodOverrideAudit.RecordAsync(
                request.StartDate, request.EndDate, request.SelectedGroup, request.Reason!, frozenClients.Count, cancellationToken);
        }
    }
}
