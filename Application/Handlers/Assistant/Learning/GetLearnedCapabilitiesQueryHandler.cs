// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Builds the "capabilities" section from the clusters that ended in a learned recipe, resolving each
/// cluster's OutcomeRef to the recipe it created. Reading through the cluster rather than through a flag
/// on agent_recipes is deliberate: it needs no column on that table, so stage G3 can add its origin
/// marker without this section having to change.
/// Returns an empty list until stage G3 starts producing capabilities.
/// </summary>
/// <param name="clusterRepository">Clusters that ended in a learned capability</param>
/// <param name="recipeRepository">Resolves a cluster's outcome reference to the actual recipe</param>

using System.Text.Json;
using Klacks.Api.Application.DTOs.Assistant.Learning;
using Klacks.Api.Application.Queries.Assistant.Learning;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant.Recipes;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant.Learning;

public class GetLearnedCapabilitiesQueryHandler
    : BaseHandler, IRequestHandler<GetLearnedCapabilitiesQuery, IReadOnlyList<LearnedCapabilityDto>>
{
    private static readonly JsonSerializerOptions StepJsonOptions =
        new(JsonSerializerDefaults.Web);

    private static readonly IReadOnlyList<string> LearnedCapabilityStatuses =
        [SkillLearningClusterStatuses.LearnedCapability];

    private readonly ISkillLearningClusterRepository _clusterRepository;
    private readonly IAgentRecipeRepository _recipeRepository;

    public GetLearnedCapabilitiesQueryHandler(
        ISkillLearningClusterRepository clusterRepository,
        IAgentRecipeRepository recipeRepository,
        ILogger<GetLearnedCapabilitiesQueryHandler> logger)
        : base(logger)
    {
        _clusterRepository = clusterRepository;
        _recipeRepository = recipeRepository;
    }

    public async Task<IReadOnlyList<LearnedCapabilityDto>> Handle(
        GetLearnedCapabilitiesQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(
            async () =>
            {
                var clusters = await _clusterRepository.ListByStatusAsync(
                    LearnedCapabilityStatuses, request.Limit, cancellationToken);

                var capabilities = new List<LearnedCapabilityDto>();

                foreach (var cluster in clusters.Where(c => !string.IsNullOrWhiteSpace(c.OutcomeRef)))
                {
                    var recipe = await _recipeRepository.GetByNameAsync(cluster.OutcomeRef!, cancellationToken);
                    if (recipe == null)
                    {
                        continue;
                    }

                    capabilities.Add(new LearnedCapabilityDto(
                        recipe.Id,
                        recipe.Name,
                        recipe.Goal,
                        ParseSteps(recipe.StepsJson),
                        cluster.LearnedAtUtc,
                        null,
                        null,
                        false));
                }

                return (IReadOnlyList<LearnedCapabilityDto>)capabilities;
            },
            "get learned capabilities",
            new { request.Limit });
    }

    private static IReadOnlyList<LearnedCapabilityStepDto> ParseSteps(string stepsJson)
    {
        if (string.IsNullOrWhiteSpace(stepsJson))
        {
            return [];
        }

        try
        {
            var steps = JsonSerializer.Deserialize<List<RecipeStep>>(stepsJson, StepJsonOptions);
            return steps == null
                ? []
                : steps.Select(step => new LearnedCapabilityStepDto(step.Kind, step.Skill)).ToList();
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
