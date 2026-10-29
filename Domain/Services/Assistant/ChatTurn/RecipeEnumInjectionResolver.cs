// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Decides what a forced recipe step injects into an enum parameter. An ask slot holds the user's raw answer
/// ("Mit dem Auto."), which the enum validation rejects, and an injection overrides the model's own value on every
/// retry, so a natural answer could never finish the step. A slot value that names an allowed value (case and
/// surrounding whitespace ignored) is injected in the declared spelling; an invalid one gives way to the value the
/// model sent, which it mapped from the user's words. Without a model value the raw answer is still injected, so
/// the validation fails loudly with the user's words instead of the skill silently falling back to its default.
/// Parameters without a declared enum keep the plain injection.
/// </summary>
/// <param name="function">The forced skill's function definition from the turn's toolset, or null when it is not in the toolset</param>
/// <param name="parameterName">The injected parameter</param>
/// <param name="injectedValue">The value resolved from the recipe slot or literal</param>
/// <param name="modelValue">The value the model sent for the parameter, or null when it sent none</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills;

namespace Klacks.Api.Domain.Services.Assistant.ChatTurn;

internal static class RecipeEnumInjectionResolver
{
    /// <summary>
    /// Returns the value to write into the call, or null when the model's own value has to stay.
    /// </summary>
    internal static object? Resolve(LLMFunction? function, string parameterName, object injectedValue, object? modelValue)
    {
        var allowed = DeclaredEnumValues(function, parameterName);
        if (allowed == null || injectedValue is not string injectedText)
        {
            return injectedValue;
        }

        var canonical = allowed.FirstOrDefault(value =>
            string.Equals(value, injectedText.Trim(), StringComparison.OrdinalIgnoreCase));
        if (canonical != null)
        {
            return canonical;
        }

        return HasValue(modelValue) ? null : injectedValue;
    }

    private static IReadOnlyList<string>? DeclaredEnumValues(LLMFunction? function, string parameterName)
    {
        if (function == null
            || !function.Parameters.TryGetValue(parameterName, out var schema)
            || schema is not IDictionary<string, object> schemaFields
            || !schemaFields.TryGetValue(SkillParameterSchemaKeys.Enum, out var enumValues)
            || enumValues is not IEnumerable<string> values)
        {
            return null;
        }

        var list = values.ToList();
        return list.Count == 0 ? null : list;
    }

    private static bool HasValue(object? modelValue)
    {
        var value = SkillParameterValueUnwrapper.Unwrap(modelValue);
        return value is string text ? !string.IsNullOrWhiteSpace(text) : value != null;
    }
}
