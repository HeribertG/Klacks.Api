// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Enqueues the thorough recalculation of all unsealed works and breaks onto the thorough-recalculation queue.
/// Returns true when the request was accepted (or an identical one is already waiting), false when the queue is full.
/// </summary>
/// <param name="queue">Queue for thorough recalculations</param>
/// <param name="logger">Logger for diagnostics</param>

using Klacks.Api.Application.Commands.Works;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Works;

public class RecalculateAllUnsealedCommandHandler : BaseHandler, IRequestHandler<RecalculateAllUnsealedCommand, bool>
{
    private readonly IThoroughRecalculationQueue _queue;

    public RecalculateAllUnsealedCommandHandler(
        IThoroughRecalculationQueue queue,
        ILogger<RecalculateAllUnsealedCommandHandler> logger)
        : base(logger)
    {
        _queue = queue;
    }

    public Task<bool> Handle(RecalculateAllUnsealedCommand request, CancellationToken cancellationToken)
    {
        return ExecuteAsync(
            () => Task.FromResult(_queue.QueueRecalculationOfAllUnsealed()),
            "queuing thorough recalculation of all unsealed entries",
            new { });
    }
}
