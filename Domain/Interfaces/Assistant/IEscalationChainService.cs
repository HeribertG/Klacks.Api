// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IEscalationChainService
{
    /// <summary>Creates the chain, freezes the roster into stages, and delivers the first wave.</summary>
    Task<Guid> StartChainAsync(StartEscalationChainRequest request, CancellationToken cancellationToken = default);

    /// <summary>Called by the sweep after a stage's expiry won; advances to the next wave or exhausts the chain.</summary>
    Task AdvanceAsync(Guid chainId, CancellationToken cancellationToken = default);

    /// <summary>Reply-path entry point: acknowledges the Notified stage this user currently holds, if any. Returns false if the user holds none.</summary>
    Task<bool> AcknowledgeAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>Owner decision B7: admins and any roster member of THIS chain may cancel, with a mandatory reason.</summary>
    Task<bool> CancelAsync(Guid chainId, string userId, string userName, string reason, CancellationToken cancellationToken = default);
}
