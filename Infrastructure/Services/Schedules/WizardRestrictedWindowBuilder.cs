// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Default <see cref="IWizardRestrictedWindowBuilder"/>. Loads the active RestrictedTimeWindowRule set and,
/// per rule, resolves the concrete set of shift ids in scope for its group tag among the period's shifts.
/// </summary>
/// <param name="ruleRepository">Reads the active RestrictedTimeWindowRule set</param>
/// <param name="context">Database access for the GroupItem/Group tag-scoping join</param>
/// <remarks>
/// Group membership is resolved by SHIFT ID with NO AnalyseToken filter (non-deleted only), the same
/// token-independent treatment the wizard qualification eligibility uses. In scenario mode the shift ids
/// handed in are the clone ids, and CloneShifts clones each shift's group links to the clone id, so the
/// unfiltered query finds them; real and clone shift-id spaces are disjoint, so it never leaks. An empty group tag resolves to ALL
/// period shifts. Scope is resolved at the PERIOD level (a shift is restricted when its tag-group membership
/// overlaps the period) - a deliberate, safe over-approximation: the per-day season test inside
/// CoreRestrictedTimeWindow still gates the actual veto to in-season days, and the authoritative per-date
/// scope is enforced by the API-side RestrictedTimeWindowEvaluator at save time.
/// </remarks>

using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Domain.Interfaces.Scheduling;
using Klacks.Api.Infrastructure.Persistence;
using Klacks.ScheduleOptimizer.Models;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.Schedules;

public sealed class WizardRestrictedWindowBuilder : IWizardRestrictedWindowBuilder
{
    private readonly IRestrictedTimeWindowRuleRepository _ruleRepository;
    private readonly DataBaseContext _context;

    public WizardRestrictedWindowBuilder(
        IRestrictedTimeWindowRuleRepository ruleRepository,
        DataBaseContext context)
    {
        _ruleRepository = ruleRepository;
        _context = context;
    }

    private readonly record struct ShiftTagLink(Guid ShiftId, string Name);

    public async Task<IReadOnlyList<CoreRestrictedTimeWindow>> BuildAsync(
        IReadOnlyList<Guid> shiftIds,
        DateOnly from,
        DateOnly until,
        CancellationToken ct)
    {
        var rules = await _ruleRepository.GetAllActiveAsync();
        if (rules.Count == 0)
        {
            return [];
        }

        var distinctShiftIds = shiftIds.Where(id => id != Guid.Empty).Distinct().ToList();
        var shiftIdsByTag = await LoadShiftIdsByTagAsync(distinctShiftIds, from, until, ct);
        var allShiftIds = new HashSet<Guid>(distinctShiftIds);

        var result = new List<CoreRestrictedTimeWindow>(rules.Count);
        foreach (var rule in rules)
        {
            IReadOnlySet<Guid> restricted = string.IsNullOrEmpty(rule.AppliesToGroupTag)
                ? allShiftIds
                : shiftIdsByTag.TryGetValue(rule.AppliesToGroupTag, out var set) ? set : EmptySet;

            if (restricted.Count == 0)
            {
                continue;
            }

            result.Add(new CoreRestrictedTimeWindow(
                rule.SeasonFromMonth,
                rule.SeasonFromDay,
                rule.SeasonToMonth,
                rule.SeasonToDay,
                ToMinutes(rule.DailyStart),
                ToMinutes(rule.DailyEnd),
                restricted));
        }

        return result;
    }

    private async Task<Dictionary<string, HashSet<Guid>>> LoadShiftIdsByTagAsync(
        IReadOnlyList<Guid> shiftIds,
        DateOnly from,
        DateOnly until,
        CancellationToken ct)
    {
        var byTag = new Dictionary<string, HashSet<Guid>>(StringComparer.OrdinalIgnoreCase);
        if (shiftIds.Count == 0)
        {
            return byTag;
        }

        var fromDate = from.ToDateTime(TimeOnly.MinValue);
        var untilDate = until.ToDateTime(TimeOnly.MinValue);

        var links = await _context.GroupItem
            .AsNoTracking()
            .Where(gi => gi.ShiftId != null
                && shiftIds.Contains(gi.ShiftId.Value)
                && !gi.IsDeleted
                && (gi.ValidFrom == null || gi.ValidFrom <= untilDate)
                && (gi.ValidUntil == null || gi.ValidUntil >= fromDate))
            .Join(
                _context.Group.AsNoTracking().Where(g => !g.IsDeleted),
                gi => gi.GroupId,
                g => g.Id,
                (gi, g) => new ShiftTagLink(gi.ShiftId!.Value, g.Name))
            .ToListAsync(ct);

        foreach (var link in links)
        {
            if (string.IsNullOrEmpty(link.Name))
            {
                continue;
            }

            if (!byTag.TryGetValue(link.Name, out var set))
            {
                set = [];
                byTag[link.Name] = set;
            }

            set.Add(link.ShiftId);
        }

        return byTag;
    }

    private static readonly IReadOnlySet<Guid> EmptySet = new HashSet<Guid>();

    private static int ToMinutes(TimeOnly time) => (int)time.ToTimeSpan().TotalMinutes;
}
