// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Experience-based learner of the emergent skill-relationship graph. From recent successful skill
/// usage grouped per session it reinforces or creates co-required edges (PMI/lift over base rates)
/// and sequential edges (conditional probability that B follows A). Confidence rises slowly on
/// positive evidence and falls fast on genuine contradiction; pairs with too little data are left
/// unchanged (cold-start safe). It corrects the substrate prior up or down but never deletes edges.
/// </summary>
/// <param name="usageRepository">Source of recent per-session skill usage records.</param>
/// <param name="relationRepository">Persistence of the skill-relationship edges.</param>
/// <param name="logger">Diagnostic logging of the learning outcome.</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Assistant.SkillGraph;

public class SkillRelationLearner : ISkillRelationLearner
{
    private readonly ISkillUsageRepository _usageRepository;
    private readonly ISkillRelationRepository _relationRepository;
    private readonly ILogger<SkillRelationLearner> _logger;

    public SkillRelationLearner(
        ISkillUsageRepository usageRepository,
        ISkillRelationRepository relationRepository,
        ILogger<SkillRelationLearner> logger)
    {
        _usageRepository = usageRepository;
        _relationRepository = relationRepository;
        _logger = logger;
    }

    public async Task<int> LearnAsync(CancellationToken cancellationToken = default)
    {
        var from = DateTime.UtcNow.AddDays(-SkillGraphConstants.LearningWindowDays);
        var records = await _usageRepository.GetRecordsAsync(from, cancellationToken);

        var sessions = BuildSessions(records);
        if (sessions.Count < SkillGraphConstants.MinSessionsForLearning)
        {
            _logger.LogInformation(
                "Skill-relation learning skipped: {Count} eligible session(s) (< {Min}).",
                sessions.Count, SkillGraphConstants.MinSessionsForLearning);
            return 0;
        }

        var stats = ComputeStats(sessions);
        var existing = await _relationRepository.GetAllAsync(cancellationToken);
        var agentIds = existing.Select(e => e.AgentId).Distinct().ToList();

        var toAdd = new List<SkillRelation>();
        var toUpdate = new List<SkillRelation>();

        foreach (var agentId in agentIds)
        {
            var agentEdges = existing.Where(e => e.AgentId == agentId).ToList();
            ApplyCoRequired(agentId, agentEdges, stats, toAdd, toUpdate);
            ApplySequential(agentId, agentEdges, stats, toAdd, toUpdate);
        }

        if (toUpdate.Count > 0)
        {
            await _relationRepository.UpdateRangeAsync(toUpdate, cancellationToken);
        }

        if (toAdd.Count > 0)
        {
            await _relationRepository.AddRangeAsync(toAdd, cancellationToken);
        }

        _logger.LogInformation(
            "Skill-relation learning complete: {Updated} updated, {Added} learned, {Agents} agent(s), {Sessions} session(s).",
            toUpdate.Count, toAdd.Count, agentIds.Count, sessions.Count);

        return toUpdate.Count + toAdd.Count;
    }

    private void ApplyCoRequired(
        Guid agentId, List<SkillRelation> agentEdges, UsageStats stats,
        List<SkillRelation> toAdd, List<SkillRelation> toUpdate)
    {
        var edges = agentEdges
            .Where(e => e.Type == SkillRelationType.CoRequired)
            .ToDictionary(e => (e.SkillAName, e.SkillBName));
        var handled = new HashSet<(string, string)>();

        foreach (var (key, coOccur) in stats.PairCoOccurrence)
        {
            if (!Evaluable(stats, key.Item1, key.Item2))
            {
                continue;
            }

            handled.Add(key);
            var lift = Lift(stats, key.Item1, key.Item2, coOccur);
            edges.TryGetValue(key, out var edge);

            if (coOccur >= SkillGraphConstants.MinCoOccurrenceSupport && lift >= SkillGraphConstants.MinLiftForPositive)
            {
                if (edge != null)
                {
                    Reinforce(edge, SkillGraphConstants.CoRequiredActiveThreshold);
                    toUpdate.Add(edge);
                }
                else
                {
                    toAdd.Add(NewLearnedEdge(agentId, key.Item1, key.Item2, SkillRelationType.CoRequired,
                        SkillGraphConstants.LearnedCoOccurrenceProvenance, SkillGraphConstants.CoRequiredActiveThreshold));
                }
            }
            else if (edge != null && lift < SkillGraphConstants.MaxLiftForContradiction)
            {
                Decay(edge, SkillGraphConstants.CoRequiredActiveThreshold);
                toUpdate.Add(edge);
            }
        }

        foreach (var edge in edges.Values)
        {
            var key = (edge.SkillAName, edge.SkillBName);
            if (handled.Contains(key) || !Evaluable(stats, edge.SkillAName, edge.SkillBName))
            {
                continue;
            }

            Decay(edge, SkillGraphConstants.CoRequiredActiveThreshold);
            toUpdate.Add(edge);
        }
    }

