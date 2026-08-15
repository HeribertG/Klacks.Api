// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Produces a copy of a skill invocation argument set in which every secret-carrying value is
/// replaced by a placeholder, so a password or API key typed into the chat never reaches the usage
/// log. Tool-call arguments arrive as JsonElement and may carry objects and arrays, so the walk
/// recurses instead of only checking the top level. The caller's dictionary is never modified.
/// </summary>
/// <param name="parameters">Raw skill invocation arguments, or null when a skill takes none.</param>

using System.Text.Json;
using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Domain.Services.Assistant.Skills;

public static class SkillParameterRedactor
{
    public static Dictionary<string, object?> Redact(IReadOnlyDictionary<string, object>? parameters)
    {
        if (parameters == null)
        {
            return new Dictionary<string, object?>();
        }

        var redacted = new Dictionary<string, object?>(parameters.Count);

        foreach (var parameter in parameters)
        {
            redacted[parameter.Key] = IsSensitiveName(parameter.Key)
                ? SensitiveSkillParameters.RedactedValue
                : RedactValue(parameter.Value);
        }

        return redacted;
    }

    public static bool IsSensitiveName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name) || SensitiveSkillParameters.NonSecretNames.Contains(name))
        {
            return false;
        }

        return SensitiveSkillParameters.NameFragments
            .Any(fragment => name.Contains(fragment, StringComparison.OrdinalIgnoreCase));
    }

    private static object? RedactValue(object? value)
    {
        return value switch
        {
            JsonElement element => RedactElement(element),
            IReadOnlyDictionary<string, object> nested => Redact(nested),
            IEnumerable<object> items => items.Select(RedactValue).ToList(),
            _ => value
        };
    }

    private static object? RedactElement(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                var members = new Dictionary<string, object?>();
                foreach (var member in element.EnumerateObject())
                {
                    members[member.Name] = IsSensitiveName(member.Name)
                        ? SensitiveSkillParameters.RedactedValue
                        : RedactElement(member.Value);
                }

                return members;

            case JsonValueKind.Array:
                return element.EnumerateArray().Select(RedactElement).ToList();

            default:
                return element;
        }
    }
}
