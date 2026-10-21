// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Gate (1) of the action dispatcher: never auto-handle a row that an earlier Klacksy execution may have
/// produced. Writes the provenance link the first time it recognises such a row, so the attribution
/// survives the tick.
/// </summary>
/// <param name="ledgerService">Writes the CausedBy provenance link.</param>
/// <param name="logger">The dispatcher's logger, so the log category stays the dispatcher's.</param>

using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Conditions;

internal sealed class ConditionCascadeGuard
{
    private const string CascadeDetectedMessage =
        "Condition {ConditionId} appeared after Klacksy executed condition {CauseId} on the same "
        + "target and is only ever hinted from now on";

    private readonly IAgentConditionLedgerService _ledgerService;
    private readonly ILogger _logger;

    public ConditionCascadeGuard(IAgentConditionLedgerService ledgerService, ILogger logger)
    {
        _ledgerService = ledgerService;
        _logger = logger;
    }

    /// <summary>
    /// True when this row must never be auto-handled because Klacksy itself may have produced it: either
    /// it already carries a provenance link, or it was detected after a Klacksy execution on the same
    /// entity within one scan interval - in which case the link is written now, so the attribution
    /// survives this tick. Matching is on EntityId when the candidate has one and falls back to GroupId
    /// only when it does not: the cascade guard exists to catch "my fix broke the thing I touched", not
    /// "something happened nearby".
    /// </summary>
    public async Task<bool> IsCascadeAsync(
        AgentCondition condition,
        IReadOnlyList<AgentCondition> recentExecutions,
        CancellationToken cancellationToken)
    {
        if (condition.CausedByConditionId is not null)
        {
            return true;
        }

        var cause = FindCause(condition, recentExecutions);
        if (cause is not Guid causeId)
        {
            return false;
        }

        await _ledgerService.TrySetCausedByAsync(condition.Id, causeId, cancellationToken);
        _logger.LogInformation(CascadeDetectedMessage, condition.Id, causeId);

        return true;
    }

    private static Guid? FindCause(AgentCondition condition, IReadOnlyList<AgentCondition> recentExecutions)
    {
        foreach (var executed in recentExecutions)
        {
            if (executed.Id == condition.Id || executed.HandledAtUtc is not { } handledAtUtc)
            {
                continue;
            }

            if (condition.DetectedAtUtc < handledAtUtc)
            {
                continue;
            }

            if (condition.EntityId is { } entityId)
            {
                if (executed.EntityId == entityId)
                {
                    return executed.Id;
                }

                continue;
            }

            if (condition.GroupId is { } groupId && executed.GroupId == groupId)
            {
                return executed.Id;
            }
        }

        return null;
    }
}
