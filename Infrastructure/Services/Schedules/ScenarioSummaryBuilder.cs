// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Default implementation of <see cref="IScenarioSummaryBuilder"/>. Demand is derived like the wizard derives it: every
/// planned, non-sporadic shift of the scenario needs Quantity x SumEmployees employees on each weekday it is active.
/// Filled slots are the scenario works per (shift, day). Each open slot is classified from the contracts of the
/// employees in the scenario group scope; a scenario without a group falls back to the employees who work in it.
/// </summary>
/// <param name="context">EF Core database context</param>
/// <param name="scenarioRepository">Resolves the scenario by its token</param>
/// <param name="scenarioService">Resolves the group hierarchy of the scenario group</param>
/// <param name="contractProvider">Effective contract data of the employees per day</param>

using Klacks.Api.Application.DTOs.Schedules.Summary;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Application.Services.Schedules;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Services.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Klacks.ScheduleOptimizer.TokenEvolution.Initialization;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.Schedules;

public sealed class ScenarioSummaryBuilder : IScenarioSummaryBuilder
{
    private readonly DataBaseContext _context;
    private readonly IAnalyseScenarioRepository _scenarioRepository;
    private readonly IAnalyseScenarioService _scenarioService;
    private readonly IClientContractDataProvider _contractProvider;

    public ScenarioSummaryBuilder(
        DataBaseContext context,
        IAnalyseScenarioRepository scenarioRepository,
        IAnalyseScenarioService scenarioService,
        IClientContractDataProvider contractProvider)
    {
        _context = context;
        _scenarioRepository = scenarioRepository;
        _scenarioService = scenarioService;
        _contractProvider = contractProvider;
    }

    public async Task<ScenarioSummaryDto?> BuildAsync(Guid token, CancellationToken ct)
    {
        var scenario = await _scenarioRepository.GetByTokenAsync(token, ct);
        if (scenario is null)
        {
            return null;
        }

        var from = scenario.FromDate;
        var until = scenario.UntilDate;

        var shifts = await LoadPlannedShiftsAsync(token, from, until, ct);
        var workCounts = await LoadWorkCountsAsync(token, from, until, ct);

        var coverage = new List<ScenarioShiftCoverageDto>();
        var openSlots = new List<(Shift Shift, DateOnly Date, int Open)>();
        foreach (var shift in shifts)
        {
            var demanded = 0;
            var filled = 0;
            var perDay = ShiftStaffingDemand.PerDay(shift.Quantity, shift.SumEmployees);
            foreach (var date in ActiveDays(shift, from, until))
            {
                var covered = Math.Min(workCounts.GetValueOrDefault((shift.Id, date)), perDay);
                demanded += perDay;
                filled += covered;
                if (covered < perDay)
                {
                    openSlots.Add((shift, date, perDay - covered));
                }
            }

            coverage.Add(new ScenarioShiftCoverageDto(
                shift.Id, ShiftDisplayName(shift), shift.Abbreviation, demanded, filled));
        }

        var agentIds = await LoadAgentIdsAsync(scenario.GroupId, token, from, until, ct);
        var reasons = await ClassifyAsync(openSlots, agentIds, from, until);

        return new ScenarioSummaryDto(
            token,
            from,
            until,
            agentIds.Count,
            workCounts.Values.Sum(),
            coverage.Sum(c => c.DemandedSlots),
            coverage.Sum(c => c.FilledSlots),
            coverage.OrderBy(c => c.ShiftName, StringComparer.CurrentCulture).ToList(),
            reasons);
    }

    private async Task<List<Shift>> LoadPlannedShiftsAsync(Guid token, DateOnly from, DateOnly until, CancellationToken ct)
        => await _context.Shift.IgnoreQueryFilters()
            .AsNoTracking()
            .Where(s => s.AnalyseToken == token
                && !s.IsDeleted
                && !s.IsSporadic
                && s.ShiftType == ShiftType.IsTask
                && (s.Status == ShiftStatus.OriginalShift || s.Status == ShiftStatus.SplitShift)
                && s.FromDate <= until
                && (s.UntilDate == null || s.UntilDate >= from))
            .ToListAsync(ct);

