// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Takes an applied move back: the work returns to the person it came from through the Work reassignment, with all its guards
/// and a new macro run. Only the last move of a work can go back: when the work has moved on since, something holds it in
/// place, or the person it came from is not free at its time (another service, or the absence that caused the move is still
/// running), the planner is told why and nothing changes. The recorded answer stays.
/// </summary>
/// <param name="repository">Finds the Move row</param>
/// <param name="clientVisibilityGuard">Refuses a row of a person outside the caller's visibility like a missing one</param>
/// <param name="scenarioTokenAccessGuard">Refuses a row of a scenario still open and hidden from the caller like a missing one</param>
/// <param name="workRepository">Loads the moved work</param>
/// <param name="reversalCheck">The checks every way of taking a move back shares (held, busy origin)</param>
/// <param name="mediator">Runs the Work reassignment</param>

using Klacks.Api.Application.Commands.Schedules;
using Klacks.Api.Application.Commands.Works;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Schedules;

public sealed class ReverseMoveCommandHandler : IRequestHandler<ReverseMoveCommand, ReassignWorkClientResponse?>
{
    private const string NotFoundMessage = "Replacement request with ID {0} not found";
    private const string NotAnAppliedMoveMessage = "Only a moved service that reached the plan can be taken back.";
    private const string AlreadyReversedMessage = "This move was already taken back.";
    private const string AlreadyBackMessage = "This service is already back with the person it came from.";
    private const string MovedOnMessage = "This service has moved on since; only its last move can be taken back.";

    private readonly IReplacementRequestRepository _repository;
    private readonly IClientVisibilityGuard _clientVisibilityGuard;
    private readonly IScenarioTokenAccessGuard _scenarioTokenAccessGuard;
    private readonly IWorkRepository _workRepository;
    private readonly IMoveReversalCheck _reversalCheck;
    private readonly IMediator _mediator;

    public ReverseMoveCommandHandler(
        IReplacementRequestRepository repository,
        IClientVisibilityGuard clientVisibilityGuard,
        IScenarioTokenAccessGuard scenarioTokenAccessGuard,
        IWorkRepository workRepository,
        IMoveReversalCheck reversalCheck,
        IMediator mediator)
    {
        _repository = repository;
        _clientVisibilityGuard = clientVisibilityGuard;
        _scenarioTokenAccessGuard = scenarioTokenAccessGuard;
        _workRepository = workRepository;
        _reversalCheck = reversalCheck;
        _mediator = mediator;
    }

    public async Task<ReassignWorkClientResponse?> Handle(ReverseMoveCommand request, CancellationToken cancellationToken)
    {
        var row = await _repository.Get(request.RequestId);
        if (row is null || row.IsDeleted
            || !await _clientVisibilityGuard.AreAllVisibleAsync([row.CandidateClientId, row.AbsentClientId], cancellationToken)
            || (row.AppliedAtUtc is null && !await _scenarioTokenAccessGuard.IsVisibleAsync(row.AnalyseToken, cancellationToken)))
        {
            throw new KeyNotFoundException(string.Format(NotFoundMessage, request.RequestId));
        }

        if (row.Kind != ReplacementRequestKind.Move || row.AppliedAtUtc is null || row.WorkId is not { } workId)
        {
            throw new MoveReversalRefusedException(NotAnAppliedMoveMessage, MoveReversalRefusalCodes.NotAnAppliedMove);
        }

        if (row.ReversedAtUtc is not null)
        {
            throw new MoveReversalRefusedException(AlreadyReversedMessage, MoveReversalRefusalCodes.AlreadyReversed);
        }

        var work = await _workRepository.GetNoTracking(workId);
        if (work?.ClientId == row.AbsentClientId)
        {
            throw new MoveReversalRefusedException(AlreadyBackMessage, MoveReversalRefusalCodes.AlreadyBack);
        }

        if (work is null || work.ClientId != row.CandidateClientId || work.MovedFromClientId != row.AbsentClientId)
        {
            throw new MoveReversalRefusedException(MovedOnMessage, MoveReversalRefusalCodes.MovedOn);
        }

        await _reversalCheck.EnsureReversibleAsync(work, cancellationToken);

        return await _mediator.Send(new ReassignWorkClientCommand(workId, row.AbsentClientId), cancellationToken);
    }
}
