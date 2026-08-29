// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Builds the "capabilities" section from the artefacts the loop activated, adding what the weekly
/// fitness snapshot says about each. The "first use still owed" mark has two conditions and needs both:
/// the execution oracle could not run the composition end to end, AND nobody has since run it for real
/// without correcting it. The second condition is what clears the mark on its own - a badge only a
/// background job could ever remove would sit there forever.
/// Read through the cluster rather than through a flag on agent_recipes, which keeps a recipe an
/// administrator disabled by hand out of the list without that table needing to know about learning.
/// </summary>
/// <param name="artefactResolver">Activated artefacts, phrases and capabilities alike</param>
/// <param name="recipeRepository">Resolves a capability's name to the recipe it created</param>
/// <param name="fitnessRepository">Latest usefulness snapshot per artefact</param>
/// <param name="trajectoryRepository">Answers whether a capability has ever run for real</param>

using System.Text.Json;
using Klacks.Api.Application.DTOs.Assistant.Learning;
using Klacks.Api.Application.Queries.Assistant.Learning;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Assistant.Recipes;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant.Learning;

public class GetLearnedCapabilitiesQueryHandler
    : BaseHandler, IRequestHandler<GetLearnedCapabilitiesQuery, IReadOnlyList<LearnedCapabilityDto>>
{
    private static readonly JsonSerializerOptions StepJsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ILearnedArtefactResolver _artefactResolver;
    private readonly IAgentRecipeRepository _recipeRepository;
    private readonly ISkillLearningFitnessRepository _fitnessRepository;
    private readonly ISkillSelectionTrajectoryRepository _trajectoryRepository;

    public GetLearnedCapabilitiesQueryHandler(
        ILearnedArtefactResolver artefactResolver,
        IAgentRecipeRepository recipeRepository,
        ISkillLearningFitnessRepository fitnessRepository,
        ISkillSelectionTrajectoryRepository trajectoryRepository,
        ILogger<GetLearnedCapabilitiesQueryHandler> logger)
        : base(logger)
    {
        _artefactResolver = artefactResolver;
        _recipeRepository = recipeRepository;
        _fitnessRepository = fitnessRepository;
        _trajectoryRepository = trajectoryRepository;
    }

    public async Task<IReadOnlyList<LearnedCapabilityDto>> Handle(
        GetLearnedCapabilitiesQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(
            async () =>
            {
                var artefacts = (await _artefactResolver.ListActiveAsync(request.Limit, cancellationToken))
                    .Where(a => string.Equals(a.Kind, SkillLearningOutcomeKinds.Capability, StringComparison.Ordinal))
                    .ToList();

                var fitness = await _fitnessRepository.GetLatestForCandidatesAsync(
                    [.. artefacts.Where(a => a.CandidateId != null).Select(a => a.CandidateId!.Value)],
                    cancellationToken);

                var capabilities = new List<LearnedCapabilityDto>();

                foreach (var artefact in artefacts)
                {
                    var recipe = await _recipeRepository.GetByNameAsync(artefact.OwnerName, cancellationToken);
                    if (recipe == null)
                    {
                        continue;
                    }

                    capabilities.Add(await BuildAsync(artefact, recipe, fitness, cancellationToken));
                }

                return (IReadOnlyList<LearnedCapabilityDto>)capabilities;
            },
            "get learned capabilities",
            new { request.Limit });
    }

    private async Task<LearnedCapabilityDto> BuildAsync(
        LearnedArtefact artefact,
        AgentRecipe recipe,
        IReadOnlyDictionary<Guid, SkillLearningFitness> fitness,
        CancellationToken cancellationToken)
    {
        var snapshot = artefact.CandidateId != null && fitness.TryGetValue(artefact.CandidateId.Value, out var found)
            ? found
            : null;

        var needsFirstUse = artefact.ExecutionUnproven
            && !await _trajectoryRepository.HasSuccessfulRecipeTurnAsync(recipe.Name, cancellationToken);

        return new LearnedCapabilityDto(
            recipe.Id,
            recipe.Name,
            recipe.Goal,
            ParseSteps(recipe.StepsJson),
            artefact.ActivatedAtUtc,
            snapshot?.Quote,
            snapshot?.Uses,
            needsFirstUse);
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
