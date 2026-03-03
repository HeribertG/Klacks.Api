// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Commands.Works;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Works;

public class RecalculatePeriodHoursCommandHandler : BaseHandler, IRequestHandler<RecalculatePeriodHoursCommand, bool>
{
    private readonly IPeriodHoursService _periodHoursService;
    private readonly IWorkNotificationService _notificationService;

    public RecalculatePeriodHoursCommandHandler(
        IPeriodHoursService periodHoursService,
        IWorkNotificationService notificationService,
        ILogger<RecalculatePeriodHoursCommandHandler> logger)
        : base(logger)
    {
        _periodHoursService = periodHoursService;
        _notificationService = notificationService;
    }

    public async Task<bool> Handle(RecalculatePeriodHoursCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            await _periodHoursService.RecalculateAllClientsAsync(request.StartDate, request.EndDate, request.SelectedGroup);
            await _notificationService.NotifyPeriodHoursRecalculated(request.StartDate, request.EndDate);
            return true;
        },
        "recalculating period hours",
        new { request.StartDate, request.EndDate, request.SelectedGroup });
    }
}
