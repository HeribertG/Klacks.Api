// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Reads one skill invocation argument and converts it to the type the skill declared. Tool-call
/// arguments arrive as JsonElement, which does not implement IConvertible, so the value is unwrapped
/// to a plain CLR value first - without that step Convert.ToInt32/ToBoolean throw and every numeric
/// and boolean parameter silently falls back to its default. Both skill base classes delegate here
/// so the conversion cannot drift apart between them again.
/// </summary>

namespace Klacks.Api.Domain.Services.Assistant.Skills;

public static class SkillParameterReader
{
    /// <param name="parameters">Raw invocation arguments; values are JsonElement or already CLR values</param>
    /// <param name="name">Argument name as declared by the skill</param>
    /// <param name="defaultValue">Returned when the argument is absent, null, or not convertible</param>
    public static T? Read<T>(Dictionary<string, object> parameters, string name, T? defaultValue = default)
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
                return (T)(object)DateOnly.Parse(unwrapped.ToString()!);
            }

            if (typeof(T) == typeof(TimeOnly) || typeof(T) == typeof(TimeOnly?))
            {
                return (T)(object)TimeOnly.Parse(unwrapped.ToString()!);
            }

            if (typeof(T) == typeof(DateTime) || typeof(T) == typeof(DateTime?))
            {
                return (T)(object)DateTime.Parse(unwrapped.ToString()!);
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
}
