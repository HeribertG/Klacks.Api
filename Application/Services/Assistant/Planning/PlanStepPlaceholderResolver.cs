// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Resolves the "$prev." placeholders of a plan step's raw parameters against the result of the previous step: a string
/// value that starts with "$prev." is replaced by the member (dictionary key or property, case-insensitive, dotted path)
/// of the previous step's result data; a placeholder that cannot be resolved is left out of the parameters.
/// </summary>

namespace Klacks.Api.Application.Services.Assistant.Planning;

internal static class PlanStepPlaceholderResolver
{
    public const string PrevPlaceholderPrefix = "$prev.";

    public static Dictionary<string, object> ResolveParameters(
        Dictionary<string, object?> rawParams,
        IReadOnlyDictionary<int, object?> stepResults)
    {
        var resolved = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        if (rawParams.Count == 0) return resolved;

        var previousData = stepResults.Count > 0
            ? stepResults[stepResults.Keys.Max()]
            : null;

        foreach (var (key, value) in rawParams)
        {
            var resolvedValue = ResolveValue(value, previousData);
            if (resolvedValue != null)
            {
                resolved[key] = resolvedValue;
            }
        }
        return resolved;
    }

    private static object? ResolveValue(object? value, object? previousData)
    {
        if (value is string strValue && strValue.StartsWith(PrevPlaceholderPrefix, StringComparison.Ordinal))
        {
            var path = strValue[PrevPlaceholderPrefix.Length..];
            return ExtractFromObject(previousData, path);
        }
        return value;
    }

    private static object? ExtractFromObject(object? source, string path)
    {
        if (source == null) return null;
        var parts = path.Split('.');

        var current = source;
        foreach (var part in parts)
        {
            current = ExtractMember(current, part);
            if (current == null) return null;
        }
        return current;
    }

    private static object? ExtractMember(object? source, string memberName)
    {
        if (source == null) return null;

        if (source is IDictionary<string, object?> dictNullable && dictNullable.TryGetValue(memberName, out var v1))
        {
            return v1;
        }
        if (source is IDictionary<string, object> dict && dict.TryGetValue(memberName, out var v2))
        {
            return v2;
        }

        var property = source.GetType().GetProperty(memberName,
            System.Reflection.BindingFlags.IgnoreCase |
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.Instance);
        return property?.GetValue(source);
    }
}
