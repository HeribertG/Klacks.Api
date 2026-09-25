// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// What a stopped turn tells the user it did, and which of its calls count as run. Two different notions on
/// purpose. The calls that RAN are those the server finished: not skipped by the stop, not rejected as a repeat
/// and not handed to the browser (UI actions and UI passthrough calls only run once the client received the
/// metadata event, which a stopped turn never sends). What the client is TOLD is narrower: only successful,
/// non-repeatable server actions - the writes - because "nothing was executed" means "nothing was changed",
/// and a held confirmation or a plain lookup changed nothing. Labels are resolved in the user's language
/// without any fallback; an action without a label in that language is not named but still counted, so the
/// client can tell "nothing ran" from "something ran that I cannot name".
/// </summary>
/// <param name="Labels">User-facing labels of the write actions that ran, one per skill, in call order</param>
/// <param name="ExecutedCount">Number of distinct write skills that ran, labelled or not</param>

using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Providers;

namespace Klacks.Api.Domain.Services.Assistant;

public sealed record StoppedTurnSummary(IReadOnlyList<string> Labels, int ExecutedCount)
{
    /// <summary>The summary of a turn that ran nothing.</summary>
    public static StoppedTurnSummary Nothing { get; } = new(Array.Empty<string>(), 0);

    /// <summary>
    /// The calls the server ran to their end: skipped, rejected and browser-side calls are left out, held and
    /// failed ones are not, because they were executed and their outcome is on record.
    /// </summary>
    /// <param name="calls">Every call of the turn, in the order the model made them</param>
    public static List<LLMFunctionCall> ExecutedCalls(IEnumerable<LLMFunctionCall> calls) =>
        calls.Where(call => call.Result != null
                && !call.SkippedByStop
                && !call.IsRejectedRepeat
                && call.UiActionSteps == null
                && call.ResultKind != LLMFunctionResultKind.UiPassthrough)
            .ToList();

    /// <summary>
    /// The answer as it is stored for a turn that was cut off: what was streamed, then the marker the model
    /// reads later.
    /// </summary>
    /// <param name="streamedContent">The text the client received before the turn was cut off</param>
    /// <param name="marker">Says why the answer ends there: the user's stop or an error</param>
    public static string StoredAnswer(string streamedContent, string marker = TurnInterruptionDefaults.InterruptedMarker) =>
        string.IsNullOrWhiteSpace(streamedContent)
            ? marker
            : streamedContent + "\n" + marker;

    /// <summary>
    /// Builds the summary of the write actions a stopped turn ran. Its count is also what tells a turn that
    /// changed something from one that did not.
    /// </summary>
    /// <param name="context">The turn context; its language and toolset resolve the labels</param>
    /// <param name="calls">Every call of the turn</param>
    public static StoppedTurnSummary From(LLMContext? context, IEnumerable<LLMFunctionCall> calls)
    {
        var writes = ExecutedCalls(calls)
            .Where(call => call.Success && !call.RequiresConfirmation && !RepeatedWriteCallGuard.IsRepeatable(call))
            .GroupBy(call => call.FunctionName, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();
        if (writes.Count == 0)
        {
            return Nothing;
        }

        var labels = writes
            .Select(call => SkillLabelResolver.Resolve(LabelsOf(context, call.FunctionName), context?.Language))
            .OfType<string>()
            .ToList();
        return new StoppedTurnSummary(labels, writes.Count);
    }

    private static IReadOnlyDictionary<string, string>? LabelsOf(LLMContext? context, string functionName) =>
        context?.AvailableFunctions?
            .FirstOrDefault(function => string.Equals(function.Name, functionName, StringComparison.OrdinalIgnoreCase))?
            .Labels;
}
