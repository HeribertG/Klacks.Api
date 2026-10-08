// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Reads the optional "analyseToken" skill parameter that selects the scope a skill works in: absent or empty
/// means the main plan, a UUID means that scenario. Unlike a lenient parse, a value that is present but not a UUID -
/// or the empty UUID, which no scenario ever carries - is reported as invalid instead of silently falling back to
/// the main plan, so a skill never reads or writes the wrong scope.
/// </summary>
/// <param name="parameters">The skill invocation parameters</param>
/// <param name="analyseToken">The selected scope; null = main plan</param>
/// <param name="error">Refusal text when the parameter is present but not a UUID</param>

namespace Klacks.Api.Application.Skills;

public static class ScenarioScopeParameter
{
    public const string Name = "analyseToken";

    public static bool TryRead(Dictionary<string, object> parameters, out Guid? analyseToken, out string? error)
    {
        analyseToken = null;
        error = null;

        if (!parameters.TryGetValue(Name, out var raw) || raw == null)
        {
            return true;
        }

        var text = raw.ToString();
        if (string.IsNullOrWhiteSpace(text))
        {
            return true;
        }

        if (!Guid.TryParse(text, out var parsed) || parsed == Guid.Empty)
        {
            error = $"Invalid {Name} '{text}'. Expected the scenario UUID, or omit it for the main plan.";
            return false;
        }

        analyseToken = parsed;
        return true;
    }
}
