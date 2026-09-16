// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Implementation of the G5 probe. Reads the same three deterministic layers SkillToolsetAssembler uses
/// - SkillMatchingEngine keyword/synonym matching over the permitted skills, RecipeForcingResolver, and
/// the engine recipe trigger with the semantic fallback switched off - and nothing else.
/// Called with userId/conversationId null on purpose: RecipeEngineService.GuaranteedSkillNamesAsync only
/// consults the paused-recipe branch when both are present, and the probe must ask "does THIS text route
/// by itself", not "is a recipe waiting for an answer" (that is gate G1, checked separately).
/// Propagates any failure rather than swallowing it into an empty result: an empty result means "does
/// not route alone", which OPENS the correction path, so a swallowed failure would silently bias towards
/// repairing. The single caller (TurnPreparationService) decides the fail-closed policy.
/// </summary>
/// <param name="skillCacheService">Source of the agent's enabled skills.</param>
/// <param name="recipeEngine">Engine recipe trigger matching, deterministic mode only.</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant;

namespace Klacks.Api.Application.Services.Assistant;

public class DeterministicRouteProbe : IDeterministicRouteProbe
{
    private readonly ISkillCacheService _skillCacheService;
    private readonly RecipeEngineService _recipeEngine;

    public DeterministicRouteProbe(ISkillCacheService skillCacheService, RecipeEngineService recipeEngine)
    {
        _skillCacheService = skillCacheService;
        _recipeEngine = recipeEngine;
    }

    public async Task<IReadOnlyList<string>> GuaranteedSkillNamesAsync(
        Agent? agent,
        IReadOnlyList<string> userRights,
        string message,
        string? language,
        CancellationToken cancellationToken = default)
    {
        if (agent == null || string.IsNullOrWhiteSpace(message))
        {
            return [];
        }

        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var skills = await _skillCacheService.GetEnabledSkillsAsync(agent.Id, cancellationToken);
        var permitted = skills
            .Where(s => Permissions.HasAllRequiredPermissions(userRights, s.RequiredPermission))
            .ToList();

        foreach (var name in SkillMatchingEngine.TopKeywordMatchedSkillNames(
                     permitted.Where(s => !s.AlwaysOn), message))
        {
            names.Add(name);
        }

        foreach (var name in RecipeForcingResolver.GuaranteedSkillNames(message))
        {
            names.Add(name);
        }

        foreach (var name in await _recipeEngine.GuaranteedSkillNamesAsync(
                     userId: null, conversationId: null, message, language, userRights,
                     allowSemanticFallback: false, cancellationToken))
        {
            names.Add(name);
        }

        return names.ToList();
    }
}
