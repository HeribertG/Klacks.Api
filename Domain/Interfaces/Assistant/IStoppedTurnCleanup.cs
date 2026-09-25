// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Interfaces.Assistant;

/// <summary>
/// What a stopped or interrupted turn leaves behind that must not outlive it: the confirmation tokens and
/// proposal hints it issued, and the UiAction tracking rows whose steps the client never received. Shared by
/// the stopped-turn tail and the interrupted-turn safety net so both end in the same state. Best-effort: a
/// failure is logged and never thrown, because it runs on the way out of a turn that has already been
/// persisted.
/// </summary>
public interface IStoppedTurnCleanup
{
    /// <summary>
    /// Drops the pending confirmations the current turn issued and closes its dispatched UiAction rows as
    /// cancelled. What earlier turns left is not touched.
    /// </summary>
    /// <param name="userId">The user the turn ran for; an unparseable id skips the confirmation part</param>
    /// <param name="turnId">The turn whose UiAction rows are closed; an empty id skips that part</param>
    /// <param name="cancellationToken">Cancels the database write</param>
    Task CleanUpAsync(string userId, Guid turnId, CancellationToken cancellationToken);
}
