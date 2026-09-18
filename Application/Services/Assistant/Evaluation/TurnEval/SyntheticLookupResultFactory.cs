// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Builds the lookup result a replay feeds back instead of executing a read-only skill: exactly one
/// hit, named after what the model itself searched for, with a fixed id. The eval database does not
/// contain the people and shifts the goldset talks about, so a real lookup would come back empty or
/// ambiguous and the second step would measure fixture data instead of the model's next choice.
/// </summary>
/// <param name="lookupParameters">Arguments of the model's first-step lookup call</param>

using System.Text.Json;
using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Application.Services.Assistant.Evaluation.TurnEval;

public static class SyntheticLookupResultFactory
{
    public static string Build(IReadOnlyDictionary<string, object> lookupParameters)
    {
        var name = lookupParameters.Values
            .Select(AsNonEmptyString)
            .FirstOrDefault(value => value != null)
            ?? TurnEvalDefaults.SyntheticLookupFallbackName;

        return JsonSerializer.Serialize(new
        {
            success = true,
            totalCount = 1,
            items = new[] { new { id = TurnEvalDefaults.SyntheticLookupEntityId, name } }
        });
    }

    private static string? AsNonEmptyString(object? value)
    {
        var text = value switch
        {
            string s => s,
            JsonElement { ValueKind: JsonValueKind.String } element => element.GetString(),
            _ => null
        };

        return string.IsNullOrWhiteSpace(text) ? null : text;
    }
}