    private void ApplySequential(
        Guid agentId, List<SkillRelation> agentEdges, UsageStats stats,
        List<SkillRelation> toAdd, List<SkillRelation> toUpdate)
    {
        var edges = agentEdges
            .Where(e => e.Type == SkillRelationType.Sequential)
            .ToDictionary(e => (e.SkillAName, e.SkillBName));
        var handled = new HashSet<(string, string)>();

        foreach (var (key, adjacency) in stats.Adjacency)
        {
            if (!Evaluable(stats, key.Item1, key.Item2))
            {
                continue;
            }

            handled.Add(key);
            var probability = (double)adjacency / stats.SkillSessions[key.Item1];
            edges.TryGetValue(key, out var edge);

            if (adjacency >= SkillGraphConstants.MinCoOccurrenceSupport && probability >= SkillGraphConstants.MinSequentialProbability)
            {
                if (edge != null)
                {
                    Reinforce(edge, SkillGraphConstants.SequentialActiveThreshold);
                    toUpdate.Add(edge);
                }
                else
                {
                    toAdd.Add(NewLearnedEdge(agentId, key.Item1, key.Item2, SkillRelationType.Sequential,
                        SkillGraphConstants.LearnedSequentialProvenance, SkillGraphConstants.SequentialActiveThreshold));
                }
            }
            else if (edge != null && probability < SkillGraphConstants.MinSequentialProbability)
            {
                Decay(edge, SkillGraphConstants.SequentialActiveThreshold);
                toUpdate.Add(edge);
            }
        }

        foreach (var edge in edges.Values)
        {
            var key = (edge.SkillAName, edge.SkillBName);
            if (handled.Contains(key) || !Evaluable(stats, edge.SkillAName, edge.SkillBName))
            {
                continue;
            }

            Decay(edge, SkillGraphConstants.SequentialActiveThreshold);
            toUpdate.Add(edge);
        }
    }

    private static bool Evaluable(UsageStats stats, string skillA, string skillB)
    {
        return stats.SkillSessions.GetValueOrDefault(skillA) >= SkillGraphConstants.MinSkillOccurrenceForEval
            && stats.SkillSessions.GetValueOrDefault(skillB) >= SkillGraphConstants.MinSkillOccurrenceForEval;
    }

    private static double Lift(UsageStats stats, string skillA, string skillB, int coOccur)
    {
        var countA = stats.SkillSessions[skillA];
        var countB = stats.SkillSessions[skillB];
        return (double)coOccur * stats.TotalSessions / ((double)countA * countB);
    }

    private static void Reinforce(SkillRelation edge, double activeThreshold)
    {
        edge.Confidence = Math.Min(SkillGraphConstants.MaxLearnedConfidence,
            edge.Confidence + SkillGraphConstants.ReinforcementStep);
        edge.SupportCount += 1;
        edge.LastReinforcedAt = DateTime.UtcNow;
        edge.Source = SkillRelationSource.Learned;
        SetStatus(edge, activeThreshold);
    }

    private static void Decay(SkillRelation edge, double activeThreshold)
    {
        edge.Confidence = Math.Max(0, edge.Confidence - SkillGraphConstants.DecayStep);
        edge.ContradictionCount += 1;
        SetStatus(edge, activeThreshold);
    }

    private static void SetStatus(SkillRelation edge, double activeThreshold)
    {
        edge.Status = edge.Confidence <= SkillGraphConstants.RetireConfidence
            ? SkillRelationStatus.Retired
            : edge.Confidence >= activeThreshold
                ? SkillRelationStatus.Active
                : SkillRelationStatus.Candidate;
    }

    private static SkillRelation NewLearnedEdge(
        Guid agentId, string skillAName, string skillBName, SkillRelationType type, string provenance, double activeThreshold)
    {
        var edge = new SkillRelation
        {
            AgentId = agentId,
            SkillAName = skillAName,
            SkillBName = skillBName,
            Type = type,
            Confidence = SkillGraphConstants.InitialLearnedConfidence,
            SupportCount = 1,
            ContradictionCount = 0,
            Provenance = provenance,
            Source = SkillRelationSource.Learned,
            LastReinforcedAt = DateTime.UtcNow,
        };
        SetStatus(edge, activeThreshold);
        return edge;
    }

    private static List<List<string>> BuildSessions(IReadOnlyList<SkillUsageRecord> records)
    {
        return records
            .Where(r => r.Success && !string.IsNullOrEmpty(r.SessionId) && IsEligible(r.Category))
            .GroupBy(r => r.SessionId!)
            .Select(g => g.OrderBy(r => r.Timestamp).Select(r => r.SkillName).ToList())
            .Where(list => list.Count > 0)
            .ToList();
    }

    private static bool IsEligible(SkillCategory category)
    {
        return category != SkillCategory.UI && category != SkillCategory.System;
    }

    private static UsageStats ComputeStats(List<List<string>> sessions)
    {
        var stats = new UsageStats { TotalSessions = sessions.Count };

        foreach (var ordered in sessions)
        {
            var distinct = ordered.Distinct().ToList();
            foreach (var skill in distinct)
            {
                stats.SkillSessions[skill] = stats.SkillSessions.GetValueOrDefault(skill) + 1;
            }

            for (var i = 0; i < distinct.Count; i++)
            {
                for (var j = i + 1; j < distinct.Count; j++)
                {
                    var key = OrderedPair(distinct[i], distinct[j]);
                    stats.PairCoOccurrence[key] = stats.PairCoOccurrence.GetValueOrDefault(key) + 1;
                }
            }

            var seenAdjacency = new HashSet<(string, string)>();
            for (var i = 0; i + 1 < ordered.Count; i++)
            {
                var pair = (ordered[i], ordered[i + 1]);
                if (pair.Item1 != pair.Item2 && seenAdjacency.Add(pair))
                {
                    stats.Adjacency[pair] = stats.Adjacency.GetValueOrDefault(pair) + 1;
                }
            }
        }

        return stats;
    }

    private static (string, string) OrderedPair(string skillA, string skillB)
    {
        return string.CompareOrdinal(skillA, skillB) <= 0 ? (skillA, skillB) : (skillB, skillA);
    }

    private sealed class UsageStats
    {
        public int TotalSessions { get; init; }

        public Dictionary<string, int> SkillSessions { get; } = new();

        public Dictionary<(string, string), int> PairCoOccurrence { get; } = new();

        public Dictionary<(string, string), int> Adjacency { get; } = new();
    }
}
