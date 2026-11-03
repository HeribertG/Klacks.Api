// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Takes the last move of a work back from the schedule: the work returns to the person it came from through the Work
/// reassignment, which clears the origin, stamps an applied Move row and runs the macros again. The shared check refuses a
/// work already back, a held one and a busy origin person, each with a code the client explains. A work outside the
/// caller's visibility reads like a missing one.
/// </summary>
/// <param name="workRepository">Loads the work</param>
/// <param name="rowAccessGuard">Decides whether the caller may see the work's owner and scenario</param>
/// <param name="reversalCheck">The checks every way of taking a move back shares</param>
/// <param name="mediator">Runs the Work reassignment</param>

using Klacks.Api.Application.Commands.Schedules;
using Klacks.Api.Application.Commands.Works;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Schedules;

public sealed class ReverseMovedWorkCommandHandler : IRequestHandler<ReverseMovedWorkCommand, ReassignWorkClientResponse?>
{
    private const string NotFoundMessage = "Work with ID {0} not found.";

    private readonly IWorkRepository _workRepository;
    private readonly IScheduleRowAccessGuard _rowAccessGuard;
    private readonly IMoveReversalCheck _reversalCheck;
    private readonly IMediator _mediator;

    public ReverseMovedWorkCommandHandler(
        IWorkRepository workRepository,
        IScheduleRowAccessGuard rowAccessGuard,
        IMoveReversalCheck reversalCheck,
        IMediator mediator)
    {
        _workRepository = workRepository;
        _rowAccessGuard = rowAccessGuard;
        _reversalCheck = reversalCheck;
        _mediator = mediator;
    }

    public async Task<ReassignWorkClientResponse?> Handle(ReverseMovedWorkCommand request, CancellationToken cancellationToken)
    {
        var work = await _workRepository.GetNoTracking(request.WorkId);
        if (work is null || !await _rowAccessGuard.IsVisibleAsync(work.ClientId, work.AnalyseToken, cancellationToken))
        {
            throw new KeyNotFoundException(string.Format(NotFoundMessage, request.WorkId));
        }

        await _reversalCheck.EnsureReversibleAsync(work, cancellationToken);
        return await _mediator.Send(new ReassignWorkClientCommand(work.Id, work.MovedFromClientId!.Value), cancellationToken);
    }
}
