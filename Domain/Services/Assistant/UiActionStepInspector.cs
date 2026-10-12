// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Looks into the JSON handler config of a UiAction skill for a step of the forbidden apiCall type, at any nesting
/// depth (then/else branches included). A config that is not valid JSON is judged by a case-insensitive text search for
/// the action name, so malformed input fails closed instead of slipping past the parser.
/// </summary>
/// <param name="handlerConfigJson">The stored or submitted handler config, or the bare steps array of a skill definition</param>

using System.Text.Json;
using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Domain.Services.Assistant;

public static class UiActionStepInspector
{
    public static bool ContainsApiCallStep(string? handlerConfigJson)
    {
        if (string.IsNullOrWhiteSpace(handlerConfigJson))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(handlerConfigJson);
            return ContainsApiCallStep(document.RootElement);
        }
        catch (JsonException)
        {
            return handlerConfigJson.Contains(UiActionStepActions.ApiCall, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static bool ContainsApiCallStep(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    if (IsApiCallAction(property) || ContainsApiCallStep(property.Value))
                    {
                        return true;
                    }
                }

                return false;
            case JsonValueKind.Array:
                return element.EnumerateArray().Any(ContainsApiCallStep);
            default:
                return false;
        }
    }

    private static bool IsApiCallAction(JsonProperty property)
    {
        return property.NameEquals(UiActionStepActions.ActionProperty)
            && property.Value.ValueKind == JsonValueKind.String
            && string.Equals(property.Value.GetString(), UiActionStepActions.ApiCall, StringComparison.OrdinalIgnoreCase);
    }
}
