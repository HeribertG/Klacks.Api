// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// See ICorrectionTurnPreparer. Extracted out of the two chat entry points, which had grown an
/// ~90-line, near byte-identical copy of this block each - exactly the divergence risk
/// ITurnPreparationService exists to prevent, reproduced one layer up because toolset assembly (needed
/// between planning and completing a correction) cannot run inside ITurnPreparationService itself: that
/// service is Domain and must not depend on ISkillToolsetAssembler (Application).
/// </summary>

using Klacks.Api.Application.Interfaces.Assistant;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Assistant;

public class CorrectionTurnPreparer : ICorrectionTurnPreparer
{
    private readonly IAssistantLastActionStore _lastActionStore;
    private readonly IPendingRecipeStore _pendingRecipeStore;
    private readonly ITurnPreparationService _turnPreparation;
    private readonly ISkillToolsetAssembler _toolsetAssembler;

    /// <summary>
    /// Holds the one-time token of an undo offer. Written HERE ONLY, never in ITurnPreparationService: a
    /// headless replay resolves the same undo as data and must leave no redeemable token behind. The
    /// token carries PendingConfirmationPurposes.GateReplay because it is redeemed exactly like any other
    /// held invocation - an affirmation narrows the next turn to confirm_pending_action, which replays
    /// these arguments. Known limitation, not introduced here: PeekLatestForUser looks up the latest
    /// token PER USER, not per conversation, so an affirmation in another conversation of the same user
    /// that is open at the same time can redeem this one. TP1 narrows the window (the token exists only
    /// on the non-ambiguous path and only when an offer was actually made); conversation-scoped
    /// confirmations are TP2 work.
    /// </summary>
    private readonly IPendingConfirmationStore _pendingConfirmationStore;

    private readonly ILogger<CorrectionTurnPreparer> _logger;

    public CorrectionTurnPreparer(
        IAssistantLastActionStore lastActionStore,
        IPendingRecipeStore pendingRecipeStore,
        ITurnPreparationService turnPreparation,
        ISkillToolsetAssembler toolsetAssembler,
        IPendingConfirmationStore pendingConfirmationStore,
        ILogger<CorrectionTurnPreparer> logger)
    {
        _lastActionStore = lastActionStore;
        _pendingRecipeStore = pendingRecipeStore;
        _turnPreparation = turnPreparation;
        _toolsetAssembler = toolsetAssembler;
        _pendingConfirmationStore = pendingConfirmationStore;
        _logger = logger;
    }

    public async Task<CorrectionTurnPreparation> PrepareAsync(
        Agent? agent,
        List<string> userRights,
        string message,
        string? conversationId,
        string userId,
        string? language,
        string? currentRoute,
        int maxToolsForProvider,
        CancellationToken cancellationToken)
    {
        Guid.TryParse(userId, out var userGuid);
        var hasConversation = userGuid != Guid.Empty && !string.IsNullOrEmpty(conversationId);

        AssistantLastAction? lastAction = null;
        GracefulCorrectionPlan? correctionPlan = null;
        try
        {
            if (hasConversation)
            {
                lastAction = _lastActionStore.Peek(userGuid, conversationId!);
            }

            var recipeIsActive = lastAction?.CanAnchorCorrection(DateTime.UtcNow) == true
                && _pendingRecipeStore.Peek(userGuid, conversationId!) != null;

            correctionPlan = await _turnPreparation.PlanCorrectionAsync(
                new GracefulCorrectionInput(
                    agent, userRights, message, conversationId, userId, language, lastAction, recipeIsActive),
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex,
                "Graceful correction planning failed for user {UserId}; continuing as an ordinary turn.",
                userId);
        }

        SkillToolsetResult toolset;
        try
        {
            toolset = await _toolsetAssembler.AssembleAsync(
                agent, userRights, correctionPlan?.CompositeMessage ?? message,
                conversationId, currentRoute, userId, language,
                maxToolsForProvider, applyLearnedPhraseGuarantee: true,
                excludedSkillNames: correctionPlan?.ExcludedSkillNames,
                pinnedSkillNames: lastAction?.ClarificationSkillNames,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex,
                "Error assembling skill toolset - this turn runs with ZERO tools, so the assistant cannot " +
                "perform any action and may answer as if it had. User {UserId}, conversation {ConversationId}",
                userId, conversationId);
            toolset = new SkillToolsetResult();
        }

        GracefulCorrectionOutcome? correction = null;
        if (correctionPlan != null)
        {
            try
            {
                correction = _turnPreparation.CompleteCorrection(correctionPlan, toolset.Functions, language);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex,
                    "Completing the graceful correction failed for user {UserId}; continuing as an ordinary turn.",
                    userId);
            }
        }

        if (correction is { ClarificationReply.Length: > 0, ClarificationSkillNames.Count: > 0 } && hasConversation)
        {
            try
            {
                _lastActionStore.SaveClarificationCandidates(
                    userGuid, conversationId!, correction.ClarificationSkillNames);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex,
                    "Could not pin the clarification candidates for user {UserId}; the follow-up turn runs without them.",
                    userId);
            }
        }

        var undoWasHeld = false;
        if (correction?.Undo != null && userGuid != Guid.Empty)
        {
            try
            {
                _pendingConfirmationStore.Create(
                    userGuid,
                    correction.Undo.SkillName,
                    correction.Undo.Arguments,
                    PendingConfirmationPurposes.CorrectionUndo);
                undoWasHeld = true;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Could not register the undo offer for skill {Skill}", correction.Undo.SkillName);
            }
        }

        return new CorrectionTurnPreparation(toolset, correction, undoWasHeld);
    }
}
