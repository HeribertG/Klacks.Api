// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Row filters of a scenario clone (scenario from scenario). Without a source token every filter is the plain real-plan
/// filter (AnalyseToken == null), unchanged from the real-plan clone. With one, dated rows come from the source scenario
/// inside its own range and from the real plan outside it (WhereFromSource), rows reached through an already chosen
/// parent are read from either world (WhereEitherWorld), and employee-scoped rows of the real plan are limited to the
/// real group members while the source scenario's own rows may also belong to its borrowed employees
/// (WhereClientInScope) - accept only replaces real rows of real members, so a borrowed employee's real rows must never
/// be copied.
/// </summary>

using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Schedules;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.AnalyseScenarios;

public partial class AnalyseScenarioService
{
    internal static IQueryable<Work> WhereFromSource(IQueryable<Work> query, ScenarioCloneSource source)
    {
        if (source.Token is not { } token)
        {
            return query.Where(w => w.AnalyseToken == null);
        }

        var from = source.From;
        var until = source.Until;
        return query.Where(w => (w.AnalyseToken == token && w.CurrentDate >= from && w.CurrentDate <= until)
            || (w.AnalyseToken == null && (w.CurrentDate < from || w.CurrentDate > until)));
    }

    internal static IQueryable<Break> WhereFromSource(IQueryable<Break> query, ScenarioCloneSource source)
    {
        if (source.Token is not { } token)
        {
            return query.Where(b => b.AnalyseToken == null);
        }

        var from = source.From;
        var until = source.Until;
        return query.Where(b => (b.AnalyseToken == token && b.CurrentDate >= from && b.CurrentDate <= until)
            || (b.AnalyseToken == null && (b.CurrentDate < from || b.CurrentDate > until)));
    }

    internal static IQueryable<ScheduleNote> WhereFromSource(IQueryable<ScheduleNote> query, ScenarioCloneSource source)
    {
        if (source.Token is not { } token)
        {
            return query.Where(sn => sn.AnalyseToken == null);
        }

        var from = source.From;
        var until = source.Until;
        return query.Where(sn => (sn.AnalyseToken == token && sn.CurrentDate >= from && sn.CurrentDate <= until)
            || (sn.AnalyseToken == null && (sn.CurrentDate < from || sn.CurrentDate > until)));
    }

    internal static IQueryable<ScheduleCommand> WhereFromSource(IQueryable<ScheduleCommand> query, ScenarioCloneSource source)
    {
        if (source.Token is not { } token)
        {
            return query.Where(sc => sc.AnalyseToken == null);
        }

        var from = source.From;
        var until = source.Until;
        return query.Where(sc => (sc.AnalyseToken == token && sc.CurrentDate >= from && sc.CurrentDate <= until)
            || (sc.AnalyseToken == null && (sc.CurrentDate < from || sc.CurrentDate > until)));
    }

    internal static IQueryable<Work> WhereEitherWorld(IQueryable<Work> query, ScenarioCloneSource source)
        => source.Token is { } token
            ? query.Where(w => w.AnalyseToken == null || w.AnalyseToken == token)
            : query.Where(w => w.AnalyseToken == null);

    internal static IQueryable<Break> WhereEitherWorld(IQueryable<Break> query, ScenarioCloneSource source)
        => source.Token is { } token
            ? query.Where(b => b.AnalyseToken == null || b.AnalyseToken == token)
            : query.Where(b => b.AnalyseToken == null);

    internal static IQueryable<WorkChange> WhereEitherWorld(IQueryable<WorkChange> query, ScenarioCloneSource source)
        => source.Token is { } token
            ? query.Where(wc => wc.AnalyseToken == null || wc.AnalyseToken == token)
            : query.Where(wc => wc.AnalyseToken == null);

    internal static IQueryable<Expenses> WhereEitherWorld(IQueryable<Expenses> query, ScenarioCloneSource source)
        => source.Token is { } token
            ? query.Where(e => e.AnalyseToken == null || e.AnalyseToken == token)
            : query.Where(e => e.AnalyseToken == null);

    internal static IQueryable<ClientShiftPreference> WhereEitherWorld(IQueryable<ClientShiftPreference> query, ScenarioCloneSource source)
        => source.Token is { } token
            ? query.Where(p => p.AnalyseToken == null || p.AnalyseToken == token)
            : query.Where(p => p.AnalyseToken == null);

