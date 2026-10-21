// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Parses AgentPlan.StepsJson into the executable PlanStep list: every object element of the top-level array with a
/// non-blank "Skill"/"skill" string becomes a step (PascalCase key wins over camelCase), Order defaults to the 1-based
/// position among the kept steps, Params values are mapped to string/long/double/bool/null or the raw JSON text of a
/// nested value. Empty, "[]", malformed or non-array JSON yields no steps. Unlike PlanStepsJson (the summary parser for
/// draft persistence and proposal rendering) this reads the full step including params.
/// </summary>
/// <param name="stepsJson">The plan's StepsJson column as persisted by the planner</param>

using System.Text.Json;

namespace Klacks.Api.Application.Services.Assistant.Planning;

internal static class PlanStepListParser
{
    private const string EmptyStepsJson = "[]";
    private const string SkillPascal = "Skill";
    private const string SkillCamel = "skill";
    private const string OrderPascal = "Order";
    private const string OrderCamel = "order";
    private const string VerifySkillPascal = "VerifySkill";
    private const string VerifySkillCamel = "verifySkill";
    private const string ReversiblePascal = "Reversible";
    private const string ReversibleCamel = "reversible";
    private const string ParamsPascal = "Params";
    private const string ParamsCamel = "params";

    public static List<PlanStep> Parse(string stepsJson)
    {
        if (string.IsNullOrWhiteSpace(stepsJson) || stepsJson == EmptyStepsJson) return [];
        try
        {
            using var doc = JsonDocument.Parse(stepsJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Array) return [];

            var list = new List<PlanStep>();
            foreach (var element in doc.RootElement.EnumerateArray())
            {
                if (element.ValueKind != JsonValueKind.Object) continue;
                if (!element.TryGetProperty(SkillPascal, out var skillEl) &&
                    !element.TryGetProperty(SkillCamel, out skillEl))
                {
                    continue;
                }
                var skill = skillEl.GetString();
                if (string.IsNullOrWhiteSpace(skill)) continue;

                var order = TryGetInt(element, OrderPascal, OrderCamel) ?? list.Count + 1;
                var verify = TryGetString(element, VerifySkillPascal, VerifySkillCamel);
                var reversible = TryGetBool(element, ReversiblePascal, ReversibleCamel) ?? false;

                list.Add(new PlanStep(order, skill, ReadParams(element), verify, reversible));
            }
            return list;
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static Dictionary<string, object?> ReadParams(JsonElement element)
    {
        var paramsMap = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        if ((element.TryGetProperty(ParamsPascal, out var paramsEl) ||
             element.TryGetProperty(ParamsCamel, out paramsEl)) &&
            paramsEl.ValueKind == JsonValueKind.Object)
        {
            foreach (var p in paramsEl.EnumerateObject())
            {
                paramsMap[p.Name] = JsonValueAsObject(p.Value);
            }
        }

        return paramsMap;
    }

    private static int? TryGetInt(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (element.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out var i))
            {
                return i;
            }
        }
        return null;
    }

    private static string? TryGetString(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (element.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String)
            {
                return v.GetString();
            }
        }
        return null;
    }

    private static bool? TryGetBool(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (element.TryGetProperty(name, out var v))
            {
                if (v.ValueKind == JsonValueKind.True) return true;
                if (v.ValueKind == JsonValueKind.False) return false;
            }
        }
        return null;
    }

    private static object? JsonValueAsObject(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.TryGetInt64(out var l) ? (object)l : value.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => value.GetRawText()
        };
    }
}
