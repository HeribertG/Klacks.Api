// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Measures how much of the current catalogue the knowledge index already holds a row for, so startup
/// can tell a usable index from an empty or half-built one.
/// </summary>
/// <param name="skillRegistry">Registry providing all currently registered skill descriptors.</param>
/// <param name="recipeRepository">Repository providing all currently enabled recipes.</param>
/// <param name="repository">Repository for reading the stored index keys.</param>
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.KnowledgeIndex.Application.Interfaces;

namespace Klacks.Api.KnowledgeIndex.Application.Services;

public sealed class KnowledgeIndexCoverageProbe : IKnowledgeIndexCoverageProbe
{
    private const double Complete = 1.0;

    private readonly ISkillRegistry _skillRegistry;
    private readonly IAgentRecipeRepository _recipeRepository;
    private readonly IKnowledgeIndexRepository _repository;

    public KnowledgeIndexCoverageProbe(
        ISkillRegistry skillRegistry,
        IAgentRecipeRepository recipeRepository,
        IKnowledgeIndexRepository repository)
    {
        _skillRegistry = skillRegistry;
        _recipeRepository = recipeRepository;
        _repository = repository;
    }

    public async Task<double> GetStoredCoverageAsync(CancellationToken cancellationToken)
    {
        var recipes = await _recipeRepository.GetAllEnabledAsync(cancellationToken);
        var required = KnowledgeIndexCatalogueKeys.Build(_skillRegistry.GetAllSkills(), recipes);
        if (required.Count == 0)
        {
            return Complete;
        }

        var stored = await _repository.GetAllHashesAsync(cancellationToken);
        var present = required.Count(stored.ContainsKey);
        return (double)present / required.Count;
    }
}
