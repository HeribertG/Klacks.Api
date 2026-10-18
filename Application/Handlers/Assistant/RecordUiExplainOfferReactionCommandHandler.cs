// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Records the caller's reaction to a UI explain offer; false for an unknown topic or outcome.
/// </summary>
/// <param name="offers">Writes the counter row and the dosage mute of the caller</param>

using Klacks.Api.Application.Commands.Assistant;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant;

public class RecordUiExplainOfferReactionCommandHandler : IRequestHandler<RecordUiExplainOfferReactionCommand, bool>
{
    private readonly IUiExplainOfferService _offers;

    public RecordUiExplainOfferReactionCommandHandler(IUiExplainOfferService offers)
    {
        _offers = offers;
    }

    public async Task<bool> Handle(RecordUiExplainOfferReactionCommand request, CancellationToken cancellationToken)
    {
        return await _offers.RecordReactionAsync(request.UserId, request.TopicKey, request.Outcome, cancellationToken);
    }
}
