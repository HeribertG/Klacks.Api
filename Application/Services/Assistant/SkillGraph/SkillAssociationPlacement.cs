// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The pure place-keeping of the skill-graph association: which retrieved skills keep their places and seed the
/// graph, which candidates may compete for the open places, and how the chosen ones are merged back with the
/// retrieved skills they displace last. Separated from SkillToolsetAssembler so the rule can be tested without the
/// whole assembly.
/// </summary>

using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.KnowledgeIndex.Domain;

namespace Klacks.Api.Application.Services.Assistant.SkillGraph;

public static class SkillAssociationPlacement
{
    /// <summary>
    /// Orders the retrieved skills by score (unscored last, then by name) and splits off the seeds that keep their
    /// places; the rest stays in score order for the fill-back.
    /// </summary>
    /// <param name="retrievedSkills">The retrieved, permitted, non-always-on skills in any order</param>
    /// <param name="retrievalScores">The retrieval score per returned skill name</param>
    /// <param name="slots">The number of open places, at least 1 and below the retrieved count</param>
    public static (List<AgentSkill> Ranked, List<AgentSkill> Seeds, Dictionary<string, double> SeedScores) SplitSeeds(
        IReadOnlyList<AgentSkill> retrievedSkills, IReadOnlyDictionary<string, double> retrievalScores, int slots)
    {
        var ranked = retrievedSkills
            .OrderByDescending(s => retrievalScores.TryGetValue(s.Name, out var score) ? score : double.NegativeInfinity)
            .ThenBy(s => s.Name, StringComparer.Ordinal)
            .ToList();
        var seeds = ranked.Take(ranked.Count - slots).ToList();
        var seedScores = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        foreach (var seed in seeds)
        {
            if (retrievalScores.TryGetValue(seed.Name, out var score))
            {
                seedScores.TryAdd(seed.Name, score);
            }
        }

        return (ranked, seeds, seedScores);
    }

    /// <summary>
    /// The skills that may take an open place: permitted, not always-on, not a seed, each once.
    /// </summary>
    /// <param name="permittedSkills">Skills the user may use</param>
    /// <param name="seeds">The retrieved skills that keep their places</param>
    public static Dictionary<string, AgentSkill> EligibleSkills(
        IEnumerable<AgentSkill> permittedSkills, IEnumerable<AgentSkill> seeds)
    {
        var seedNames = seeds.Select(s => s.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var eligible = new Dictionary<string, AgentSkill>(StringComparer.OrdinalIgnoreCase);
        foreach (var skill in permittedSkills.Where(s => !s.AlwaysOn && !seedNames.Contains(s.Name)))
        {
            eligible.TryAdd(skill.Name, skill);
        }

        return eligible;
    }

    /// <summary>
    /// The cosine score of every eligible pool candidate, first occurrence wins.
    /// </summary>
    /// <param name="associationPool">The cosine-scored deep candidates of the retrieval pass</param>
    /// <param name="eligible">The skills that may take an open place</param>
    public static Dictionary<string, double> PoolScores(
        IEnumerable<RetrievalCandidate> associationPool, IReadOnlyDictionary<string, AgentSkill> eligible)
    {
        var poolScores = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        foreach (var candidate in associationPool.Where(c => eligible.ContainsKey(c.Entry.SourceId)))
        {
            poolScores.TryAdd(candidate.Entry.SourceId, candidate.Score);
        }

        return poolScores;
    }

    /// <summary>
    /// Seeds first, then the associated skills, then the remaining retrieved skills in score order, cut to the
    /// retrieved count: every open place the association left unused goes back to the next retrieved skill.
    /// </summary>
    /// <param name="ranked">All retrieved skills in score order</param>
    /// <param name="seeds">The retrieved skills that keep their places</param>
    /// <param name="picks">The associated skills, best first</param>
    public static List<AgentSkill> Merge(
        IReadOnlyList<AgentSkill> ranked, IReadOnlyList<AgentSkill> seeds, IReadOnlyList<AgentSkill> picks)
    {
        var pickNames = picks.Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return seeds
            .Concat(picks)
            .Concat(ranked.Skip(seeds.Count).Where(s => !pickNames.Contains(s.Name)))
            .Take(ranked.Count)
            .ToList();
    }
}
