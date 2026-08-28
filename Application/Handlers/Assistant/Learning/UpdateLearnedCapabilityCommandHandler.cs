// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Edits the wording of a learned capability. Goal and trigger phrases are editable, the steps are not:
/// the steps are exactly what the execution oracle verified, and changing them would silently invalidate
/// that verdict. Refreshes the catalogue afterwards so the changed goal reaches the knowledge index the
/// recipe is retrieved through.
/// </summary>
/// <param name="recipeRepository">Recipe store</param>
/// <param name="catalogRefresher">Rebuilds the skill catalogue and the knowledge index</param>

using Klacks.Api.Application.Commands.Assistant.Learning;
using Klacks.Api.Application.Services.Assistant;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant.Learning;

public class UpdateLearnedCapabilityCommandHandler
    : IRequestHandler<UpdateLearnedCapabilityCommand, LearningMutationResult>
{
    public const int MinGoalLength = 3;

    private const string RefreshReason = "learned capability edited by an administrator";

    private readonly IAgentRecipeRepository _recipeRepository;
    private readonly ISkillCatalogRefresher _catalogRefresher;

    public UpdateLearnedCapabilityCommandHandler(
        IAgentRecipeRepository recipeRepository,
        ISkillCatalogRefresher catalogRefresher)
    {
        _recipeRepository = recipeRepository;
        _catalogRefresher = catalogRefresher;
    }

    public async Task<LearningMutationResult> Handle(
        UpdateLearnedCapabilityCommand request, CancellationToken cancellationToken)
    {
        var recipe = await _recipeRepository.GetByIdAsync(request.Id, cancellationToken);
        if (recipe == null)
        {
            return LearningMutationResult.NotFound();
        }

        if (request.Goal != null)
        {
            var goal = request.Goal.Trim();
            if (goal.Length < MinGoalLength)
            {
                return LearningMutationResult.Invalid($"Goal must be at least {MinGoalLength} characters.");
            }

            recipe.Goal = goal;
        }

        if (request.Synonyms != null)
        {
            recipe.Synonyms = request.Synonyms;
        }

        recipe.UpdateTime = DateTime.UtcNow;
        await _recipeRepository.UpdateAsync(recipe, cancellationToken);
        await _catalogRefresher.RefreshAsync(RefreshReason, cancellationToken);

        return LearningMutationResult.Success();
    }
}
