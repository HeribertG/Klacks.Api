// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// See IInterruptedTurnFinalizer. Works from the turn's run state and nothing else about the turn: the caller
/// hands over only the user and turn ids, because a connection can drop before the turn has a context at all
/// (between the correction preparation and the start of the chat service). A turn that has an outcome is
/// done. One that has none but never got a context is only cleaned up - its correction-undo token and any
/// UiAction row must not outlive it. Every other one is persisted by the same method the stop tail uses, so
/// both end in the same state, exactly once. A turn that ended on an error - reported by the caller or claimed
/// by the chat service - is claimed as Errored and never labelled "interrupted by the user"; when the server
/// had already run a write action in it, it is persisted under a neutral error marker (F21), otherwise it is
/// left alone. The recorder guarantees that this happens once, so the chat service and this class cannot both
/// write it.
/// </summary>
/// <param name="turnState">The turn's run state, whose outcome tells whether the turn ended on its own</param>
/// <param name="recorder">Persists an interrupted turn from the run state</param>
/// <param name="cleanup">Drops the confirmations and closes the UiAction rows of a turn that never began</param>
/// <param name="logger">Logs how the turn was found</param>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Services.Assistant;

public class InterruptedTurnFinalizer : IInterruptedTurnFinalizer
{
    private readonly TurnRunState _turnState;
    private readonly TurnCompletionRecorder _recorder;
    private readonly IStoppedTurnCleanup _cleanup;
    private readonly ILogger<InterruptedTurnFinalizer> _logger;

    public InterruptedTurnFinalizer(
        TurnRunState turnState,
        TurnCompletionRecorder recorder,
        IStoppedTurnCleanup cleanup,
        ILogger<InterruptedTurnFinalizer> logger)
    {
        _turnState = turnState;
        _recorder = recorder;
        _cleanup = cleanup;
        _logger = logger;
    }

    public async Task<StoppedTurnSummary?> FinalizeAsync(string userId, Guid turnId, bool endedInError)
    {
        try
        {
            if (endedInError)
            {
                _turnState.TrySetOutcome(TurnOutcome.Errored);
            }

            if (_turnState.Outcome == TurnOutcome.Errored)
            {
                await _recorder.RecordErroredAsync(CancellationToken.None);
                return null;
            }

            if (_turnState.Outcome != null)
            {
                return null;
            }

            if (_turnState.Context == null)
            {
                if (_turnState.TrySetOutcome(TurnOutcome.Stopped))
                {
                    _logger.LogInformation(
                        "Turn {TurnId} of user {UserId} ended before its context was prepared; only its confirmations are dropped",
                        turnId, userId);
                    await _cleanup.CleanUpAsync(userId, turnId, CancellationToken.None);
                    return StoppedTurnSummary.Nothing;
                }

                return null;
            }

            _logger.LogInformation(
                "Turn {TurnId} of user {UserId} was left mid-way ({Reason}) and is persisted as interrupted",
                turnId, userId, _turnState.StopRequested ? "stop requested" : "connection lost");
            return await _recorder.TryRecordStoppedAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Finalizing the interrupted turn {TurnId} of user {UserId} failed", turnId, userId);
            return null;
        }
    }
}
