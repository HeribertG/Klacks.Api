// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Default <see cref="IPlanningRuleCarryInLoader"/>. The read window comes from PlanRuleHorizon (neighbour days
/// of the sequence rules, whole calendar periods of the PeriodCount rules); the covered window
/// [from - coveredBoundaryDays, until + coveredBoundaryDays] is cut out, leaving at most one range before and
/// one after the period. Work rows are mapped like WizardHardConstraintBuilder maps them: clock times kept,
/// shift type inferred from the span, paid hours as duration fallback. Breaks never appear (a break is free).
/// </summary>
/// <param name="dataReader">Reads the Work rows of the agents in a date range</param>

using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Domain.Interfaces.Scheduling;
using Klacks.Api.Domain.Models.Scheduling;
using Klacks.ScheduleOptimizer.Constraints.Rules;
using Klacks.ScheduleOptimizer.TokenEvolution.Initialization;

namespace Klacks.Api.Application.Services.Schedules.PlanningRules;

public sealed class PlanningRuleCarryInLoader : IPlanningRuleCarryInLoader
{
    private readonly IPlanningRuleDataReader _dataReader;

    public PlanningRuleCarryInLoader(IPlanningRuleDataReader dataReader)
    {
        _dataReader = dataReader;
    }

    public async Task<IReadOnlyList<RuleSegment>> LoadAsync(
        IReadOnlyCollection<Guid> agentIds,
        DateOnly from,
        DateOnly until,
        IReadOnlyList<PlanRule> rules,
        Guid? analyseToken,
        int coveredBoundaryDays,
        CancellationToken cancellationToken = default)
    {
        if (agentIds.Count == 0 || rules.Count == 0)
        {
            return [];
        }

        var segments = new List<RuleSegment>();
        foreach (var (rangeFrom, rangeUntil) in CarryInRanges(rules, from, until, coveredBoundaryDays))
        {
            var works = await _dataReader.GetWorkSpansAsync(agentIds, rangeFrom, rangeUntil, analyseToken, cancellationToken);
            segments.AddRange(works.Select(ToSegment));
        }

        return segments;
    }

    /// <summary>
    /// Date ranges (inclusive) to read: the rule horizon minus the covered window, at most one range before
    /// and one after the period, never overlapping the period or the covered days.
    /// </summary>
    public static IReadOnlyList<(DateOnly From, DateOnly Until)> CarryInRanges(
        IReadOnlyList<PlanRule> rules, DateOnly from, DateOnly until, int coveredBoundaryDays)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(coveredBoundaryDays);
        var (windowFrom, windowUntil) = PlanRuleHorizon.BoundaryWindow(rules, from, until);
        var coveredFrom = from.AddDays(-coveredBoundaryDays);
        var coveredUntil = until.AddDays(coveredBoundaryDays);

        var ranges = new List<(DateOnly, DateOnly)>(2);
        if (windowFrom < coveredFrom)
        {
            ranges.Add((windowFrom, coveredFrom.AddDays(-1)));
        }

        if (windowUntil > coveredUntil)
        {
            ranges.Add((coveredUntil.AddDays(1), windowUntil));
        }

        return ranges;
    }

    public static RuleSegment ToSegment(PlanningRuleWorkSpan work) => new(
        AgentId: work.ClientId.ToString(),
        Date: work.Date,
        Start: work.StartTime,
        End: work.EndTime,
        ShiftTypeIndex: ShiftTypeInference.FromSpan(work.StartTime, work.EndTime),
        Hours: work.WorkTime);
}
