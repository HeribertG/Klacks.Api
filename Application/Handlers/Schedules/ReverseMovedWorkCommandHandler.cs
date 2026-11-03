// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Takes the last move of a work back from the schedule: the work returns to the person it came from through the Work
/// reassignment, which clears the origin, stamps an applied Move row and runs the macros again. A work that carries no
/// origin is already back, a work something holds in place is refused; both with a code the client explains. A work
/// outside the caller's visibility reads like a missing one.
/// </summary>
/// <param name="workRepository">Loads the work</param>
/// <param name="rowAccessGuard">Decides whether the caller may see the work's owner and scenario</param>
/// <param name="factsReader">Tells whether something holds the work in place</param>
/// <param name="mediator">Runs the Work reassignment</param>

using Klacks.Api.Application.Commands.Schedules;
using Klacks.Api.Application.Commands.Works;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Services.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Schedules;

public sealed class ReverseMovedWorkCommandHandler : IRequestHandler<ReverseMovedWorkCommand, ReassignWorkClientResponse?>
{
    private const string NotFoundMessage = "Work with ID {0} not found.";
    private const string AlreadyBackMessage = "This service is already back with the person it came from.";
    private const string HeldMessage = "This service is sealed or replaced and can no longer change person.";

    private readonly IWorkRepository _workRepository;
    private readonly IScheduleRowAccessGuard _rowAccessGuard;
    private readonly ICoverMoveFactsReader _factsReader;
    private readonly IMediator _mediator;

    public ReverseMovedWorkCommandHandler(
        IWorkRepository workRepository,
        IScheduleRowAccessGuard rowAccessGuard,
        ICoverMoveFactsReader factsReader,
        IMediator mediator)
    {
        _workRepository = workRepository;
        _rowAccessGuard = rowAccessGuard;
        _factsReader = factsReader;
        _mediator = mediator;
    }

    public async Task<ReassignWorkClientResponse?> Handle(ReverseMovedWorkCommand request, CancellationToken cancellationToken)
    {
        var work = await _workRepository.GetNoTracking(request.WorkId);
        if (work is null || !await _rowAccessGuard.IsVisibleAsync(work.ClientId, work.AnalyseToken, cancellationToken))
        {
            throw new KeyNotFoundException(string.Format(NotFoundMessage, request.WorkId));
        }

        if (work.MovedFromClientId is not { } origin)
        {
            throw new MoveReversalRefusedException(AlreadyBackMessage, MoveReversalRefusalCodes.AlreadyBack);
        }

        var facts = await _factsReader.LoadAsync([work.Id], work.AnalyseToken, cancellationToken);
        if (!facts.TryGetValue(work.Id, out var fact) || !CoverMoveDecision.IsUntouched(fact))
        {
            throw new MoveReversalRefusedException(HeldMessage, MoveReversalRefusalCodes.Held);
        }

        return await _mediator.Send(new ReassignWorkClientCommand(work.Id, origin), cancellationToken);
    }
}
