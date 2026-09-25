// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Services.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

/// <summary>
/// The safety net of a streamed turn: called once the enumeration of the turn is over, whichever way it ended.
/// A turn that ended on its own (completed, clarified, stopped) is left alone. A turn that was left mid-way -
/// the tab was closed, the network dropped, the client cut the connection hard - is persisted the way a stop
/// is, so its history, usage and correction anchor exist and the confirmations it issued are gone. A turn that
/// ended on an error is persisted the same way, under a neutral error marker, when the server had already run a
/// write action in it, and left alone otherwise. Never throws.
/// It tells the caller whether it was the one that persisted the turn as stopped, so a stop whose cancellation
/// escaped the turn before the stop tail could run can still be reported to the client.
/// </summary>
public interface IInterruptedTurnFinalizer
{
    /// <param name="userId">The user the turn ran for; known to the caller even when the turn never got as far as its own context</param>
    /// <param name="turnId">The turn's id; known to the caller for the same reason</param>
    /// <param name="endedInError">True when the caller ended the turn on an unhandled failure: that turn is never "interrupted by the user"; it is persisted only if a write action had run in it</param>
    /// <returns>What the client is to be told about the write actions that ran when this call itself persisted the turn as stopped; null in every other case, in particular when the turn had ended on its own or was persisted by someone else</returns>
    Task<StoppedTurnSummary?> FinalizeAsync(string userId, Guid turnId, bool endedInError);
}
