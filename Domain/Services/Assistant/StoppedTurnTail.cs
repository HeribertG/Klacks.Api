// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// The ending of a turn the user stopped: the turn is persisted first (history with the partial answer,
/// usage, correction anchor, discarded confirmations, cancelled UiAction rows, the background tasks that stay
/// allowed) and only then does the client hear that it was stopped. The events are turn_stopped and then done,
/// nothing else, because the client keeps processing the stream after a confirmed stop and would otherwise
/// append text or start UI actions to a stopped turn. Should the client be gone by then, the state is complete
/// anyway; the safety net finds the Stopped outcome and does nothing.
/// </summary>

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Services.Assistant;

internal static class StoppedTurnTail
{
    /// <param name="recorder">Persists the stopped turn from its run state and claims the Stopped outcome</param>
    /// <param name="turn">The stopped turn; only its turn id is read here</param>
    internal static async IAsyncEnumerable<SseChunk> StreamAsync(TurnCompletionRecorder recorder, TurnRunState turn)
    {
        var summary = await recorder.RecordStoppedAsync(CancellationToken.None);

        yield return SseChunk.TurnStopped(
            turn.Context!.TurnId.GetValueOrDefault(), summary.Labels.ToList(), summary.ExecutedCount);
        yield return SseChunk.Done();
    }
}
