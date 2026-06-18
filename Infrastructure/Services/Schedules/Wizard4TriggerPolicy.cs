// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Infrastructure.Services.Schedules;

/// <summary>
/// Pure decision brain of the Wizard-4 background trigger: given the groups currently being viewed in
/// the Original schedule and a per-group cooldown, decides which to optimise this tick. Deduplicates by
/// group (a group viewed by several users is optimised once) and skips groups still in cooldown so the
/// optimiser does not re-run the same plan every tick. Pure + deterministic so it is fully unit-tested;
/// the surrounding BackgroundService supplies presence, the gate, agent resolution and the run.
/// </summary>
public sealed class Wizard4TriggerPolicy
{
    /// <summary>Selects the distinct, not-in-cooldown groups to optimise. First occurrence of each group wins.</summary>
    public IReadOnlyList<Wizard4TriggerTarget> SelectTargets(
        IReadOnlyList<Wizard4TriggerTarget> viewedGroups,
        IReadOnlyDictionary<Guid, DateTime> cooldownUntil,
        DateTime now)
    {
        var result = new List<Wizard4TriggerTarget>();
        var seen = new HashSet<Guid>();
        foreach (var target in viewedGroups)
        {
            if (!seen.Add(target.GroupId))
            {
                continue;
            }

            if (cooldownUntil.TryGetValue(target.GroupId, out var until) && now < until)
            {
                continue;
            }

            result.Add(target);
        }

        return result;
    }
}
