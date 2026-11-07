// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Reprocesses the breaks of the requested window and recalculates the cached period hours of all clients (of the
/// selected group) once per real pay period the window touches, then tells the schedules to reload them. Frozen client
/// periods are skipped and their number is returned; with ConfirmFrozen and a reason they are recalculated too. When
/// there are any, the override is written to the period audit log BEFORE anything is recalculated, so a failing audit
/// write stops the override instead of leaving it unrecorded. The period hours follow the caller's group visibility
/// (RecalculateAllClientsAsync); for a group-restricted caller (planner) the breaks are reprocessed only for the clients
/// they may see, and not at all when they may see nobody (an empty scope would mean "all breaks"). Unrestricted callers
/// (admin, installation without groups) reprocess every break of the window, as before. A caller without the admin role
/// may recalculate at most PayPeriodLimits.MaxPeriodsForPlannerRecalculation pay periods per request
/// (ManualRecalculationWindow), checked before anything is written. Whether the caller may confirm frozen periods is
/// decided by the controller.
/// </summary>
/// <param name="periodHoursService">Splits the window into pay periods and stores one row per client and period</param>
/// <param name="frozenPeriodOverrideAudit">Checks the override reason and records the override</param>
/// <param name="userService">Tells the window cap whether the caller is an admin</param>

using Klacks.Api.Application.Commands.Works;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Macros;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Services.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Works;

public class RecalculatePeriodHoursCommandHandler
    : BaseHandler, IRequestHandler<RecalculatePeriodHoursCommand, PeriodHoursRecalculationResult>
{
    private readonly IPeriodHoursService _periodHoursService;
    private readonly IWorkNotificationService _notificationService;
    private readonly IBreakMacroService _breakMacroService;
    private readonly IFrozenPeriodOverrideAudit _frozenPeriodOverrideAudit;
    private readonly IUserService _userService;

    public RecalculatePeriodHoursCommandHandler(
        IPeriodHoursService periodHoursService,
        IWorkNotificationService notificationService,
        IBreakMacroService breakMacroService,
        IFrozenPeriodOverrideAudit frozenPeriodOverrideAudit,
        IUserService userService,
        ILogger<RecalculatePeriodHoursCommandHandler> logger)
        : base(logger)
    {
        _periodHoursService = periodHoursService;
        _notificationService = notificationService;
        _breakMacroService = breakMacroService;
        _frozenPeriodOverrideAudit = frozenPeriodOverrideAudit;
        _userService = userService;
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
            await ManualRecalculationWindow.EnsureWithinCallerLimitAsync(payPeriods.Count, _userService);

            if (request.ConfirmFrozen)
            {
                await RecordOverrideBeforeRecalculationAsync(request, payPeriods, cancellationToken);
            }

            await ReprocessVisibleBreaksAsync(request);

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

    private async Task ReprocessVisibleBreaksAsync(RecalculatePeriodHoursCommand request)
    {
        var visibleClientIds = await _periodHoursService.GetRestrictedCallerClientIdsAsync(
            request.StartDate, request.EndDate, request.SelectedGroup);
        if (visibleClientIds is null)
        {
            await _breakMacroService.ReprocessAllBreaksAsync(request.StartDate, request.EndDate);
            return;
        }

        if (visibleClientIds.Count > 0)
        {
            await _breakMacroService.ReprocessAllBreaksAsync(request.StartDate, request.EndDate, visibleClientIds.ToList());
        }
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
