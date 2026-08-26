// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Stores a user's reaction (helpful / dismissed) on a proactive message they received. Returns
/// false when the dispatch row does not exist or belongs to a different user, so the caller can
/// answer with not found without leaking foreign rows. After a stored reaction three follow-ups
/// run, all strictly secondary to the stored reaction and none able to fail the request: the
/// helpful-boost evaluator recomputes the kind's daily budget boost for the user, and on a
/// dismissal the dismiss-streak evaluator may ask the user once whether the trigger kind should
/// be muted, and a dismissal of a message that reported a condition-ledger finding rejects that
/// finding with the given reason. The reaction is persisted before any of them run, because it is
/// the one effect the user asked for and the only one that is guaranteed to be possible.
/// </summary>
/// <param name="dispatchRepository">Persistence of the proactive trigger dispatch rows.</param>
/// <param name="dismissStreakEvaluator">Fires a mute suggestion after repeated dismissals.</param>
/// <param name="ledgerService">Writes the rejection back onto the condition-ledger row the message reported.</param>
/// <param name="helpfulBoostEvaluator">Recomputes the helpful-learned daily budget boost.</param>
/// <param name="logger">Logs follow-up failures without failing the request.</param>

using Klacks.Api.Application.Commands.Assistant;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant;

public class SetProactiveReactionCommandHandler : IRequestHandler<SetProactiveReactionCommand, bool>
{
    private readonly IProactiveTriggerDispatchRepository _dispatchRepository;
    private readonly IDismissStreakEvaluator _dismissStreakEvaluator;
    private readonly IAgentConditionLedgerService _ledgerService;
    private readonly IHelpfulBoostEvaluator _helpfulBoostEvaluator;
    private readonly ILogger<SetProactiveReactionCommandHandler> _logger;

    public SetProactiveReactionCommandHandler(
        IProactiveTriggerDispatchRepository dispatchRepository,
        IDismissStreakEvaluator dismissStreakEvaluator,
        IAgentConditionLedgerService ledgerService,
        IHelpfulBoostEvaluator helpfulBoostEvaluator,
        ILogger<SetProactiveReactionCommandHandler> logger)
    {
        _dispatchRepository = dispatchRepository;
        _dismissStreakEvaluator = dismissStreakEvaluator;
        _ledgerService = ledgerService;
        _helpfulBoostEvaluator = helpfulBoostEvaluator;
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
            await RejectLedgerConditionAsync(row, request.RejectReason, cancellationToken);

            try
            {
                await _dismissStreakEvaluator.EvaluateAsync(row.UserId, row.TriggerKind, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Dismiss-streak evaluation failed for user {UserId}, kind {TriggerKind}", row.UserId, row.TriggerKind);
            }
        }

        try
        {
            await _helpfulBoostEvaluator.EvaluateAsync(row.UserId, row.TriggerKind, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Helpful-boost evaluation failed for user {UserId}, kind {TriggerKind}", row.UserId, row.TriggerKind);
        }

        return true;
    }

    /// <summary>
    /// Best-effort write-back of the rejection onto the finding the message reported. Only rows that
    /// carry a ConditionId take part - the majority do not, because only detector events admitted by
    /// AgentConditionLedgerPolicy ever open a ledger row. Not reaching Rejected is an ordinary outcome
    /// and stays at information level: the row may already be Executed, Resolved or Escalated, or still
    /// Detected, from none of which the state machine grants Rejected. The dismissal itself is already
    /// persisted at this point and must survive every one of those cases.
    /// </summary>
    private async Task RejectLedgerConditionAsync(
        ProactiveTriggerDispatchRow row,
        AgentConditionRejectReason? rejectReason,
        CancellationToken cancellationToken)
    {
        if (row.ConditionId is not Guid conditionId)
        {
            return;
        }

        try
        {
            var rejected = await _ledgerService.TryRejectAsync(
                conditionId,
                rejectReason ?? AgentConditionRejectReason.NoReason,
                RejectingUserId(row.UserId),
                cancellationToken);

            if (!rejected)
            {
                _logger.LogInformation(
                    "Condition {ConditionId} was not marked rejected after user {UserId} dismissed its message; the dismissal itself is stored",
                    conditionId,
                    row.UserId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Rejecting condition {ConditionId} after a dismissal by user {UserId} failed; the dismissal itself is stored",
                conditionId,
                row.UserId);
        }
    }

    /// <summary>
    /// The ledger stores the rejecting user as a Guid while a dispatch row carries the identity user id
    /// as a string. Every Klacks user id is a Guid, so a value that does not parse means the row was
    /// written by something that is not a user; the rejection is then recorded without an author rather
    /// than abandoned, because who rejected matters less than that the finding was rejected.
    /// </summary>
    private static Guid? RejectingUserId(string userId) =>
        Guid.TryParse(userId, out var parsed) ? parsed : null;
}