    internal static IQueryable<ShiftExpenses> WhereEitherWorld(IQueryable<ShiftExpenses> query, ScenarioCloneSource source)
        => source.Token is { } token
            ? query.Where(e => e.AnalyseToken == null || e.AnalyseToken == token)
            : query.Where(e => e.AnalyseToken == null);

    internal static IQueryable<Shift> WhereEitherWorld(IQueryable<Shift> query, ScenarioCloneSource source)
        => source.Token is { } token
            ? query.Where(s => s.AnalyseToken == null || s.AnalyseToken == token)
            : query.Where(s => s.AnalyseToken == null);

    internal static IQueryable<GroupItem> WhereEitherWorld(IQueryable<GroupItem> query, ScenarioCloneSource source)
        => source.Token is { } token
            ? query.Where(gi => gi.AnalyseToken == null || gi.AnalyseToken == token)
            : query.Where(gi => gi.AnalyseToken == null);

    internal static IQueryable<Break> WhereClientInScope(
        IQueryable<Break> query, (List<Guid> Real, List<Guid> WithSource) clients, ScenarioCloneSource source)
    {
        var real = clients.Real;
        if (source.Token is null)
        {
            return query.Where(b => real.Contains(b.ClientId));
        }

        var withSource = clients.WithSource;
        return query.Where(b => (b.AnalyseToken == null && real.Contains(b.ClientId))
            || (b.AnalyseToken != null && withSource.Contains(b.ClientId)));
    }

    internal static IQueryable<ScheduleNote> WhereClientInScope(
        IQueryable<ScheduleNote> query, (List<Guid> Real, List<Guid> WithSource) clients, ScenarioCloneSource source)
    {
        var real = clients.Real;
        if (source.Token is null)
        {
            return query.Where(sn => real.Contains(sn.ClientId));
        }

        var withSource = clients.WithSource;
        return query.Where(sn => (sn.AnalyseToken == null && real.Contains(sn.ClientId))
            || (sn.AnalyseToken != null && withSource.Contains(sn.ClientId)));
    }

    internal static IQueryable<ScheduleCommand> WhereClientInScope(
        IQueryable<ScheduleCommand> query, (List<Guid> Real, List<Guid> WithSource) clients, ScenarioCloneSource source)
    {
        var real = clients.Real;
        if (source.Token is null)
        {
            return query.Where(sc => real.Contains(sc.ClientId));
        }

        var withSource = clients.WithSource;
        return query.Where(sc => (sc.AnalyseToken == null && real.Contains(sc.ClientId))
            || (sc.AnalyseToken != null && withSource.Contains(sc.ClientId)));
    }

    /// <summary>
    /// Shifts of the group hierarchy: the real memberships, plus with a source scenario the memberships of its clone
    /// shifts (a source clone stands in for its real root, see LoadShiftsToCloneAsync).
    /// </summary>
    private async Task<List<Guid>> LoadGroupShiftIdsAsync(List<Guid> groupIds, ScenarioCloneSource source, CancellationToken ct)
        => await WhereEitherWorld(_context.Set<GroupItem>()
                .Where(gi => !gi.IsDeleted && groupIds.Contains(gi.GroupId) && gi.ShiftId != null), source)
            .Select(gi => gi.ShiftId!.Value)
            .Distinct()
            .ToListAsync(ct);

    /// <summary>
    /// Employees of the group hierarchy: Real = the real members (the only employees whose real rows accept replaces);
    /// WithSource = additionally, with a source scenario, its temporary cross-group members (borrowed employees), whose
    /// rows OF THE SOURCE SCENARIO belong to the clone. Without a source both lists are the real members.
    /// </summary>
    private async Task<(List<Guid> Real, List<Guid> WithSource)> LoadGroupClientScopeAsync(
        List<Guid> groupIds, ScenarioCloneSource source, CancellationToken ct)
    {
        var memberships = await WhereEitherWorld(_context.Set<GroupItem>()
                .Where(gi => !gi.IsDeleted && groupIds.Contains(gi.GroupId) && gi.ClientId != null), source)
            .Select(gi => new { ClientId = gi.ClientId!.Value, gi.AnalyseToken })
            .ToListAsync(ct);

        var real = memberships.Where(m => m.AnalyseToken == null).Select(m => m.ClientId).Distinct().ToList();
        var withSource = memberships.Select(m => m.ClientId).Distinct().ToList();
        return (real, withSource);
    }
}
