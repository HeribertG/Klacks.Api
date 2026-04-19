// Copyright (c) Heribert Gasparoli Private. All rights reserved.

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
            await _breakMacroService.ReprocessAllBreaksAsync(request.StartDate, request.EndDate);
            await _periodHoursService.RecalculateAllClientsAsync(request.StartDate, request.EndDate, request.SelectedGroup);
            // System-wide group-scoped recalculation: no entity-level AnalyseToken association possible.
            await _notificationService.NotifyPeriodHoursRecalculated(request.StartDate, request.EndDate, analyseToken: null);
            return true;
        },
        "recalculating period hours",
        new { request.StartDate, request.EndDate, request.SelectedGroup });
    }
}
