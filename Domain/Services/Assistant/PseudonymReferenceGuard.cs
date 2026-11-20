// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Rejects a tool call before it runs when one of its arguments still carries
/// <see cref="PseudonymizationConstants.UnknownReferenceMarker"/>: the pseudonymising provider puts it in place of a
/// pseudonym code that does not exist, so an invented or altered code never reaches a skill, least of all a write.
/// The model gets <see cref="PseudonymizationConstants.UnknownReferenceRejectedResult"/> back. Without pseudonymisation
/// the marker never occurs (it starts with a NUL character), so the guard is a no-op.
/// </summary>

using System.Text.Json;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Logging;
using Klacks.Api.Domain.Services.Assistant.Providers;

namespace Klacks.Api.Domain.Services.Assistant;

internal static class PseudonymReferenceGuard
{
    internal static List<LLMFunctionCall> Apply(List<LLMFunctionCall> functionCalls, ILogger logger)
    {
        var executable = new List<LLMFunctionCall>(functionCalls.Count);
        foreach (var call in functionCalls)
        {
            if (!call.Parameters.Values.Any(ContainsMarker))
            {
                executable.Add(call);
                continue;
            }

            logger.LogWarning("Model referred to an unknown pseudonym in a call of {FunctionName}; the call did not run",
                call.FunctionName.ForLog());
            call.Success = false;
            call.IsRejectedUnknownPseudonym = true;
            call.Result = PseudonymizationConstants.UnknownReferenceRejectedResult;
        }

        return executable;
    }

    private static bool ContainsMarker(object? value) => value switch
    {
        string text => text.Contains(PseudonymizationConstants.UnknownReferenceMarker, StringComparison.Ordinal),
        JsonElement element => ContainsMarkerInElement(element),
        Dictionary<string, object> nested => nested.Values.Any(ContainsMarker),
        IEnumerable<object> list => list.Any(ContainsMarker),
        _ => false
    };

    private static bool ContainsMarkerInElement(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.String => ContainsMarker(element.GetString()),
        JsonValueKind.Object => element.EnumerateObject().Any(property => ContainsMarkerInElement(property.Value)),
        JsonValueKind.Array => element.EnumerateArray().Any(ContainsMarkerInElement),
        _ => false
    };
}