    private async Task<Dictionary<(Guid ShiftId, DateOnly Date), int>> LoadWorkCountsAsync(
        Guid token, DateOnly from, DateOnly until, CancellationToken ct)
    {
        var works = await _context.Work.IgnoreQueryFilters()
            .AsNoTracking()
            .Where(w => w.AnalyseToken == token
                && !w.IsDeleted
                && w.ParentWorkId == null
                && w.CurrentDate >= from
                && w.CurrentDate <= until)
            .Select(w => new { w.ShiftId, w.CurrentDate })
            .ToListAsync(ct);

        return works
            .GroupBy(w => (w.ShiftId, w.CurrentDate))
            .ToDictionary(g => g.Key, g => g.Count());
    }

    private async Task<List<Guid>> LoadAgentIdsAsync(
        Guid? groupId, Guid token, DateOnly from, DateOnly until, CancellationToken ct)
    {
        if (groupId.HasValue)
        {
            var groupIds = await _scenarioService.GetGroupHierarchyIdsAsync(groupId.Value, ct);
            return await _context.Set<GroupItem>()
                .AsNoTracking()
                .Where(gi => !gi.IsDeleted
                    && (gi.AnalyseToken == null || gi.AnalyseToken == token)
                    && groupIds.Contains(gi.GroupId)
                    && gi.ClientId != null)
                .Select(gi => gi.ClientId!.Value)
                .Distinct()
                .ToListAsync(ct);
        }

        return await _context.Work.IgnoreQueryFilters()
            .AsNoTracking()
            .Where(w => w.AnalyseToken == token
                && !w.IsDeleted
                && w.CurrentDate >= from
                && w.CurrentDate <= until)
            .Select(w => w.ClientId)
            .Distinct()
            .ToListAsync(ct);
    }

    private async Task<IReadOnlyList<ScenarioOpenSlotReasonDto>> ClassifyAsync(
        List<(Shift Shift, DateOnly Date, int Open)> openSlots,
        List<Guid> agentIds,
        DateOnly from,
        DateOnly until)
    {
        if (openSlots.Count == 0)
        {
            return [];
        }

        var contractsByDate = agentIds.Count == 0
            ? new Dictionary<DateOnly, Dictionary<Guid, EffectiveContractData>>()
            : await _contractProvider.GetEffectiveContractDataForClientsRangeAsync(agentIds, from, until);

        var countByReason = new Dictionary<string, int>();
        var shiftNamesByReason = new Dictionary<string, SortedSet<string>>();
        foreach (var (shift, date, open) in openSlots)
        {
            IReadOnlyCollection<EffectiveContractData> contracts = contractsByDate.TryGetValue(date, out var perDay)
                ? perDay.Values
                : [];
            var isEarly = ShiftTypeInference.FromSpan(shift.StartShift, shift.EndShift) == ShiftTypeInference.EarlyIndex;
            var reason = OpenSlotReasonClassifier.Classify(contracts, date.DayOfWeek, isEarly);

            countByReason[reason] = countByReason.GetValueOrDefault(reason) + open;
            if (!shiftNamesByReason.TryGetValue(reason, out var names))
            {
                names = new SortedSet<string>(StringComparer.CurrentCulture);
                shiftNamesByReason[reason] = names;
            }

            names.Add(ShiftDisplayName(shift));
        }

        return countByReason
            .Select(kv => new ScenarioOpenSlotReasonDto(kv.Key, kv.Value, shiftNamesByReason[kv.Key].ToList()))
            .OrderByDescending(r => r.SlotCount)
            .ToList();
    }

    private static IEnumerable<DateOnly> ActiveDays(Shift shift, DateOnly from, DateOnly until)
    {
        var first = shift.FromDate > from ? shift.FromDate : from;
        var last = shift.UntilDate is { } shiftUntil && shiftUntil < until ? shiftUntil : until;
        for (var date = first; date <= last; date = date.AddDays(1))
        {
            if (IsActiveOn(shift, date.DayOfWeek))
            {
                yield return date;
            }
        }
    }

    private static bool IsActiveOn(Shift shift, DayOfWeek day) => day switch
    {
        DayOfWeek.Monday => shift.IsMonday,
        DayOfWeek.Tuesday => shift.IsTuesday,
        DayOfWeek.Wednesday => shift.IsWednesday,
        DayOfWeek.Thursday => shift.IsThursday,
        DayOfWeek.Friday => shift.IsFriday,
        DayOfWeek.Saturday => shift.IsSaturday,
        DayOfWeek.Sunday => shift.IsSunday,
        _ => false,
    };

    private static string ShiftDisplayName(Shift shift)
        => string.IsNullOrWhiteSpace(shift.Name) ? shift.Abbreviation : shift.Name;
}
