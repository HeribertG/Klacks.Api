// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Operator-declared overrides for the request parameters a single model accepts, parsed from the
/// JSON map stored on <see cref="LLMModel.SupportedParameters"/>. A parameter that carries no entry
/// is deliberately left undecided so the built-in default rule applies; only explicit entries win.
/// This lets an operator record a quirk of their own model without a code change, which matters
/// because Klacks cannot know which provider or model a customer runs.
/// </summary>
/// <param name="declarations">Parameter name to "may be sent" flag; empty when nothing is declared</param>

using System.Text.Json;

namespace Klacks.Api.Domain.Models.Assistant;

public sealed class ModelParameterSupport(IReadOnlyDictionary<string, bool> declarations)
{
    private static readonly JsonSerializerOptions ParseOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static ModelParameterSupport Empty { get; } =
        new(new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase));

    /// <summary>
    /// Reads the stored JSON map. Malformed or non-boolean content yields <see cref="Empty"/> rather
    /// than throwing: a bad operator entry must not take the chat down, it must only fail to override.
    /// </summary>
    /// <param name="json">Raw JSON object such as {"temperature": false}; null or blank yields Empty</param>
    public static ModelParameterSupport Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return Empty;
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<Dictionary<string, bool>>(json, ParseOptions);

            return parsed is null or { Count: 0 }
                ? Empty
                : new ModelParameterSupport(new Dictionary<string, bool>(parsed, StringComparer.OrdinalIgnoreCase));
        }
        catch (JsonException)
        {
            return Empty;
        }
    }

    /// <summary>
    /// Checks operator input before it is stored. <see cref="Parse"/> deliberately swallows bad content
    /// at request time, which would leave a mistyped entry silently without effect — the operator must
    /// learn about it while saving instead.
    /// </summary>
    /// <param name="json">Raw JSON object to validate; null or blank is valid and means "no overrides"</param>
    /// <param name="error">Human-readable reason when the result is false</param>
    public static bool TryValidate(string? json, out string? error)
    {
        error = null;

        if (string.IsNullOrWhiteSpace(json))
        {
            return true;
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<Dictionary<string, bool>>(json, ParseOptions);

            if (parsed is null)
            {
                error = "Supported parameters must be a JSON object such as {\"temperature\": false}.";
                return false;
            }

            return true;
        }
        catch (JsonException ex)
        {
            error = $"Supported parameters must be a JSON object mapping parameter names to true or false, " +
                    $"such as {{\"temperature\": false}}. {ex.Message}";
            return false;
        }
    }

    /// <summary>
    /// Returns whether the operator took a decision for this parameter.
    /// </summary>
    /// <param name="parameterName">Parameter name as sent on the wire, e.g. "temperature"</param>
    /// <param name="isSupported">The declared decision; only meaningful when this returns true</param>
    public bool TryGetDeclaration(string parameterName, out bool isSupported) =>
        declarations.TryGetValue(parameterName, out isSupported);
}
