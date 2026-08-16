// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Persistence and the conditional-update surface for EscalationChain/EscalationStage. Every method
/// that changes a status returns whether THIS call won the transition (mirrors
/// ScheduledTaskRepository.TryClaimAsync): a WHERE clause on the expected prior status turns a
/// concurrent sweep tick or a race between the sweep and an incoming reply into a single winner
/// instead of a double delivery or a double acknowledgement.
/// </summary>

using Klacks.Api.Domain.Models.Assistant.Escalation;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IEscalationChainRepository
{
    Task AddAsync(EscalationChain chain, CancellationToken cancellationToken = default);

    Task<EscalationChain?> GetByIdWithStagesAsync(Guid chainId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EscalationStage>> GetStagesByChainAsync(Guid chainId, CancellationToken cancellationToken = default);

    /// <summary>Stages currently Notified whose DueAtUtc has passed, across every Running chain.</summary>
    Task<IReadOnlyList<EscalationStage>> GetDueStagesAsync(DateTime nowUtc, CancellationToken cancellationToken = default);

    /// <summary>Running chains whose DeadlineUtc has passed - the safety net for Exhausted even if stage timing drifted.</summary>
    Task<IReadOnlyList<Guid>> GetOverdueRunningChainIdsAsync(DateTime nowUtc, CancellationToken cancellationToken = default);

    /// <summary>Running chains that carry an AbsenceBreakId, for the F3 Superseded sweep.</summary>
    Task<IReadOnlyList<EscalationChain>> GetRunningChainsWithAbsenceBreakAsync(CancellationToken cancellationToken = default);

    Task<bool> IsBreakDeletedAsync(Guid breakId, CancellationToken cancellationToken = default);

    /// <summary>The stage this user is currently Notified on, if any - the reply path's lookup, chain id not known in advance.</summary>
    Task<EscalationStage?> FindNotifiedStageForUserAsync(string userId, CancellationToken cancellationToken = default);

    Task<bool> TryNotifyStageAsync(
        Guid stageId,
        DateTime notifiedAtUtc,
        DateTime dueAtUtc,
        string? deliveryChannel,
        string? deliveryOutcome,
        Guid? dispatchRowId,
        CancellationToken cancellationToken = default);

    Task<bool> TrySkipStageAsync(Guid stageId, string skipReason, CancellationToken cancellationToken = default);

    Task<bool> TryExpireStageAsync(Guid stageId, CancellationToken cancellationToken = default);

    Task<bool> TryAcknowledgeStageAsync(Guid stageId, DateTime respondedAtUtc, CancellationToken cancellationToken = default);

    Task<bool> TryAcknowledgeChainAsync(
        Guid chainId, string userId, string userName, DateTime atUtc, CancellationToken cancellationToken = default);

    /// <summary>Cancels every OTHER stage still Pending or Notified once one stage has been acknowledged.</summary>
    Task<int> CancelRemainingStagesAsync(Guid chainId, Guid exceptStageId, CancellationToken cancellationToken = default);

    Task<bool> TryExhaustChainAsync(Guid chainId, string outcomeReason, CancellationToken cancellationToken = default);

    Task<bool> TrySupersedeChainAsync(Guid chainId, string outcomeReason, CancellationToken cancellationToken = default);

    Task<bool> TryCancelChainAsync(
        Guid chainId, string userId, string userName, string reason, DateTime atUtc, CancellationToken cancellationToken = default);
}
