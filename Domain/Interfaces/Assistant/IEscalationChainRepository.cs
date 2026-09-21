// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Persistence and the conditional-update surface for EscalationChain/EscalationStage. Every method
/// that changes a status returns whether THIS call won the transition (mirrors
/// ScheduledTaskRepository.TryClaimAsync): a WHERE clause on the expected prior status turns a
/// concurrent sweep tick or a race between the sweep and an incoming reply into a single winner
/// instead of a double delivery or a double acknowledgement.
/// </summary>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant.Escalation;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IEscalationChainRepository
{
    /// <summary>Returns false without throwing when one of the partial unique indexes (WorkId or
    /// ConditionId, each where Status=Running) already holds a chain for this key - a second CoverAbsence
    /// on the same shift, or a second approval request for a condition still awaiting its answer.</summary>
    Task<bool> AddAsync(EscalationChain chain, CancellationToken cancellationToken = default);

    Task<EscalationChain?> GetByIdWithStagesAsync(Guid chainId, CancellationToken cancellationToken = default);

    /// <summary>This chain's status alone, or null when there is no such chain - what a caller needs who
    /// only has to report where the chain ended up, without paying for its stages.</summary>
    Task<EscalationChainStatus?> GetStatusAsync(Guid chainId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EscalationStage>> GetStagesByChainAsync(Guid chainId, CancellationToken cancellationToken = default);

    /// <summary>Stages currently Notified whose DueAtUtc has passed, across every Running chain.</summary>
    Task<IReadOnlyList<EscalationStage>> GetDueStagesAsync(DateTime nowUtc, CancellationToken cancellationToken = default);

    /// <summary>Running chains whose DeadlineUtc has passed - the safety net for Exhausted even if stage timing drifted.</summary>
    Task<IReadOnlyList<Guid>> GetOverdueRunningChainIdsAsync(DateTime nowUtc, CancellationToken cancellationToken = default);

    /// <summary>Running chains that carry an AbsenceBreakId, for the F3 Superseded sweep.</summary>
    Task<IReadOnlyList<EscalationChain>> GetRunningChainsWithAbsenceBreakAsync(CancellationToken cancellationToken = default);

    /// <summary>Every Running chain with its stages, for the admin intervention list.</summary>
    Task<IReadOnlyList<EscalationChain>> GetRunningChainsWithStagesAsync(CancellationToken cancellationToken = default);

    Task<bool> IsBreakDeletedAsync(Guid breakId, CancellationToken cancellationToken = default);

    /// <summary>The most recently created chain for this condition whatever its status, or null - what the
    /// tick consults before asking for an approval: Running means the question is already out, and a
    /// chain created on the current company day means it was answered (or not) today already.</summary>
    Task<EscalationChain?> GetLatestChainForConditionAsync(Guid conditionId, CancellationToken cancellationToken = default);

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

    /// <summary>Acknowledges a stage only while it is Notified AND its chain is still Running. The second
    /// half is what keeps a late reply from writing an Acknowledged stage under a chain that has already
    /// been exhausted, cancelled or superseded - a record that used to survive because the caller's next
    /// compare-and-swap on the chain then failed and nothing rolled the stage back.</summary>
    Task<bool> TryAcknowledgeStageAsync(Guid stageId, DateTime respondedAtUtc, CancellationToken cancellationToken = default);

    Task<bool> TryAcknowledgeChainAsync(
        Guid chainId, string userId, string userName, DateTime atUtc, CancellationToken cancellationToken = default);

    /// <summary>Cancels every OTHER stage still Pending or Notified once one stage has been acknowledged.</summary>
    Task<int> CancelRemainingStagesAsync(Guid chainId, Guid exceptStageId, CancellationToken cancellationToken = default);

    /// <summary>Ends the chain as Exhausted and, in the SAME transaction, cancels every stage still Pending
    /// or Notified, so no stage is left in a state a late reply could still win. Unlike the other methods on
    /// this interface it therefore commits a transaction of its own; every caller is a sweep tick or a chain
    /// wave with no surrounding unit of work, and a half-applied exhaust is exactly the state this fixes.
    /// The returned stages are the ones this call moved from Notified to Cancelled - their inbox rows are
    /// still open and the caller is expected to close them.</summary>
    Task<EscalationChainExhaustResult> TryExhaustChainAsync(Guid chainId, string outcomeReason, CancellationToken cancellationToken = default);

    Task<bool> TrySupersedeChainAsync(Guid chainId, string outcomeReason, CancellationToken cancellationToken = default);

    Task<bool> TryCancelChainAsync(
        Guid chainId, string userId, string userName, string reason, DateTime atUtc, CancellationToken cancellationToken = default);
}
