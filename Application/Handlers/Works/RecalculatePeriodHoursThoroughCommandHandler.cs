// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Enqueues a thorough recalculation onto ThoroughRecalculationBackgroundService.
/// Returns true when the request was accepted into the channel.
/// </summary>
/// <param name="backgroundService">Singleton background processor for thorough recalculations</param>
/// <param name="logger">Logger for diagnostics</param>

using Klacks.Api.Application.Commands.Works;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Infrastructure.Services;

namespace Klacks.Api.Application.Handlers.Works;

public class RecalculatePeriodHoursThoroughCommandHandler : BaseHandler, IRequestHandler<RecalculatePeriodHoursThoroughCommand, bool>
{
    private readonly ThoroughRecalculationBackgroundService _backgroundService;

    public RecalculatePeriodHoursThoroughCommandHandler(
        ThoroughRecalculationBackgroundService backgroundService,
        ILogger<RecalculatePeriodHoursThoroughCommandHandler> logger)
        : base(logger)
    {
        _backgroundService = backgroundService;
    }

    public Task<bool> Handle(RecalculatePeriodHoursThoroughCommand request, CancellationToken cancellationToken)
    {
        return ExecuteAsync(() =>
        {
            var queued = _backgroundService.QueueRecalculation(
                request.StartDate,
                request.EndDate,
                request.SelectedGroup,
                request.AnalyseToken);
            return Task.FromResult(queued);
        },
        "queuing thorough recalculation",
        new { request.StartDate, request.EndDate, request.SelectedGroup, request.AnalyseToken });
    }
}
