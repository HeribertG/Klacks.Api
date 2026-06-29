// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Enqueues a thorough recalculation onto the thorough-recalculation queue.
/// Returns true when the request was accepted into the channel.
/// </summary>
/// <param name="queue">Queue for thorough recalculations</param>
/// <param name="logger">Logger for diagnostics</param>

using Klacks.Api.Application.Commands.Works;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Works;

public class RecalculatePeriodHoursThoroughCommandHandler : BaseHandler, IRequestHandler<RecalculatePeriodHoursThoroughCommand, bool>
{
    private readonly IThoroughRecalculationQueue _queue;

    public RecalculatePeriodHoursThoroughCommandHandler(
        IThoroughRecalculationQueue queue,
        ILogger<RecalculatePeriodHoursThoroughCommandHandler> logger)
        : base(logger)
    {
        _queue = queue;
    }

    public Task<bool> Handle(RecalculatePeriodHoursThoroughCommand request, CancellationToken cancellationToken)
    {
        return ExecuteAsync(() =>
        {
            var queued = _queue.QueueRecalculation(
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
