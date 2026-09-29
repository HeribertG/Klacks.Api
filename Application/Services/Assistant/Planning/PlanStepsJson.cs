// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Pure parser for AgentPlan.StepsJson that defines what counts as a step: an element of the top-level array that
/// is a JSON object with a non-blank string "Skill"/"skill" property. Empty, "[]", malformed or non-array JSON yields
/// no steps, and elements that are not objects or lack a skill are skipped. Shared by every caller that decides on
/// the step count (draft persistence, proposal rendering) so they can never disagree about an empty plan.
/// </summary>

using System.Text.Json;

namespace Klacks.Api.Application.Services.Assistant.Planning;

public static class PlanStepsJson
{
    private const string EmptyStepsJson = "[]";
    private const string SkillPascal = "Skill";
    private const string SkillCamel = "skill";
    private const string VerifySkillPascal = "VerifySkill";
    private const string VerifySkillCamel = "verifySkill";

    public static IReadOnlyList<PlanStepSummary> Parse(string? stepsJson)
    {
        var steps = new List<PlanStepSummary>();
        if (string.IsNullOrWhiteSpace(stepsJson) || stepsJson == EmptyStepsJson)
        {
            return steps;
        }

        try
        {
            using var doc = JsonDocument.Parse(stepsJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
            {
                return steps;
            }

            foreach (var element in doc.RootElement.EnumerateArray())
            {
                if (element.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                var skill = ReadString(element, SkillPascal, SkillCamel);
                if (string.IsNullOrWhiteSpace(skill))
                {
                    continue;
                }

                steps.Add(new PlanStepSummary(skill, ReadString(element, VerifySkillPascal, VerifySkillCamel)));
            }
        }
        catch (JsonException)
        {
            return [];
        }

        return steps;
    }

    public static int CountSteps(string? stepsJson) => Parse(stepsJson).Count;

    private static string? ReadString(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String)
            {
                return value.GetString();
            }
        }

        return null;
    }
}
