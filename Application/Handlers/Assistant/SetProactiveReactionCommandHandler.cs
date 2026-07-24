// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Stores a user's reaction (helpful / dismissed) on a proactive message they received. Returns
/// false when the dispatch row does not exist or belongs to a different user, so the caller can
/// answer with not found without leaking foreign rows. After a stored dismissal the dismiss-streak
/// evaluator may ask the user once whether the trigger kind should be muted; an evaluator failure
/// never fails the reaction request.
/// </summary>
/// <param name="dispatchRepository">Persistence of the proactive trigger dispatch rows.</param>
/// <param name="dismissStreakEvaluator">Fires a mute suggestion after repeated dismissals.</param>
/// <param name="logger">Logs evaluator failures without failing the request.</param>

using Klacks.Api.Application.Commands.Assistant;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant;

public class SetProactiveReactionCommandHandler : IRequestHandler<SetProactiveReactionCommand, bool>
{
    private readonly IProactiveTriggerDispatchRepository _dispatchRepository;
    private readonly IDismissStreakEvaluator _dismissStreakEvaluator;
    private readonly ILogger<SetProactiveReactionCommandHandler> _logger;

    public SetProactiveReactionCommandHandler(
        IProactiveTriggerDispatchRepository dispatchRepository,
        IDismissStreakEvaluator dismissStreakEvaluator,
        ILogger<SetProactiveReactionCommandHandler> logger)
    {
        _dispatchRepository = dispatchRepository;
        _dismissStreakEvaluator = dismissStreakEvaluator;
        _logger = logger;
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

        if (request.Reaction == ProactiveReaction.Dismissed)
        {
            try
            {
                await _dismissStreakEvaluator.EvaluateAsync(row.UserId, row.TriggerKind, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Dismiss-streak evaluation failed for user {UserId}, kind {TriggerKind}", row.UserId, row.TriggerKind);
            }
        }

        return true;
    }
}
