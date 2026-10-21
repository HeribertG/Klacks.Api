// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Assembles the parameters of a verify-skill call. The verify skill receives the preceding mutation's RESULT payload
/// (e.g. the created entity id), not the mutation's own input parameters: the payload is flattened case-insensitively
/// (dictionary entries or public readable properties, nulls dropped, strings and primitives contribute nothing), then
/// run through <see cref="VerifyParamNameBridge"/>, which fills a verify parameter whose name does not match a result key
/// by case alone (e.g. result 'EmployeeId' -> verify 'clientId') without overwriting an existing key; ambiguous cases are
/// reported as notes and left unbridged rather than guessed. Last, every non-null literal step parameter overrides the
/// payload, while unresolved "$prev." placeholders are skipped.
/// </summary>
/// <param name="step">The plan step whose literal Params are applied last</param>
/// <param name="mutationData">Result payload of the mutation that just ran</param>
/// <param name="declaredVerifyParamNames">Parameter names the verify skill declares, used by the alias bridge</param>

namespace Klacks.Api.Application.Services.Assistant.Planning;

internal static class PlanVerifyParameterBuilder
{
    public static Dictionary<string, object> Build(
        PlanStep step,
        object? mutationData,
        IReadOnlyList<string> declaredVerifyParamNames,
        out IReadOnlyList<string> bridgeNotes)
    {
        var parameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        foreach (var (key, value) in FlattenPayloadToParameters(mutationData))
        {
            parameters[key] = value;
        }

        var bridge = VerifyParamNameBridge.BuildAliases(parameters, declaredVerifyParamNames);
        bridgeNotes = bridge.Notes;
        foreach (var (paramName, value) in bridge.Aliases)
        {
            if (!parameters.ContainsKey(paramName))
            {
                parameters[paramName] = value;
            }
        }

        foreach (var (key, value) in step.Params)
        {
            if (value is string s && s.StartsWith(PlanStepPlaceholderResolver.PrevPlaceholderPrefix, StringComparison.Ordinal))
            {
                continue;
            }
            if (value != null)
            {
                parameters[key] = value;
            }
        }

        return parameters;
    }

    private static IEnumerable<KeyValuePair<string, object>> FlattenPayloadToParameters(object? payload)
    {
        if (payload == null)
        {
            yield break;
        }

        if (payload is IDictionary<string, object?> nullableDict)
        {
            foreach (var (key, value) in nullableDict)
            {
                if (value != null)
                {
                    yield return new KeyValuePair<string, object>(key, value);
                }
            }
            yield break;
        }

        if (payload is IDictionary<string, object> dict)
        {
            foreach (var (key, value) in dict)
            {
                yield return new KeyValuePair<string, object>(key, value);
            }
            yield break;
        }

        if (payload is string || payload.GetType().IsPrimitive)
        {
            yield break;
        }

        foreach (var property in payload.GetType().GetProperties(
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
        {
            if (!property.CanRead || property.GetIndexParameters().Length > 0)
            {
                continue;
            }
            var value = property.GetValue(payload);
            if (value != null)
            {
                yield return new KeyValuePair<string, object>(property.Name, value);
            }
        }
    }
}
