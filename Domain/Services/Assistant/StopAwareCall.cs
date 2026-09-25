// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Runs one non-streaming model call of a streamed turn so that it ends the moment the turn's owner asks
/// for a stop. The call gets a token linked to the request token and the turn's stop token; the link is
/// disposed with the call, otherwise the registration on the request token would outlive it. A stop that
/// cancels the call is reported as null instead of an exception, so the turn ends at its next safe point
/// rather than through the error path; a dropped connection still propagates.
/// </summary>

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Services.Assistant;

internal static class StopAwareCall
{
    /// <param name="turn">The turn whose stop token cancels the call</param>
    /// <param name="requestToken">The HTTP request token; its cancellation propagates as an exception</param>
    /// <param name="call">The call to run with the linked token</param>
    /// <returns>The call's result, or null when a stop request cancelled it</returns>
    internal static async Task<T?> RunAsync<T>(
        TurnRunState turn,
        CancellationToken requestToken,
        Func<CancellationToken, Task<T>> call)
        where T : class
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(requestToken, turn.StopToken);
        try
        {
            return await call(linked.Token);
        }
        catch (OperationCanceledException) when (turn.StopRequested && !requestToken.IsCancellationRequested)
        {
            return null;
        }
    }
}
