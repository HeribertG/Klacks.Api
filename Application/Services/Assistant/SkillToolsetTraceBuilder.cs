// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Builds the observation-only trace of a toolset assembly: which skills retrieval delivered, what the selection held
/// before the provider cap and what the cap removed.
/// </summary>
/// <param name="retrieved">Retrieved candidates that survived the exclusion, including prepended guarantees</param>
/// <param name="retrievalScores">The retrieval score per surfaced skill name; a candidate without one was not delivered by retrieval</param>
/// <param name="preCap">The selection before the provider cap</param>
/// <param name="postCap">The selection after the provider cap</param>

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Assistant;

internal static class SkillToolsetTraceBuilder
{
    public static SkillToolsetTrace Build(
        IReadOnlyCollection<AgentSkill> retrieved,
        IReadOnlyDictionary<string, double> retrievalScores,
        IReadOnlyCollection<AgentSkill> preCap,
        IReadOnlyCollection<AgentSkill> postCap)
    {
        var retrievedNames = retrieved
            .Where(s => retrievalScores.ContainsKey(s.Name))
            .OrderByDescending(s => retrievalScores[s.Name])
            .ThenBy(s => s.Name, StringComparer.Ordinal)
            .Select(s => s.Name)
            .ToList();

        var preCapNames = preCap.Select(s => s.Name).ToList();

        var truncatedNames = preCap.Count == postCap.Count
            ? []
            : RemovedNames(preCap, postCap);

        return new SkillToolsetTrace(retrievedNames, preCapNames, truncatedNames);
    }

    private static List<string> RemovedNames(IEnumerable<AgentSkill> preCap, IEnumerable<AgentSkill> postCap)
    {
        var kept = postCap.Select(s => s.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return preCap.Where(s => !kept.Contains(s.Name)).Select(s => s.Name).ToList();
    }
}
