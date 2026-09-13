// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Validates skill invocation argument values against the parameter types declared in the skill
/// definition, so a value that is not parsable as its declared type fails loudly at dispatch time
/// (with parameter name, expected type and received value) instead of silently degrading to a CLR
/// default inside the skill. Values that arrive as a parsable string representation (e.g. "5" for
/// an Integer) pass; null, blank and undeclared parameters are left to the skill itself.
/// Numeric checks accept invariant OR current culture, so they gate "plausibly numeric" values
/// rather than guaranteeing the exact value the skill's culture-specific conversion will produce.
/// The date gate is given the caller's language and must use exactly the language
/// <c>SkillDateParser</c> is given afterwards: a relative day word is only a date in the user's own
/// language or in English, so a French "hier" sent by a German user is blocked here rather than
/// silently becoming yesterday inside the skill.
/// </summary>
/// <param name="descriptor">Skill whose declared parameters define the expected types.</param>
/// <param name="parameters">Raw invocation arguments (JsonElement or CLR values), keyed by name.</param>
/// <param name="language">UI language of the calling user; null widens relative day words to every
/// language, which is what a caller without a user (batch, scheduler) needs.</param>

using System.Globalization;
using System.Text.Json;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Services.Assistant.Skills;

public static class SkillParameterTypeValidator
{
    public static IReadOnlyList<string> Validate(
        SkillDescriptor descriptor,
        Dictionary<string, object> parameters,
        string? language = null)
    {
        var errors = new List<string>();

        foreach (var declared in descriptor.Parameters)
        {
            if (!parameters.TryGetValue(declared.Name, out var rawValue))
            {
                continue;
            }

            var value = SkillParameterValueUnwrapper.Unwrap(rawValue);
            if (value is null || (value is string text && string.IsNullOrWhiteSpace(text)))
            {
                continue;
            }

            var error = ValidateValue(declared, value, language);
            if (error is not null)
            {
                errors.Add(error);
            }
        }

        return errors;
    }

    private static string? ValidateValue(SkillParameter declared, object value, string? language)
    {
        return declared.Type switch
        {
            SkillParameterType.Integer => IsInteger(value)
                ? null
                : TypeError(declared.Name, value, "an integer (e.g. 5)"),
            SkillParameterType.Decimal => IsDecimal(value)
                ? null
                : TypeError(declared.Name, value, "a number (e.g. 7.5)"),
            SkillParameterType.Boolean => IsBoolean(value)
                ? null
                : TypeError(declared.Name, value, "a boolean (true or false)"),
            SkillParameterType.Date => IsDate(value, language)
                ? null
                : TypeError(declared.Name, value, "a date (e.g. 2026-07-08)"),
            SkillParameterType.Time => IsTime(value)
                ? null
                : TypeError(declared.Name, value, "a time (e.g. 08:30)"),
            SkillParameterType.DateTime => IsDateTime(value, language)
                ? null
                : TypeError(declared.Name, value, "a date-time (e.g. 2026-07-08T08:30)"),
            SkillParameterType.Enum => IsEnumValue(declared, value)
                ? null
                : TypeError(
                    declared.Name,
                    value,
                    declared.EnumValues is { Count: > 0 }
                        ? $"one of: {string.Join(", ", declared.EnumValues)}"
                        : "a valid enum value"),
            _ => null
        };
    }

    private static bool IsEnumValue(SkillParameter declared, object value)
    {
        if (declared.EnumValues is null || declared.EnumValues.Count == 0)
        {
            return true;
        }

        var text = value switch
        {
            string s => s,
            JsonElement { ValueKind: JsonValueKind.String } je => je.GetString(),
            _ => Convert.ToString(value, CultureInfo.InvariantCulture)
        };

        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        return declared.EnumValues.Any(allowed =>
            string.Equals(allowed, text.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    private static string TypeError(string parameterName, object value, string expected) =>
        $"Parameter '{parameterName}' must be {expected} but received '{value}'. " +
        "Call the skill again with a valid value.";

    private static bool IsInteger(object value)
    {
        return value switch
        {
            sbyte or byte or short or ushort or int or uint or long or ulong => true,
            float f => float.IsFinite(f) && f == MathF.Truncate(f),
            double d => double.IsFinite(d) && d == Math.Truncate(d),
            decimal m => m == decimal.Truncate(m),
            string s => long.TryParse(s.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out _),
            _ => false
        };
    }

    private static bool IsDecimal(object value)
    {
        return value switch
        {
            sbyte or byte or short or ushort or int or uint or long or ulong
                or float or double or decimal => true,
            string s => decimal.TryParse(s.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out _)
                        || decimal.TryParse(s.Trim(), NumberStyles.Number, CultureInfo.CurrentCulture, out _),
            _ => false
        };
    }

    private static bool IsBoolean(object value)
    {
        return value switch
        {
            bool => true,
            sbyte or byte or short or ushort or int or uint or long or ulong => true,
            string s => bool.TryParse(s.Trim(), out _),
            _ => false
        };
    }

    private static bool IsDate(object value, string? language)
    {
        return value switch
        {
            DateOnly or DateTime or DateTimeOffset => true,
            string s => SkillRelativeDayWords.IsRelativeDayWord(s, language) || ParsesAsDate(s.Trim()),
            _ => false
        };
    }

    private static bool IsTime(object value)
    {
        return value switch
        {
            TimeOnly or TimeSpan => true,
            string s => ParsesAsTime(s.Trim()),
            _ => false
        };
    }

    private static bool IsDateTime(object value, string? language)
    {
        return value switch
        {
            DateTime or DateTimeOffset or DateOnly => true,
            string s => SkillRelativeDayWords.IsRelativeDayWord(s, language) || ParsesAsDate(s.Trim()),
            _ => false
        };
    }

    private static bool ParsesAsTime(string text)
    {
        foreach (var parseCulture in SkillDateCultureResolver.AllSupportedCultures)
        {
            if (TimeOnly.TryParse(text, parseCulture, out _))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ParsesAsDate(string text)
    {
        foreach (var culture in SkillDateCultureResolver.AllSupportedCultures)
        {
            if (DateTime.TryParse(text, culture, DateTimeStyles.None, out _))
            {
                return true;
            }
        }

        return false;
    }
}
