// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Reprocesses the breaks of the requested window and recalculates the cached period hours of all clients (of the
/// selected group) once per real pay period the window touches, then tells the schedules to reload them.
/// </summary>
/// <param name="periodHoursService">Splits the window into pay periods and stores one row per client and period</param>

using Klacks.Api.Application.Commands.Works;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces.Macros;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Works;

public class RecalculatePeriodHoursCommandHandler : BaseHandler, IRequestHandler<RecalculatePeriodHoursCommand, bool>
{
    private readonly IPeriodHoursService _periodHoursService;
    private readonly IWorkNotificationService _notificationService;
    private readonly IBreakMacroService _breakMacroService;

    public RecalculatePeriodHoursCommandHandler(
        IPeriodHoursService periodHoursService,
        IWorkNotificationService notificationService,
        IBreakMacroService breakMacroService,
        ILogger<RecalculatePeriodHoursCommandHandler> logger)
        : base(logger)
    {
        _periodHoursService = periodHoursService;
        _notificationService = notificationService;
        _breakMacroService = breakMacroService;
    }

    public async Task<bool> Handle(RecalculatePeriodHoursCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var payPeriods = await _periodHoursService.GetPeriodsInRangeAsync(request.StartDate, request.EndDate);
            await _breakMacroService.ReprocessAllBreaksAsync(request.StartDate, request.EndDate);
            foreach (var (periodStart, periodEnd) in payPeriods)
            {
                await _periodHoursService.RecalculateAllClientsAsync(periodStart, periodEnd, request.SelectedGroup);
            }

            // System-wide group-scoped recalculation: no entity-level AnalyseToken association possible.
            await _notificationService.NotifyPeriodHoursRecalculated(request.StartDate, request.EndDate, analyseToken: null);
            return true;
        },
        "recalculating period hours",
        new { request.StartDate, request.EndDate, request.SelectedGroup });
    }
}
