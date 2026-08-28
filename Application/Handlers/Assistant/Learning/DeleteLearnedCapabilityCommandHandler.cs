// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Withdraws a learned capability. The recipe is disabled rather than deleted, and its cluster moves to
/// retired, so the loop knows this wish was answered and rejected and does not immediately learn the same
/// thing again on the next occurrence.
/// </summary>
/// <param name="recipeRepository">Recipe store</param>
/// <param name="clusterRepository">Cluster store, holds the link back to the wish</param>
/// <param name="catalogRefresher">Rebuilds the skill catalogue so the disabled recipe stops being retrieved</param>

using Klacks.Api.Application.Commands.Assistant.Learning;
using Klacks.Api.Application.Services.Assistant;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant.Learning;

public class DeleteLearnedCapabilityCommandHandler
    : IRequestHandler<DeleteLearnedCapabilityCommand, LearningMutationResult>
{
    private const int LinkedClusterLimit = 200;
    private const string RefreshReason = "learned capability withdrawn by an administrator";

    private static readonly IReadOnlyList<string> LearnedCapabilityStatuses =
        [SkillLearningClusterStatuses.LearnedCapability];

    private readonly IAgentRecipeRepository _recipeRepository;
    private readonly ISkillLearningClusterRepository _clusterRepository;
    private readonly ISkillCatalogRefresher _catalogRefresher;

    public DeleteLearnedCapabilityCommandHandler(
        IAgentRecipeRepository recipeRepository,
        ISkillLearningClusterRepository clusterRepository,
        ISkillCatalogRefresher catalogRefresher)
    {
        _recipeRepository = recipeRepository;
        _clusterRepository = clusterRepository;
        _catalogRefresher = catalogRefresher;
    }

    public async Task<LearningMutationResult> Handle(
        DeleteLearnedCapabilityCommand request, CancellationToken cancellationToken)
    {
        var recipe = await _recipeRepository.GetByIdAsync(request.Id, cancellationToken);
        if (recipe == null)
        {
            return LearningMutationResult.NotFound();
        }

        recipe.IsEnabled = false;
        recipe.UpdateTime = DateTime.UtcNow;
        await _recipeRepository.UpdateAsync(recipe, cancellationToken);

        await RetireLinkedClustersAsync(recipe.Name, cancellationToken);
        await _catalogRefresher.RefreshAsync(RefreshReason, cancellationToken);

        return LearningMutationResult.Success();
    }

    private async Task RetireLinkedClustersAsync(string recipeName, CancellationToken cancellationToken)
    {
        var clusters = await _clusterRepository.ListByStatusAsync(
            LearnedCapabilityStatuses, LinkedClusterLimit, cancellationToken);

        foreach (var cluster in clusters.Where(c => string.Equals(c.OutcomeRef, recipeName, StringComparison.Ordinal)))
        {
            await _clusterRepository.TryTransitionAsync(
                cluster.Id,
                SkillLearningClusterStatuses.LearnedCapability,
                SkillLearningClusterStatuses.Retired,
                cancellationToken);
        }
    }
}
