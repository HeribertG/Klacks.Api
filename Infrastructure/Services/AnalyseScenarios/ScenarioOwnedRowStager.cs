// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Scenario-owned rows that are not cloned from the real plan: temporary cross-group memberships and
/// scenario-scoped planning constraints. A temporary membership is one row per day (ValidFrom equals ValidUntil,
/// IsTemporary set), so a person borrowed on two separate days is a member on those two days only. A row over several
/// days, written before memberships were kept per day, is still read and is cut into day rows where a day is removed or
/// only partly taken over. Accepting the scenario turns the day rows into temporary memberships of the real plan.
/// Planning constraints of a scenario are soft-deleted on reject/delete
/// AND on accept - accepting a scenario adopts its plan, never its what-if rules: promoting them to real rules
/// would approve a rule outside the UI pending list (owner decision 2026-10-03).
/// </summary>
/// <param name="context">Database context shared with the other scenario collaborators of the scope</param>

using System.Linq.Expressions;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Services.Groups;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.AnalyseScenarios;

public sealed class ScenarioOwnedRowStager
{
    private readonly DataBaseContext _context;

    public ScenarioOwnedRowStager(DataBaseContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Stages one membership row per day of the window. A day that a membership of the client in that group under the
    /// token already covers, saved or still unsaved, gets no second row.
    /// </summary>
    public async Task AddScenarioMembershipAsync(
        Guid token, Guid clientId, Guid groupId, DateOnly validFrom, DateOnly validUntil, CancellationToken cancellationToken)
    {
        var existing = await LoadLiveMembershipsAsync(
            token, clientId, groupId, ToUtcDay(validFrom), ToUtcDay(validUntil), cancellationToken);

        for (var date = validFrom; date <= validUntil; date = date.AddDays(1))
        {
            var day = ToUtcDay(date);
            if (existing.Any(membership => Covers(membership.ValidFrom, membership.ValidUntil, day)))
            {
                continue;
            }

            await _context.Set<GroupItem>().AddAsync(NewDayRow(clientId, groupId, day, token), cancellationToken);
        }
    }

    /// <summary>
    /// Stages the removal of the client's memberships of that day under the token, in every group. A row of that single
    /// day is deleted; a row over several days keeps its other days: it shrinks to the first of them and the rest
    /// become day rows.
    /// </summary>
    public async Task RemoveScenarioMembershipsAsync(
        Guid token, Guid clientId, DateOnly date, CancellationToken cancellationToken)
    {
        var day = ToUtcDay(date);
        var memberships = await LoadLiveMembershipsAsync(token, clientId, null, day, day, cancellationToken);

        foreach (var membership in memberships)
        {
            var remainingDays = DaysOf(membership).Where(other => other != day).ToList();
            if (remainingDays.Count == 0)
            {
                _context.Set<GroupItem>().Remove(membership);
                continue;
            }

            await KeepOnlyDaysAsync(membership, remainingDays, token, cancellationToken);
        }
    }

    /// <summary>
    /// Accept step for the group items of a scenario: the links of its clone shifts are discarded with those shifts, its
    /// client memberships become temporary memberships of the real plan. A day on which the client already has a
    /// membership of the real plan in that group, regular or temporary, is not taken over a second time: a membership
    /// whose days are all covered is discarded, one that is partly covered keeps its uncovered days as day rows. A
    /// membership without a first or a last day is no day row: it becomes a regular membership, as it did before
    /// memberships were kept per day, unless the client already has a regular membership in that group.
    /// </summary>
    /// <param name="token">Token of the accepted scenario</param>
    /// <param name="nowUtc">Deletion stamp of the discarded rows</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task PromoteScenarioMembershipsAsync(Guid token, DateTime nowUtc, CancellationToken cancellationToken)
    {
        var scenarioItems = await _context.Set<GroupItem>().IgnoreQueryFilters()
            .Where(gi => gi.AnalyseToken == token && !gi.IsDeleted)
            .OrderBy(gi => gi.ValidFrom)
            .ThenBy(gi => gi.Id)
            .ToListAsync(cancellationToken);

        foreach (var shiftLink in scenarioItems.Where(gi => gi.ShiftId != null))
        {
            Discard(shiftLink, nowUtc);
        }

        var memberships = scenarioItems.Where(gi => gi.ShiftId == null).ToList();
        var realWindows = await LoadRealWindowsAsync(memberships, cancellationToken);

        foreach (var membership in memberships)
        {
            if (membership.ValidFrom is null || membership.ValidUntil is null)
            {
                PromoteAsRegular(membership, realWindows, nowUtc);
                continue;
            }

            var days = DaysOf(membership);
            var uncoveredDays = days
                .Where(day => !realWindows.Any(real => real.ClientId == membership.ClientId
                    && real.GroupId == membership.GroupId
                    && Covers(real.ValidFrom, real.ValidUntil, day)))
                .ToList();
            if (uncoveredDays.Count == 0)
            {
                Discard(membership, nowUtc);
                continue;
            }

            membership.AnalyseToken = null;
            membership.IsTemporary = true;
            if (uncoveredDays.Count < days.Count)
            {
                await KeepOnlyDaysAsync(membership, uncoveredDays, null, cancellationToken);
                realWindows.AddRange(uncoveredDays.Select(day => new RealWindow(membership.ClientId, membership.GroupId, day, day, true)));
            }
            else
            {
                realWindows.Add(new RealWindow(membership.ClientId, membership.GroupId, membership.ValidFrom, membership.ValidUntil, true));
            }
        }
    }

    /// <summary>
    /// A scenario membership with an open side: it turns into the regular membership of the client in that group, or is
    /// discarded when the real plan already holds one (the unique index allows a single regular membership).
    /// </summary>
    private static void PromoteAsRegular(GroupItem membership, List<RealWindow> realWindows, DateTime nowUtc)
    {
        if (realWindows.Any(real => real.ClientId == membership.ClientId && real.GroupId == membership.GroupId && !real.IsTemporary))
        {
            Discard(membership, nowUtc);
            return;
        }

        membership.AnalyseToken = null;
        membership.IsTemporary = false;
        realWindows.Add(new RealWindow(membership.ClientId, membership.GroupId, membership.ValidFrom, membership.ValidUntil, false));
    }

    public async Task SoftDeleteScenarioPlanningConstraintsAsync(Guid token, DateTime nowUtc, CancellationToken ct)
    {
        var constraints = await _context.PlanningConstraint
            .Where(c => c.AnalyseToken == token && !c.IsDeleted)
            .ToListAsync(ct);
        foreach (var constraint in constraints)
        {
            constraint.IsDeleted = true;
            constraint.DeletedTime = nowUtc;
        }
    }

    /// <summary>
    /// The memberships of the client under the token that touch the window, saved ones and those staged in this context,
    /// without the ones staged for deletion.
    /// </summary>
    /// <param name="groupId">Restricts to one group; null reads every group</param>
    private async Task<List<GroupItem>> LoadLiveMembershipsAsync(
        Guid token, Guid clientId, Guid? groupId, DateTime fromDay, DateTime untilDay, CancellationToken cancellationToken)
    {
        Expression<Func<GroupItem, bool>> touchesWindow = gi =>
            !gi.IsDeleted
            && gi.AnalyseToken == token
            && gi.ClientId == clientId
            && gi.ShiftId == null
            && (groupId == null || gi.GroupId == groupId)
            && gi.ValidFrom <= untilDay
            && gi.ValidUntil >= fromDay;

        var saved = await _context.Set<GroupItem>().Where(touchesWindow).ToListAsync(cancellationToken);
        var staged = _context.Set<GroupItem>().Local.Where(touchesWindow.Compile());

        return saved.Union(staged)
            .Where(membership => _context.Entry(membership).State != EntityState.Deleted)
            .ToList();
    }

    /// <summary>
    /// The windows of the real-plan memberships (regular and temporary) of the clients and groups the scenario
    /// memberships name.
    /// </summary>
    private async Task<List<RealWindow>> LoadRealWindowsAsync(
        List<GroupItem> scenarioMemberships, CancellationToken cancellationToken)
    {
        var clientIds = scenarioMemberships.Where(gi => gi.ClientId != null).Select(gi => gi.ClientId!.Value).Distinct().ToList();
        var groupIds = scenarioMemberships.Select(gi => gi.GroupId).Distinct().ToList();
        if (clientIds.Count == 0)
        {
            return [];
        }

        var rows = await _context.Set<GroupItem>()
            .AsNoTracking()
            .Where(gi => gi.ShiftId == null
                && gi.ClientId != null
                && clientIds.Contains(gi.ClientId.Value)
                && groupIds.Contains(gi.GroupId))
            .Where(RegularMembership.OrTemporary)
            .Select(gi => new { gi.ClientId, gi.GroupId, gi.ValidFrom, gi.ValidUntil, gi.IsTemporary })
            .ToListAsync(cancellationToken);

        return rows.Select(row => new RealWindow(row.ClientId, row.GroupId, row.ValidFrom, row.ValidUntil, row.IsTemporary)).ToList();
    }

    /// <summary>
    /// Cuts a membership down to the given days: the row itself becomes the first day, so its key columns never collide
    /// with a row of the same save, and every further day gets a row of its own in the world the membership is in.
    /// </summary>
    /// <param name="days">Days to keep, ascending; at least one</param>
    /// <param name="token">Scenario token of the new day rows; null for the real plan</param>
    private async Task KeepOnlyDaysAsync(GroupItem membership, List<DateTime> days, Guid? token, CancellationToken cancellationToken)
    {
        membership.ValidFrom = days[0];
        membership.ValidUntil = days[0];
        membership.IsTemporary = true;

        foreach (var day in days.Skip(1))
        {
            await _context.Set<GroupItem>().AddAsync(
                NewDayRow(membership.ClientId, membership.GroupId, day, token), cancellationToken);
        }
    }

    private static GroupItem NewDayRow(Guid? clientId, Guid groupId, DateTime day, Guid? token) => new()
    {
        Id = Guid.NewGuid(),
        ClientId = clientId,
        GroupId = groupId,
        ShiftId = null,
        ValidFrom = day,
        ValidUntil = day,
        AnalyseToken = token,
        IsTemporary = true
    };

    /// <returns>The days of the membership window, ascending; empty when a bound is missing</returns>
    private static List<DateTime> DaysOf(GroupItem membership)
    {
        var days = new List<DateTime>();
        if (membership.ValidFrom is not { } from || membership.ValidUntil is not { } until)
        {
            return days;
        }

        for (var day = ToUtcDay(DateOnly.FromDateTime(from)); day <= ToUtcDay(DateOnly.FromDateTime(until)); day = day.AddDays(1))
        {
            days.Add(day);
        }

        return days;
    }

    private static bool Covers(DateTime? validFrom, DateTime? validUntil, DateTime day)
        => (validFrom == null || ToUtcDay(DateOnly.FromDateTime(validFrom.Value)) <= day)
            && (validUntil == null || ToUtcDay(DateOnly.FromDateTime(validUntil.Value)) >= day);

    private static DateTime ToUtcDay(DateOnly date) => date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

    private static void Discard(GroupItem item, DateTime nowUtc)
    {
        item.IsDeleted = true;
        item.DeletedTime = nowUtc;
    }

    private sealed record RealWindow(Guid? ClientId, Guid GroupId, DateTime? ValidFrom, DateTime? ValidUntil, bool IsTemporary);
}
