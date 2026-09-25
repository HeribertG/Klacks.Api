// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Drops what a stopped turn left in the pending-confirmation store: the tokens and proposal hints the turn
/// registered on its scope, and the correction-undo offer when the turn made one. Tokens of earlier turns
/// stay redeemable. A hint created this turn has already replaced the user's earlier hint (the store keeps
/// one hint per user), so discarding it by name loses nothing the turn had not already replaced.
/// </summary>
/// <param name="turnScope">The confirmations this turn issued</param>
/// <param name="confirmationStore">Holds the pending confirmations</param>
/// <param name="logger">Logs a store failure</param>

using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Autonomy;

public class TurnConfirmationDiscarder : ITurnConfirmationDiscarder
{
    private readonly ITurnConfirmationScope _turnScope;
    private readonly IPendingConfirmationStore _confirmationStore;
    private readonly ILogger<TurnConfirmationDiscarder> _logger;

    public TurnConfirmationDiscarder(
        ITurnConfirmationScope turnScope,
        IPendingConfirmationStore confirmationStore,
        ILogger<TurnConfirmationDiscarder> logger)
    {
        _turnScope = turnScope;
        _confirmationStore = confirmationStore;
        _logger = logger;
    }

    public void DiscardIssuedThisTurn(Guid userId, bool correctionUndoOffered)
    {
        try
        {
            if (_turnScope.IssuedTokens.Count > 0)
            {
                _confirmationStore.DiscardByTokens(userId, _turnScope.IssuedTokens);
            }

            foreach (var applySkill in _turnScope.ProposalHintSkills)
            {
                _confirmationStore.DiscardProposalHints(userId, applySkill);
            }

            if (correctionUndoOffered)
            {
                _confirmationStore.DiscardCorrectionUndo(userId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Dropping the confirmations of a stopped turn failed for user {UserId}", userId);
        }
    }
}
