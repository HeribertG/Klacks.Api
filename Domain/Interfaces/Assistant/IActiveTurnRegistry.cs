// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Interfaces.Assistant;

/// <summary>
/// In-memory registry of the chat turns currently streaming, keyed by turn id. Lets the cancel endpoint hand
/// a cooperative stop request to a turn whose SSE request is still running on another HTTP request. The
/// stop token is NOT the HTTP request token: the turn checks it at its safe points and finishes cleanly,
/// while a client disconnect keeps arriving through the request token.
/// An entry belongs to the user who registered it, and only that user can stop it - not even an admin.
/// NOTE: process-local. Klacks runs as a single application instance; on a scaled-out deployment a cancel
/// request may miss the instance running the turn, which then ends through the interrupted-turn safety net.
/// </summary>
public interface IActiveTurnRegistry
{
    /// <summary>
    /// Registers a turn and returns the token that is cancelled when its owner requests a stop.
    /// </summary>
    /// <param name="turnId">Id of the turn that is about to stream; must be non-empty and not registered yet.</param>
    /// <param name="userId">The user who owns the turn; must be non-empty.</param>
    /// <exception cref="ArgumentException">The turn id is empty or the user id is empty or blank.</exception>
    /// <exception cref="InvalidOperationException">The turn id is already registered.</exception>
    CancellationToken Register(Guid turnId, string userId);

    /// <summary>
    /// Requests a cooperative stop of a running turn. Repeating the request is harmless and answers Accepted again.
    /// </summary>
    /// <param name="turnId">Id of the turn to stop.</param>
    /// <param name="userId">The user asking; must be the owner recorded at registration.</param>
    /// <returns>Accepted when the turn is running and belongs to the user, otherwise NotFound.</returns>
    StopRequestOutcome RequestStop(Guid turnId, string userId);

    /// <summary>
    /// Whether a stop has been requested for the turn. False for an unknown or finished turn.
    /// </summary>
    /// <param name="turnId">Id of the turn to look up.</param>
    bool IsStopRequested(Guid turnId);

    /// <summary>
    /// Removes the turn once it has ended. Safe to call for an unknown or already removed turn.
    /// </summary>
    /// <param name="turnId">Id of the turn that has ended.</param>
    void Complete(Guid turnId);
}
