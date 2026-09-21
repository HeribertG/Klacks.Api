// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Persistence for the standing approvals (agent_standing_approval): an administrator's advance, budgeted and
/// time-limited permission for Klacksy to remediate one trigger kind in one scope without asking per
/// finding.
/// </summary>
/// <remarks>
/// This is the STAGE-ONLY repository convention: the writes here have exactly one caller family, the
/// admin-only grant and revoke handlers inside an HTTP request, so the handler commits through
/// IUnitOfWork.CompleteAsync() and one request stays one transaction. It is deliberately NOT
/// self-committing like IAgentConditionRepository - nothing outside the request cycle ever writes a
/// grant, and a repository that flushed by itself would also flush whatever the request had staged
/// before it.
/// </remarks>

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IStandingApprovalRepository
{
    /// <summary>
    /// The grant that applies to this kind in this scope at <paramref name="nowUtc"/>, or null when the
    /// finding has to go through the approval chain as before.
    ///
    /// The scope is matched by EXACT equality on <paramref name="groupId"/>, including null against null.
    /// There is deliberately NO fallback from a group to the installation-wide row, although
    /// IAgentTriggerGovernanceRepository.FindAsync has one: a governance row only says how far Klacksy
    /// may go, while this row REPLACES the human approval, so the same fallback would silently turn one
    /// null-group grant into unattended autonomy for every group of the installation. Null here means the
    /// findings that carry no group at all - the same bucket
    /// IAgentConditionRepository.CountActionClaimsAsync(groupId: null) counts.
    ///
    /// Newest grant first when several apply. Two active grants for one scope are possible only through a
    /// race between two administrators granting at the same instant (the duplicate check is in the
    /// handler, not a database constraint, because an EXPIRED row must not block a fresh grant); taking
    /// the newest makes the outcome deterministic instead of index-order-dependent.
    /// </summary>
    Task<StandingApproval?> FindActiveAsync(
        string triggerKind,
        Guid? groupId,
        DateTime nowUtc,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Every stored grant, active or not, newest first - the admin card shows the running autonomy
    /// windows AND the ones that were open recently, which is the only place a revoked grant is ever
    /// read. Whether a row still applies is decided by the caller with StandingApprovalPolicy, so the
    /// list cannot disagree with the dispatcher about it.
    /// </summary>
    Task<IReadOnlyList<StandingApproval>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<StandingApproval?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Stages a new grant; the caller commits it.</summary>
    Task AddAsync(StandingApproval approval, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stages the withdrawal of a grant that is not revoked yet and returns whether there was one to
    /// withdraw. False for an unknown id and for an already revoked row - revoking twice is not an error,
    /// it just changes nothing, and the first revocation's user and time are the ones that stay recorded.
    /// An EXPIRED grant can still be revoked: the row is history either way, and refusing it would make
    /// the admin card answer differently for two rows that both no longer apply.
    /// </summary>
    Task<bool> TryRevokeAsync(
        Guid id,
        Guid revokedByUserId,
        DateTime revokedAtUtc,
        CancellationToken cancellationToken = default);
}
