// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Deterministic naming bridge between a mutation skill's flattened result payload and the parameter
/// names a paired verify skill declares. It closes the gap where the flattened result key (e.g.
/// <c>EmployeeId</c>) does not match the verify skill's declared parameter (e.g. <c>clientId</c>) by
/// case alone, so the verify step would otherwise run with the parameter missing. The bridge is
/// conservative: it never guesses across differently named entities. Only these rules apply, and any
/// ambiguity produces no alias plus a human-readable note (never a guess):
/// - exact case-insensitive matches are handled by the caller's dictionary and treated as satisfied;
/// - a curated cross-prefix alias table (<see cref="VerifyParamAliases"/>) for domain-equivalent ids;
/// - a generic id rule: a declared bare <c>id</c> parameter is filled from the unique id-shaped result
///   key.
/// </summary>

namespace Klacks.Api.Application.Services.Assistant.Planning;

public static class VerifyParamNameBridge
{
    private const string IdSuffix = "Id";
    private const string GenericIdParamName = "id";

    /// <summary>
    /// Computes the additional verify parameters to inject.
    /// </summary>
    /// <param name="flattenedResult">The mutation result flattened to a case-insensitive key/value map.</param>
    /// <param name="declaredVerifyParamNames">The parameter names the verify skill declares.</param>
    /// <returns>Aliases keyed by the declared parameter name, plus notes describing skipped ambiguities.</returns>
    public static (IReadOnlyDictionary<string, object> Aliases, IReadOnlyList<string> Notes) BuildAliases(
        IReadOnlyDictionary<string, object> flattenedResult,
        IReadOnlyList<string> declaredVerifyParamNames)
    {
        var aliases = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        var notes = new List<string>();

        if (declaredVerifyParamNames == null || declaredVerifyParamNames.Count == 0
            || flattenedResult == null || flattenedResult.Count == 0)
        {
            return (aliases, notes);
        }

        foreach (var param in declaredVerifyParamNames)
        {
            if (IsSatisfied(flattenedResult, param))
            {
                continue;
            }

            if (TryBridgeFromAliasTable(flattenedResult, param, aliases, notes))
            {
                continue;
            }

            TryBridgeGenericId(flattenedResult, param, aliases, notes);
        }

        return (aliases, notes);
    }

    private static bool TryBridgeFromAliasTable(
        IReadOnlyDictionary<string, object> flattenedResult,
        string param,
        Dictionary<string, object> aliases,
        List<string> notes)
    {
        if (!VerifyParamAliases.SourceKeysByParam.TryGetValue(param, out var sourceKeys))
        {
            return false;
        }

        var present = flattenedResult.Keys
            .Where(key => sourceKeys.Any(source => string.Equals(source, key, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        if (present.Count == 1)
        {
            aliases[param] = flattenedResult[present[0]];
            return true;
        }

        if (present.Count > 1)
        {
            notes.Add($"parameter '{param}' not bridged: ambiguous alias sources [{string.Join(", ", present)}]");
            return true;
        }

        return false;
    }

    private static void TryBridgeGenericId(
        IReadOnlyDictionary<string, object> flattenedResult,
        string param,
        Dictionary<string, object> aliases,
        List<string> notes)
    {
        if (!string.Equals(param, GenericIdParamName, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var idKeys = flattenedResult.Keys.Where(IsIdShaped).ToList();
        if (idKeys.Count == 1)
        {
            aliases[param] = flattenedResult[idKeys[0]];
        }
        else if (idKeys.Count > 1)
        {
            notes.Add($"parameter '{param}' not bridged: ambiguous id-shaped source keys [{string.Join(", ", idKeys)}]");
        }
    }

    private static bool IsSatisfied(IReadOnlyDictionary<string, object> flattenedResult, string paramName)
        => flattenedResult.Keys.Any(key => string.Equals(key, paramName, StringComparison.OrdinalIgnoreCase));

    private static bool IsIdShaped(string name)
        => name.EndsWith(IdSuffix, StringComparison.Ordinal)
           || string.Equals(name, GenericIdParamName, StringComparison.OrdinalIgnoreCase);
}
