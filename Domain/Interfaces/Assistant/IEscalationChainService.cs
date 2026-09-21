// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IEscalationChainService
{
    /// <summary>Creates the chain, freezes the roster into stages, and delivers the first wave. Returns
    /// null without creating anything when the deadline is further away than the roster could reach
    /// even at max stage length - too early to be worth waking anyone yet.</summary>
    Task<Guid?> StartChainAsync(StartEscalationChainRequest request, CancellationToken cancellationToken = default);

    /// <summary>Creates a ProactiveApproval chain from a roster and deadline the caller already resolved,
    /// freezes the roster into stages in the given order and delivers the first wave. No urgency gate: the
    /// caller owns the deadline. Returns null without creating anything when a chain is already Running
    /// for this ConditionId.</summary>
    Task<Guid?> StartConditionApprovalChainAsync(StartConditionApprovalChainRequest request, CancellationToken cancellationToken = default);

    /// <summary>Called by the sweep after a stage's expiry won; advances to the next wave or exhausts the chain.</summary>
    Task AdvanceAsync(Guid chainId, CancellationToken cancellationToken = default);

    /// <summary>Reply-path entry point: acknowledges the Notified stage this user currently holds, if any. Returns false if the user holds none.</summary>
    Task<bool> AcknowledgeAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>Intervention-list entry point: acknowledges this user's Notified stage on THIS specific
    /// chain. Unlike AcknowledgeAsync, safe when the user holds a Notified stage on more than one chain
    /// at once. Returns false if the user holds no Notified stage on this chain.</summary>
    Task<bool> AcknowledgeChainAsync(Guid chainId, string userId, CancellationToken cancellationToken = default);

    /// <summary>Owner decision B7: admins and any roster member of THIS chain may cancel, with a mandatory reason.</summary>
    Task<bool> CancelAsync(Guid chainId, string userId, string userName, string reason, CancellationToken cancellationToken = default);

    /// <summary>Ends the Running ProactiveApproval chain of this condition, if there is one, as Superseded
    /// with the given reason and cancels its remaining stages, so the roster is no longer woken for a
    /// question somebody has already answered elsewhere (a delegation). Returns whether a chain was
    /// superseded by THIS call; false when none is running or another instance ended it first.</summary>
    Task<bool> SupersedeConditionApprovalChainAsync(Guid conditionId, string reason, CancellationToken cancellationToken = default);
}
