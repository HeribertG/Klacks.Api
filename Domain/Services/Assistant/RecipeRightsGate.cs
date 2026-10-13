// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Checks, before a matched recipe is offered, that the user holds the right of EVERY step skill, not only of the
/// writing ones: a step skill the user may not call is dropped from the turn's toolset and the forcing of that step
/// silently falls away, so the recipe would collect its answers and then end on a step it can never run. The check uses
/// the same rule as the toolset assembly (Permissions.HasAllRequiredPermissions, administrator bypass included), so a
/// recipe is denied exactly when one of its step skills would be missing from the toolset because of rights. The skill rows
/// are read for the default agent first, which is the agent the chat runs on; a chat on another agent would filter its own
/// rows, so a skill of the same name with a different requirement on that agent is the one case the two could disagree on. Asks no
/// question of the database for a recipe without step skills, and one projection query otherwise.
/// </summary>
/// <param name="skillRepository">Reads the RequiredPermission of the step skills by name</param>
/// <param name="userRights">The rights of the user the turn runs for, roles already expanded to permissions</param>

using System.Text.Json;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Assistant.Recipes;

namespace Klacks.Api.Domain.Services.Assistant;

public sealed class RecipeRightsGate
{
    private const char PermissionSeparator = ',';

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly IAgentSkillRepository _skillRepository;
    private readonly IReadOnlyList<string> _userRights;

    public RecipeRightsGate(IAgentSkillRepository skillRepository, IReadOnlyCollection<string> userRights)
    {
        _skillRepository = skillRepository;
        _userRights = userRights as IReadOnlyList<string> ?? userRights.ToList();
    }

    public async Task<RecipeDenial?> CheckAsync(AgentRecipe recipe, CancellationToken cancellationToken)
    {
        var skillNames = StepSkillNames(recipe);
        if (skillNames.Count == 0)
        {
            return null;
        }

        var requirements = await _skillRepository.GetRequiredPermissionsByNameAsync(skillNames, cancellationToken);
        var missing = requirements.Values
            .Where(required => !Permissions.HasAllRequiredPermissions(_userRights, required))
            .SelectMany(required => required!.Split(PermissionSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Where(right => !Permissions.HasPermission(_userRights, right))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        return missing.Count == 0 ? null : new RecipeDenial(recipe.Name, recipe.Goal, missing);
    }

    private static IReadOnlyCollection<string> StepSkillNames(AgentRecipe recipe)
    {
        if (string.IsNullOrWhiteSpace(recipe.StepsJson))
        {
            return [];
        }

        List<RecipeStep>? steps;
        try
        {
            steps = JsonSerializer.Deserialize<List<RecipeStep>>(recipe.StepsJson, JsonOptions);
        }
        catch (JsonException)
        {
            return [];
        }

        return steps?
            .Where(step => !string.IsNullOrWhiteSpace(step.Skill))
            .Select(step => step.Skill!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList() ?? [];
    }
}
