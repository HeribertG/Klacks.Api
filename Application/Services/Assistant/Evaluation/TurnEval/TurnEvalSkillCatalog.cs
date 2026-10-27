// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Name lookup over the skills installed in the eval environment, built once per run from the skill cache so the miss
/// classifier needs no repository access per item. Names are matched case-insensitively; when several agents carry the
/// same skill name the first one wins.
/// </summary>
/// <param name="skills">The enabled skills of the installation</param>

using System.Text.Json;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Evaluation.TurnEval;

public sealed class TurnEvalSkillCatalog
{
    private const string IdParameterSuffix = "Id";
    private const string ParameterNameProperty = "name";
    private const string ParameterRequiredProperty = "required";

    private readonly Dictionary<string, TurnEvalSkillInfo> _skills = new(StringComparer.OrdinalIgnoreCase);

    public TurnEvalSkillCatalog(IEnumerable<AgentSkill> skills)
    {
        foreach (var skill in skills)
        {
            _skills.TryAdd(skill.Name, new TurnEvalSkillInfo(skill.Effect, HasRequiredIdParameter(skill.ParametersJson)));
        }
    }

    public bool TryGet(string? name, out TurnEvalSkillInfo info)
    {
        if (name != null && _skills.TryGetValue(name, out var found))
        {
            info = found;
            return true;
        }

        info = null!;
        return false;
    }

    private static bool HasRequiredIdParameter(string parametersJson)
    {
        try
        {
            using var document = JsonDocument.Parse(parametersJson);
            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                return false;
            }

            return document.RootElement.EnumerateArray().Any(IsRequiredIdParameter);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool IsRequiredIdParameter(JsonElement parameter)
    {
        if (parameter.ValueKind != JsonValueKind.Object
            || !TryGetProperty(parameter, ParameterNameProperty, out var name)
            || name.ValueKind != JsonValueKind.String
            || !TryGetProperty(parameter, ParameterRequiredProperty, out var required)
            || required.ValueKind != JsonValueKind.True)
        {
            return false;
        }

        var text = name.GetString();
        return text != null && text.EndsWith(IdParameterSuffix, StringComparison.Ordinal);
    }

    private static bool TryGetProperty(JsonElement element, string name, out JsonElement value)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }
}
