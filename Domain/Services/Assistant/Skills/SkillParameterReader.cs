// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Reads one skill invocation argument and converts it to the type the skill declared. Tool-call
/// arguments arrive as JsonElement, which does not implement IConvertible, so the value is unwrapped
/// to a plain CLR value first - without that step Convert.ToInt32/ToBoolean throw and every numeric
/// and boolean parameter silently falls back to its default. Both skill base classes delegate here
/// so the conversion cannot drift apart between them again.
/// </summary>

using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Domain.Services.Assistant.Skills;

public static class SkillParameterReader
{
    /// <param name="parameters">Raw invocation arguments; values are JsonElement or already CLR values</param>
    /// <param name="name">Argument name as declared by the skill</param>
    /// <param name="defaultValue">Returned when the argument is absent, null, or not convertible</param>
    /// <param name="language">UI language of the calling user, used to resolve ambiguous date formats;
    /// null falls back to the reserved SkillParameterKeys.UserLanguage entry the executor adds, and
    /// then to the default culture list</param>
    public static T? Read<T>(
        Dictionary<string, object> parameters,
        string name,
        T? defaultValue = default,
        string? language = null)
    {
        if (!parameters.TryGetValue(name, out var value))
        {
            return defaultValue;
        }

        var unwrapped = SkillParameterValueUnwrapper.Unwrap(value);
        if (unwrapped is null)
        {
            return defaultValue;
        }

        var effectiveLanguage = language ?? ReadUserLanguage(parameters);

        if (unwrapped is T typedValue)
        {
            return typedValue;
        }

        try
        {
            if (typeof(T) == typeof(string))
            {
                return (T)(object)unwrapped.ToString()!;
            }

            if (typeof(T) == typeof(int) || typeof(T) == typeof(int?))
            {
                return (T)(object)Convert.ToInt32(unwrapped);
            }

            if (typeof(T) == typeof(bool) || typeof(T) == typeof(bool?))
            {
                return (T)(object)Convert.ToBoolean(unwrapped);
            }

            if (typeof(T) == typeof(Guid) || typeof(T) == typeof(Guid?))
            {
                return (T)(object)Guid.Parse(unwrapped.ToString()!);
            }

            if (typeof(T) == typeof(DateOnly) || typeof(T) == typeof(DateOnly?))
            {
                if (!SkillCalendarStringParser.TryParseDateOnly(unwrapped.ToString(), effectiveLanguage, out var dateValue))
                {
                    return defaultValue;
                }

                return (T)(object)dateValue;
            }

            if (typeof(T) == typeof(TimeOnly) || typeof(T) == typeof(TimeOnly?))
            {
                if (!SkillCalendarStringParser.TryParseTimeOnly(unwrapped.ToString(), effectiveLanguage, out var timeValue))
                {
                    return defaultValue;
                }

                return (T)(object)timeValue;
            }

            if (typeof(T) == typeof(DateTime) || typeof(T) == typeof(DateTime?))
            {
                if (!SkillCalendarStringParser.TryParseDateTime(unwrapped.ToString(), effectiveLanguage, out var dateTimeValue))
                {
                    return defaultValue;
                }

                return (T)(object)dateTimeValue;
            }

            if (typeof(T) == typeof(decimal) || typeof(T) == typeof(decimal?))
            {
                return (T)(object)Convert.ToDecimal(unwrapped);
            }

            if (typeof(T) == typeof(double) || typeof(T) == typeof(double?))
            {
                return (T)(object)Convert.ToDouble(unwrapped);
            }

            return (T)Convert.ChangeType(unwrapped, typeof(T));
        }
        catch
        {
            return defaultValue;
        }
    }

    private static string? ReadUserLanguage(Dictionary<string, object> parameters)
    {
        if (parameters.TryGetValue(SkillParameterKeys.UserLanguage, out var raw)
            && SkillParameterValueUnwrapper.Unwrap(raw) is { } reserved)
        {
            return reserved.ToString();
        }

        return null;
    }
}
