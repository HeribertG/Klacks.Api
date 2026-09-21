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
using Klacks.Api.Domain.Services.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Conditions;

public class AgentConditionLedgerService : IAgentConditionLedgerService
{
    private const string EmptyPayloadJson = "{}";
    private const string DelegatedEventType = "Delegated";
    private const string DelegatedEventDetailFormat = "MaxAction={0}";

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
        IReadOnlySet<Guid> groupIds,
        string severity,
        string payloadJson,
        CancellationToken cancellationToken = default)
    {
        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        var existing = await _repository.FindOpenByFingerprintAsync(fingerprint, cancellationToken);
        if (existing != null)
        {
            return (await TouchAsync(existing, triggerKind, groupIds, payloadJson, nowUtc, cancellationToken), false);
        }

        var groupId = AgentConditionLedgerPolicy.PrimaryGroupIdFor(groupIds);
        var condition = NewCondition(triggerKind, fingerprint, entityId, groupId, severity, payloadJson, nowUtc);
        var inserted = await _repository.InsertAsync(
            condition,
            DetectionEvent(condition.Id, nowUtc),
            groupIds,
            cancellationToken);

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

        return (await TouchAsync(winner, triggerKind, groupIds, payloadJson, nowUtc, cancellationToken), false);
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

    public async Task<bool> TryDelegateAsync(
        Guid conditionId,
        ProactiveMaxAction maxAction,
        Guid delegatingUserId,
        CancellationToken cancellationToken = default)
    {
        var delegated = await _repository.SetDelegationAsync(conditionId, maxAction, delegatingUserId, cancellationToken);
        if (!delegated)
        {
            return false;
        }

        await _repository.InsertEventAsync(
            new AgentConditionEvent
            {
                Id = Guid.NewGuid(),
                ConditionId = conditionId,
                EventType = DelegatedEventType,
                AtUtc = _timeProvider.GetUtcNow().UtcDateTime,
                UserId = delegatingUserId,
                Detail = string.Format(DelegatedEventDetailFormat, maxAction)
            },
            cancellationToken);

        return true;
    }

    public async Task<bool> TryApproveAsync(
        Guid conditionId,
        Guid approverUserId,
        CancellationToken cancellationToken = default)
    {
        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        var stamped = await _repository.TryStampApprovalAsync(conditionId, approverUserId, nowUtc, cancellationToken);
        if (!stamped)
        {
            return false;
        }

        await _repository.InsertEventAsync(
            new AgentConditionEvent
            {
                Id = Guid.NewGuid(),
                ConditionId = conditionId,
                EventType = AgentConditionEventTypes.Approved,
                AtUtc = nowUtc,
                UserId = approverUserId
            },
            cancellationToken);

        return true;
    }

    public async Task<bool> TryWithdrawApprovalAsync(
        Guid conditionId,
        Guid approverUserId,
        string detail,
        CancellationToken cancellationToken = default)
    {
        var cleared = await _repository.TryClearApprovalAsync(conditionId, approverUserId, cancellationToken);
        if (!cleared)
        {
            return false;
        }

        await _repository.InsertEventAsync(
            new AgentConditionEvent
            {
                Id = Guid.NewGuid(),
                ConditionId = conditionId,
                EventType = AgentConditionEventTypes.ApprovalWithdrawn,
                AtUtc = _timeProvider.GetUtcNow().UtcDateTime,
                UserId = approverUserId,
                Detail = detail
            },
            cancellationToken);

        return true;
    }

    public async Task<bool> TryReclaimStaleAsync(
        Guid conditionId,
        TimeSpan staleAfter,
        string detail,
        CancellationToken cancellationToken = default)
    {
        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        return await _repository.TryReclaimStaleAsync(
            conditionId,
            nowUtc - staleAfter,
            nowUtc,
            new AgentConditionEvent
            {
                Id = Guid.NewGuid(),
                ConditionId = conditionId,
                EventType = AgentConditionEventTypes.Reclaimed,
                AtUtc = nowUtc,
                Detail = detail
            },
            cancellationToken);
    }

    public async Task RecordEventAsync(
        Guid conditionId,
        string eventType,
        string detail,
        CancellationToken cancellationToken = default)
    {
        await _repository.InsertEventAsync(
            new AgentConditionEvent
            {
                Id = Guid.NewGuid(),
                ConditionId = conditionId,
                EventType = eventType,
                AtUtc = _timeProvider.GetUtcNow().UtcDateTime,
                Detail = detail
            },
            cancellationToken);
    }

    public async Task<bool> TrySetCausedByAsync(
        Guid conditionId,
        Guid causedByConditionId,
        CancellationToken cancellationToken = default) =>
        await _repository.TrySetCausedByAsync(conditionId, causedByConditionId, cancellationToken);

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

    /// <summary>
    /// Re-observation of a row that is already open: LastSeenAtUtc moves forward, PayloadJson is
    /// rewritten when the detector now reports something different from what the row was opened with, and
    /// the group set is brought in step with what the detector now reports.
    ///
    /// The group set is compared before it is touched, for the same reason the payload is: a tick
    /// re-observes every open row - roughly 2900 in the reference installation - and virtually none of
    /// them change groups. The comparison is free because FindOpenByFingerprintAsync loaded the stored set
    /// along with the row. A mismatch only means "worth asking the repository"; the repository re-reads
    /// and diffs against what is actually stored, so a set this service never loaded (a fake, an older
    /// caller) costs one query and still writes nothing when nothing changed.
    ///
    /// AgentCondition.GroupId is deliberately NOT rewritten here even when the smallest member of the set
    /// changed - see that property for why the budget bucket has to stay put.
    ///
    /// The payload is compared before it is written rather than written unconditionally. A tick re-reports
    /// every open row of every kind - roughly 2900 in the reference installation - and almost none of them
    /// have changed, so an unconditional write would spend thousands of UPDATEs and as much WAL per tick to
    /// store the bytes that are already there. The comparison itself is free: FindOpenByFingerprintAsync has
    /// already materialised the stored payload. It is an ordinal string comparison, not a semantic JSON one,
    /// because both sides are produced by the same serializer over the same dictionary shape - equal content
    /// therefore yields equal bytes, and the worst a spurious difference can cost is one redundant UPDATE.
    /// </summary>
    private async Task<AgentCondition> TouchAsync(
        AgentCondition condition,
        string triggerKind,
        IReadOnlySet<Guid> groupIds,
        string payloadJson,
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

        var refreshedPayload = RefreshedPayloadOrNull(condition, payloadJson);

        if (await _repository.TouchLastSeenAsync(condition.Id, nowUtc, refreshedPayload, cancellationToken))
        {
            condition.LastSeenAtUtc = nowUtc > condition.LastSeenAtUtc ? nowUtc : condition.LastSeenAtUtc;

            if (refreshedPayload != null)
            {
                condition.PayloadJson = refreshedPayload;
            }
        }

        await SyncGroupsIfChangedAsync(condition, groupIds, cancellationToken);

        return condition;
    }

    /// <summary>
    /// The in-memory <see cref="AgentCondition.Groups"/> is only updated when the repository confirms it
    /// wrote, and a false from it does NOT mean the stored set equals the requested one - it can also mean
    /// another instance won the same insert (see IAgentConditionRepository.SyncGroupsAsync). The returned
    /// object's group set can therefore be one tick stale after such a race. Deliberately not re-read: the
    /// database is correct either way, no caller of UpsertDetectedAsync reads Groups off the result today
    /// (AgentTriggerBackgroundService and NextPeriodAutoCommitService use Status and Id), and spending a
    /// query per open row per tick to refresh a value nobody reads is exactly the cost this whole diff
    /// exists to avoid.
    /// </summary>
    private async Task SyncGroupsIfChangedAsync(
        AgentCondition condition,
        IReadOnlySet<Guid> groupIds,
        CancellationToken cancellationToken)
    {
        var storedGroupIds = condition.Groups.Select(group => group.GroupId).ToHashSet();
        if (storedGroupIds.SetEquals(groupIds))
        {
            return;
        }

        if (await _repository.SyncGroupsAsync(condition.Id, groupIds, cancellationToken))
        {
            condition.Groups = groupIds
                .Select(groupId => new AgentConditionGroup { ConditionId = condition.Id, GroupId = groupId })
                .ToList();
        }
    }

    /// <summary>
    /// The payload to write, or null when the stored one already says the same thing. An empty payload from
    /// the detector never overwrites a populated one: a detector that reports nothing structured is not
    /// asserting that what the row knows is wrong.
    /// </summary>
    private static string? RefreshedPayloadOrNull(AgentCondition condition, string payloadJson)
    {
        if (string.IsNullOrWhiteSpace(payloadJson) || string.Equals(payloadJson, EmptyPayloadJson, StringComparison.Ordinal))
        {
            return null;
        }

        return string.Equals(condition.PayloadJson, payloadJson, StringComparison.Ordinal) ? null : payloadJson;
    }
}
