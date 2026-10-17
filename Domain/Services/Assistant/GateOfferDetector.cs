// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Decides whether a finished turn ENDED at a gate that carries an explain offer: its last executed skill call
/// either returned a held confirmation (RequiresConfirmation) of a topic's gate skill, or was a successful
/// apply=false preview of a topic's preview skill. Only the last executed call counts - when the model went on to do
/// something else after the gate, the question the user sees is no longer the gate's. Calls the loop rejected
/// without running and calls skipped by a stop never executed, so they are not "the last call" either.
/// The input is the typed call list (LLMResponse/SseChunk.ExecutedCalls), never the wire projection in FunctionCalls,
/// which carries neither RequiresConfirmation nor the stop and rejection flags.
/// </summary>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Providers;

namespace Klacks.Api.Domain.Services.Assistant;

public static class GateOfferDetector
{
    /// <summary>The gate the turn ended at (topic, gate skill, issued token), null when it ended anywhere else.</summary>
    /// <param name="functionCalls">The executed calls of the turn in execution order</param>
    public static GateOffer? Detect(IEnumerable<LLMFunctionCall>? functionCalls)
    {
        var last = functionCalls?.LastOrDefault(call => !call.IsRejectedWithoutRunning && !call.SkippedByStop);
        if (last == null)
        {
            return null;
        }

        if (last.RequiresConfirmation)
        {
            var heldTopic = ExplainOfferTopics.FindByConfirmationGate(last.FunctionName);
            return heldTopic == null ? null : new GateOffer(heldTopic, last.FunctionName, last.IssuedConfirmationToken);
        }

        var previewTopic = last.Success && PreviewApplySkillCalls.IsPreviewCall(last.FunctionName, last.Parameters)
            ? ExplainOfferTopics.FindByPreviewGate(last.FunctionName)
            : null;
        return previewTopic == null ? null : new GateOffer(previewTopic, last.FunctionName, null);
    }
}
