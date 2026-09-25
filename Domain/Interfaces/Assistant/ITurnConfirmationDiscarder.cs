// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Interfaces.Assistant;

/// <summary>
/// Drops the pending confirmations a stopped or interrupted turn left behind. The turn asked the user
/// nothing - the answer that would have carried the question was never delivered - so a token it issued
/// must not stay redeemable by a later "yes" that was never meant for it. Best-effort: a failure is logged
/// and never thrown, because it runs on the way out of a turn that has already been persisted.
/// </summary>
public interface ITurnConfirmationDiscarder
{
    /// <summary>
    /// Drops every confirmation token and proposal hint the current turn issued, the correction-undo offer
    /// included. What earlier turns left is not touched.
    /// </summary>
    /// <param name="userId">The user the turn ran for</param>
    void DiscardIssuedThisTurn(Guid userId);
}
