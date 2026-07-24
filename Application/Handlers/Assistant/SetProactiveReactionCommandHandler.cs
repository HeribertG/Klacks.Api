// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Stores a user's reaction (helpful / dismissed) on a proactive message they received. Returns
/// false when the dispatch row does not exist or belongs to a different user, so the caller can
/// answer with not found without leaking foreign rows.
/// </summary>
/// <param name="dispatchRepository">Persistence of the proactive trigger dispatch rows.</param>

using Klacks.Api.Application.Commands.Assistant;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant;

public class SetProactiveReactionCommandHandler : IRequestHandler<SetProactiveReactionCommand, bool>
{
    private readonly IProactiveTriggerDispatchRepository _dispatchRepository;

    public SetProactiveReactionCommandHandler(IProactiveTriggerDispatchRepository dispatchRepository)
    {
        _dispatchRepository = dispatchRepository;
    }

    public async Task<bool> Handle(SetProactiveReactionCommand request, CancellationToken cancellationToken)
    {
        var row = await _dispatchRepository.GetByIdAsync(request.Id, cancellationToken);
        if (row == null || !string.Equals(row.UserId, request.UserId, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        row.Reaction = request.Reaction;
        row.ReactionAtUtc = DateTime.UtcNow;
        await _dispatchRepository.UpdateAsync(row, cancellationToken);
        return true;
    }
}
