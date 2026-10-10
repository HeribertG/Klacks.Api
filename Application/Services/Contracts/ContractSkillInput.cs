// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Strict reading of optional numeric and boolean skill arguments for the contract-from-template skills.
/// SkillParameterReader returns null for a value it cannot convert, which is indistinguishable from an omitted
/// argument; for an optional override that would silently keep the template value. These readers tell the two
/// apart: an absent, null or blank argument is simply not given, while a present but unreadable one ("20h",
/// "ja", "20,5") is reported as an error naming the parameter. Numbers are read with the invariant culture and
/// only a leading sign and a decimal point are accepted, so a decimal comma or thousands separator is never
/// guessed.
/// </summary>

using System.Globalization;
using Klacks.Api.Domain.Services.Assistant.Skills;

namespace Klacks.Api.Application.Services.Contracts;

public static class ContractSkillInput
{
    private const NumberStyles DecimalStyles = NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint;

    public static bool TryReadDecimal(
        Dictionary<string, object> parameters, string name, out decimal? value, out string? error)
    {
        value = null;
        error = null;

        var unwrapped = Unwrap(parameters, name);
        if (unwrapped is null)
        {
            return true;
        }

        if (unwrapped is string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return true;
            }

            if (decimal.TryParse(text.Trim(), DecimalStyles, CultureInfo.InvariantCulture, out var parsed))
            {
                value = parsed;
                return true;
            }

            error = Unreadable(name, text, "a number such as 120 or 80.5");
            return false;
        }

        try
        {
            value = unwrapped switch
            {
                decimal exact => exact,
                long or int or short or byte => Convert.ToDecimal(unwrapped, CultureInfo.InvariantCulture),
                double or float => Convert.ToDecimal(unwrapped, CultureInfo.InvariantCulture),
                _ => null
            };
        }
        catch (OverflowException)
        {
            value = null;
        }

        if (value is null)
        {
            error = Unreadable(name, unwrapped.ToString() ?? string.Empty, "a number such as 120 or 80.5");
            return false;
        }

        return true;
    }

    public static bool TryReadBool(
        Dictionary<string, object> parameters, string name, out bool? value, out string? error)
    {
        value = null;
        error = null;

        var unwrapped = Unwrap(parameters, name);
        if (unwrapped is null)
        {
            return true;
        }

        if (unwrapped is bool flag)
        {
            value = flag;
            return true;
        }

        if (unwrapped is string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return true;
            }

            if (bool.TryParse(text.Trim(), out var parsed))
            {
                value = parsed;
                return true;
            }

            error = Unreadable(name, text, "true or false");
            return false;
        }

        error = Unreadable(name, unwrapped.ToString() ?? string.Empty, "true or false");
        return false;
    }

    private static object? Unwrap(Dictionary<string, object> parameters, string name) =>
        parameters.TryGetValue(name, out var raw) ? SkillParameterValueUnwrapper.Unwrap(raw) : null;

    private static string Unreadable(string name, string raw, string expected) =>
        $"Parameter '{name}' has the unreadable value '{raw.Trim()}'; expected {expected}. " +
        "Ask the administrator for a plain value instead of guessing.";
}
