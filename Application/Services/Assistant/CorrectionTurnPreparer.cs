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

    private readonly ISkillRegistry _skillRegistry;
    private readonly ISkillPermissionGate _permissionGate;
    private readonly ILogger<CorrectionTurnPreparer> _logger;
    private readonly ITurnConfirmationScope _turnScope;

    public CorrectionTurnPreparer(
        IAssistantLastActionStore lastActionStore,
        IPendingRecipeStore pendingRecipeStore,
        ITurnPreparationService turnPreparation,
        ISkillToolsetAssembler toolsetAssembler,
        IPendingConfirmationStore pendingConfirmationStore,
        ISkillRegistry skillRegistry,
        ISkillPermissionGate permissionGate,
        ILogger<CorrectionTurnPreparer> logger,
        ITurnConfirmationScope turnScope)
    {
        _lastActionStore = lastActionStore;
        _pendingRecipeStore = pendingRecipeStore;
        _turnPreparation = turnPreparation;
        _toolsetAssembler = toolsetAssembler;
        _pendingConfirmationStore = pendingConfirmationStore;
        _skillRegistry = skillRegistry;
        _permissionGate = permissionGate;
        _logger = logger;
        _turnScope = turnScope;
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
            var undoIsPermitted = await UndoIsPermittedAsync(correctionPlan, userId, cancellationToken);

            try
            {
                correction = _turnPreparation.CompleteCorrection(
                    correctionPlan, toolset.Functions, language, undoIsPermitted);
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
                var undoToken = _pendingConfirmationStore.Create(
                    userGuid,
                    correction.Undo.SkillName,
                    correction.Undo.Arguments,
                    PendingConfirmationPurposes.CorrectionUndo);
                _turnScope.MarkIssued(undoToken);
                undoWasHeld = true;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Could not register the undo offer for skill {Skill}", correction.Undo.SkillName);
            }
        }

        return new CorrectionTurnPreparation(toolset, correction, undoWasHeld);
    }

    /// <summary>
    /// Whether this account may release the inverse skill the correction would offer to run. Asked HERE,
    /// before the note is composed, because a refusal has to cost the offer as a whole: the sentence and
    /// the redeemable token are one promise, and SkillExecutorService.ValidatePermissions would refuse the
    /// redemption anyway - create_group is reversed by delete_group, which stays Admin-only, so a
    /// Supervisor was offered an undo that was then denied. The success message is unaffected.
    /// Fails closed on every uncertainty: no inverse, an inverse no longer in the registry (its
    /// redemption would answer "skill not found"), or a gate that threw. The Admin bypass and the role
    /// expansion are the gate's own, which is why the check is not repeated against the turn's rights.
    /// </summary>
    /// <param name="plan">The correction the planning decided on</param>
    /// <param name="userId">The account behind this turn, as the chat entry point resolved it</param>
    /// <param name="cancellationToken">Cancellation of the turn</param>
    private async Task<bool> UndoIsPermittedAsync(
        GracefulCorrectionPlan plan, string userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var undo = _turnPreparation.PeekUndo(plan);
            if (undo == null)
            {
                return false;
            }

            var inverse = _skillRegistry.GetSkillByName(undo.SkillName);
            if (inverse == null)
            {
                _logger.LogWarning(
                    "Graceful correction: the inverse skill '{Inverse}' is not registered, so no undo is offered.",
                    undo.SkillName);
                return false;
            }

            return await _permissionGate.HoldsAsync(userId, inverse.RequiredPermissions);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex,
                "Could not check whether user {UserId} may release the undo; no undo is offered.", userId);
            return false;
        }
    }
}
