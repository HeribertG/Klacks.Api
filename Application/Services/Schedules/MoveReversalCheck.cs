// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The checks every way of taking a move back shares: an origin, nothing holding the work, and a free origin person.
/// </summary>
/// <param name="factsReader">Reads what holds the work and whether the origin person is free</param>

using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Services.Schedules;

namespace Klacks.Api.Application.Services.Schedules;

public sealed class MoveReversalCheck : IMoveReversalCheck
{
    private const string AlreadyBackMessage = "This service is already back with the person it came from.";
    private const string HeldMessage = "This service is sealed or replaced and can no longer change person.";
    private const string OriginBusyMessage =
        "The person this service came from is not free at its time: another service or an absence is in the way.";

    private readonly ICoverMoveFactsReader _factsReader;

    public MoveReversalCheck(ICoverMoveFactsReader factsReader)
    {
        _factsReader = factsReader;
    }

    public async Task EnsureReversibleAsync(Work work, CancellationToken cancellationToken)
    {
        if (work.MovedFromClientId is not { } origin)
        {
            throw new MoveReversalRefusedException(AlreadyBackMessage, MoveReversalRefusalCodes.AlreadyBack);
        }

        var facts = await _factsReader.LoadAsync([work.Id], work.AnalyseToken, cancellationToken);
        if (!facts.TryGetValue(work.Id, out var fact) || !CoverMoveDecision.IsUntouched(fact))
        {
            throw new MoveReversalRefusedException(HeldMessage, MoveReversalRefusalCodes.Held);
        }

        if (!await _factsReader.IsFreeAsync(work.AnalyseToken, origin, work.Id, cancellationToken))
        {
            throw new MoveReversalRefusedException(OriginBusyMessage, MoveReversalRefusalCodes.OriginBusy);
        }
    }
}
