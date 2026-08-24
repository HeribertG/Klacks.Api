// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Write side of the condition ledger. Holds the lifecycle rules (via AgentConditionStateMachine) and the
/// re-arm and resolve semantics; the repository below it only knows how to make a conditional update
/// atomic. Timestamps come from the injected TimeProvider so a test can drive the clock instead of racing
/// the wall clock.
/// </summary>
/// <param name="repository">Ledger persistence, including the compare-and-swap transitions.</param>
/// <param name="timeProvider">Source of DetectedAtUtc, LastSeenAtUtc and the status-derived timestamps.</param>
/// <param name="logger">Records the one case the ledger cannot resolve itself: a fingerprint claimed by another detector kind.</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Conditions;

public class AgentConditionLedgerService : IAgentConditionLedgerService
{
    private const string EmptyPayloadJson = "{}";

    private readonly IAgentConditionRepository _repository;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<AgentConditionLedgerService> _logger;

    public AgentConditionLedgerService(
        IAgentConditionRepository repository,
        TimeProvider timeProvider,
        ILogger<AgentConditionLedgerService> logger)
    {
        _repository = repository;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<(AgentCondition Condition, bool IsNew)> UpsertDetectedAsync(
        string triggerKind,
        string fingerprint,
        Guid? entityId,
        Guid? groupId,
        string severity,
        string payloadJson,
        CancellationToken cancellationToken = default)
    {
        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        var existing = await _repository.FindOpenByFingerprintAsync(fingerprint, cancellationToken);
        if (existing != null)
        {
            return (await TouchAsync(existing, triggerKind, nowUtc, cancellationToken), false);
        }

        var condition = NewCondition(triggerKind, fingerprint, entityId, groupId, severity, payloadJson, nowUtc);
        var inserted = await _repository.InsertAsync(condition, DetectionEvent(condition.Id, nowUtc), cancellationToken);
        if (inserted != null)
        {
            return (inserted, true);
        }

        var winner = await _repository.FindOpenByFingerprintAsync(fingerprint, cancellationToken);
        if (winner == null)
        {
            throw new ConcurrencyException(
                $"Opening a ledger row for fingerprint '{fingerprint}' was rejected as a duplicate, but no open row for it exists.");
        }

        return (await TouchAsync(winner, triggerKind, nowUtc, cancellationToken), false);
    }

    public async Task<int> MarkResolvedAsync(
        string triggerKind,
        IReadOnlySet<string> completeFingerprintSet,
        CancellationToken cancellationToken = default)
    {
        var openConditions = await _repository.GetOpenByKindAsync(triggerKind, cancellationToken);
        var resolvedCount = 0;

        foreach (var condition in openConditions)
        {
            if (completeFingerprintSet.Contains(condition.Fingerprint))
            {
                continue;
            }

            var resolved = await TryTransitionAsync(
                condition.Id,
                condition.Status,
                AgentConditionStatus.Resolved,
                cancellationToken: cancellationToken);

            if (resolved)
            {
                resolvedCount++;
            }
        }

        return resolvedCount;
    }

    public async Task<bool> TryTransitionAsync(
        Guid conditionId,
        AgentConditionStatus fromStatus,
        AgentConditionStatus toStatus,
        Guid? userId = null,
        string? detail = null,
        AgentConditionTransitionFields? fields = null,
        CancellationToken cancellationToken = default)
    {
        if (!AgentConditionStateMachine.IsLegalTransition(fromStatus, toStatus))
        {
            throw new InvalidRequestException(
                $"Condition status transition {fromStatus} -> {toStatus} is not part of the condition-ledger state machine.");
        }

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        var auditEvent = new AgentConditionEvent
        {
            Id = Guid.NewGuid(),
            ConditionId = conditionId,
            EventType = toStatus.ToString(),
            AtUtc = nowUtc,
            UserId = userId,
            Detail = detail
        };

        return await _repository.TryTransitionAsync(
            conditionId,
            fromStatus,
            toStatus,
            WithStatusTimestamps(fields, toStatus, nowUtc),
            auditEvent,
            cancellationToken);
    }

    public async Task<bool> TryRejectAsync(
        Guid conditionId,
        AgentConditionRejectReason rejectReason,
        Guid? rejectedByUserId,
        CancellationToken cancellationToken = default)
    {
        var condition = await _repository.GetByIdAsync(conditionId, cancellationToken);
        if (condition == null)
        {
            _logger.LogInformation(
                "Condition {ConditionId} was rejected by a user but no ledger row carries that id; the rejection is recorded on the notification only.",
                conditionId);

            return false;
        }

        if (!AgentConditionStateMachine.IsLegalTransition(condition.Status, AgentConditionStatus.Rejected))
        {
            _logger.LogInformation(
                "Condition {ConditionId} is {Status} and can no longer be rejected; the rejection is recorded on the notification only.",
                conditionId,
                condition.Status);

            return false;
        }

        return await TryTransitionAsync(
            conditionId,
            condition.Status,
            AgentConditionStatus.Rejected,
            rejectedByUserId,
            fields: new AgentConditionTransitionFields(
                RejectReason: rejectReason,
                RejectedByUserId: rejectedByUserId),
            cancellationToken: cancellationToken);
    }

    private static AgentCondition NewCondition(
        string triggerKind,
        string fingerprint,
        Guid? entityId,
        Guid? groupId,
        string severity,
        string payloadJson,
        DateTime nowUtc) => new()
        {
            Id = Guid.NewGuid(),
            TriggerKind = triggerKind,
            Fingerprint = fingerprint,
            EntityId = entityId,
            GroupId = groupId,
            Severity = severity,
            Status = AgentConditionStatus.Detected,
            DetectedAtUtc = nowUtc,
            LastSeenAtUtc = nowUtc,
            HandlingKind = AgentConditionHandlingKind.None,
            PayloadJson = string.IsNullOrWhiteSpace(payloadJson) ? EmptyPayloadJson : payloadJson
        };

    private static AgentConditionEvent DetectionEvent(Guid conditionId, DateTime nowUtc) => new()
    {
        Id = Guid.NewGuid(),
        ConditionId = conditionId,
        EventType = AgentConditionStatus.Detected.ToString(),
        AtUtc = nowUtc
    };

    private static AgentConditionTransitionFields WithStatusTimestamps(
        AgentConditionTransitionFields? fields,
        AgentConditionStatus toStatus,
        DateTime nowUtc)
    {
        var merged = fields ?? new AgentConditionTransitionFields();

        return toStatus switch
        {
            AgentConditionStatus.Resolved => merged with { ResolvedAtUtc = merged.ResolvedAtUtc ?? nowUtc },
            AgentConditionStatus.Escalated => merged with { EscalatedAtUtc = merged.EscalatedAtUtc ?? nowUtc },
            AgentConditionStatus.Executed or AgentConditionStatus.Rejected =>
                merged with { HandledAtUtc = merged.HandledAtUtc ?? nowUtc },
            _ => merged
        };
    }

    private async Task<AgentCondition> TouchAsync(
        AgentCondition condition,
        string triggerKind,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(condition.TriggerKind, triggerKind, StringComparison.Ordinal))
        {
            _logger.LogWarning(
                "Fingerprint {Fingerprint} is already held by an open condition of kind {OwningKind}; the detector reporting it as {ReportingKind} cannot open a row of its own for it.",
                condition.Fingerprint,
                condition.TriggerKind,
                triggerKind);
        }

        if (await _repository.TouchLastSeenAsync(condition.Id, nowUtc, cancellationToken))
        {
            condition.LastSeenAtUtc = nowUtc;
        }

        return condition;
    }
}
