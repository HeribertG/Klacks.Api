// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The set of index keys the current catalogue requires: one per registered skill and one per enabled
/// recipe. Shared by the synchronizer (orphan detection) and the startup coverage probe so both agree
/// on what a complete index is.
/// </summary>
/// <param name="skills">All currently registered skill descriptors.</param>
/// <param name="recipes">All currently enabled recipes.</param>
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.KnowledgeIndex.Domain;

namespace Klacks.Api.KnowledgeIndex.Application.Services;

public static class KnowledgeIndexCatalogueKeys
{
    public static HashSet<(KnowledgeEntryKind Kind, string SourceId)> Build(
        IEnumerable<SkillDescriptor> skills,
        IEnumerable<AgentRecipe> recipes)
    {
        var keys = new HashSet<(KnowledgeEntryKind Kind, string SourceId)>();

        foreach (var skill in skills)
            keys.Add((KnowledgeEntryKind.Skill, skill.Name));

        foreach (var recipe in recipes)
            keys.Add((KnowledgeEntryKind.Recipe, recipe.Name));

        return keys;
    }
}
