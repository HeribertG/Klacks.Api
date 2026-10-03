// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Separates the findings a planned write causes from the ones that already existed. Findings are grouped per
/// (rule, agent); a group is attributed to the write only when its total excess rises, because a key-by-key
/// comparison misreads anchor shifts (removing the first day of a too long run moves the run's anchor but
/// improves it). Inside a rising group every after-finding is reported unless a before-finding of the same
/// date already had at least the same excess. Known blind spot: a write that repairs one violation of a
/// (rule, agent) and creates another of equal excess is not reported.
/// </summary>

using Klacks.ScheduleOptimizer.Constraints.Rules;

namespace Klacks.Api.Application.Services.Schedules.PlanningRules;

public static class PlanningRuleFindingDelta
{
    public static IReadOnlyList<RuleFinding> NewOrWorsened(IReadOnlyList<RuleFinding> before, IReadOnlyList<RuleFinding> after)
    {
        ArgumentNullException.ThrowIfNull(before);
        ArgumentNullException.ThrowIfNull(after);
        if (after.Count == 0)
        {
            return [];
        }

        var beforeByGroup = before.ToLookup(GroupKey);
        var result = new List<RuleFinding>();
        foreach (var group in after.GroupBy(GroupKey))
        {
            var previous = beforeByGroup[group.Key].ToList();
            if (group.Sum(f => f.Excess) <= previous.Sum(f => f.Excess))
            {
                continue;
            }

            result.AddRange(group.Where(finding => !previous.Any(old => old.Date == finding.Date && old.Excess >= finding.Excess)));
        }

        return result;
    }

    private static (Guid RuleId, string? AgentId) GroupKey(RuleFinding finding) => (finding.RuleId, finding.AgentId);
}
