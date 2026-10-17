// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Restarts the lifetime of ONE held confirmation: the one an explain offer was made at. Needed when a detour turn (the
/// explanation the user asked for at the gate) sits between the question and the answer: the answer's forced redemption
/// window (AutonomyDefaults.ConfirmationForceWindowSeconds) and the token's own lifetime are both measured from the issue
/// time, so without a renewal the "yes" that follows the explanation would find the token too old. The renewal is bound to
/// the exact token (by fingerprint), its skill, its purpose, its channel and its user, so it can never pull an unrelated
/// older confirmation back into the force window. Kept apart from IPendingConfirmationStore because only this one caller
/// needs it.
/// </summary>

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IPendingConfirmationRenewal
{
    /// <summary>
    /// Gives the still-active confirmation of the user whose token has this fingerprint a fresh lifetime, as if it had
    /// been issued now - but only when its skill, purpose and channel match as well. Returns false and changes nothing
    /// when there is no such row (consumed, expired, another user's, another skill's or purpose's).
    /// </summary>
    /// <param name="userId">The user the confirmation was issued to</param>
    /// <param name="tokenFingerprint">ConfirmationTokenHasher fingerprint of the token the offer was made at</param>
    /// <param name="skillName">The skill the confirmation must hold</param>
    /// <param name="purposes">The PendingConfirmationPurposes the row may carry</param>
    /// <param name="channel">One of PendingConfirmationChannels</param>
    /// <param name="cancellationToken">Cancels the lookup and the write</param>
    Task<bool> RenewOfferedAsync(
        Guid userId,
        string tokenFingerprint,
        string skillName,
        IReadOnlyCollection<string> purposes,
        string channel,
        CancellationToken cancellationToken = default);
}
