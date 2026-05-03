// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Services.Schedules;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Infrastructure.Persistence;
using Klacks.ScheduleOptimizer.Harmonizer.Bitmap;
using Klacks.ScheduleOptimizer.TokenEvolution.Initialization;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.Schedules;

/// <summary>
/// Loads the saved schedule plus everything the domain-aware validator needs: per-agent
/// contract caps, per-(agent, date) availability (WorksOnDay flag, FREE keywords, break
/// blockers), and ClientShiftPreference Blacklist sets. Uses the same data sources as
/// Wizard 1 and the same scenario-isolation semantics for the AnalyseToken.
/// </summary>
/// <param name="context">EF Core database context</param>
/// <param name="contractProvider">Source of effective contract data per client/date</param>
public sealed class HarmonizerContextBuilder : IHarmonizerContextBuilder
{
    private readonly DataBaseContext _context;
    private readonly IClientContractDataProvider _contractProvider;
    private readonly IWorkSofteningRepository _softeningRepository;

    public HarmonizerContextBuilder(
        DataBaseContext context,
        IClientContractDataProvider contractProvider,
        IWorkSofteningRepository softeningRepository)
    {
        _context = context;
        _contractProvider = contractProvider;
        _softeningRepository = softeningRepository;
    }

    public async Task<BitmapInput> BuildContextAsync(HarmonizerContextRequest request, CancellationToken ct)
    {
        var agentIds = request.AgentIds.ToList();

        var works = await LoadWorksAsync(agentIds, request.PeriodFrom, request.PeriodUntil, request.AnalyseToken, ct);
        var clients = await LoadClientsAsync(agentIds, ct);
        var firstDayContracts = await _contractProvider.GetEffectiveContractDataForClientsAsync(agentIds, request.PeriodFrom);
        var preferredSymbols = await LoadPreferredSymbolsAsync(agentIds, ct);
        var blacklistByAgent = await LoadBlacklistByAgentAsync(agentIds, ct);
        var freeCommandDates = await LoadFreeCommandDatesAsync(agentIds, request.PeriodFrom, request.PeriodUntil, request.AnalyseToken, ct);
        var breakDates = await LoadBreakDatesAsync(agentIds, request.PeriodFrom, request.PeriodUntil, request.AnalyseToken, ct);
        var contractDays = await LoadContractDaysAsync(agentIds, request.PeriodFrom, request.PeriodUntil, ct);
        var softenings = await _softeningRepository.LoadAsync(agentIds, request.PeriodFrom, request.PeriodUntil, request.AnalyseToken, ct);

        var agents = BuildAgents(agentIds, firstDayContracts, clients, preferredSymbols, blacklistByAgent);
        var availability = BuildAvailability(agentIds, request.PeriodFrom, request.PeriodUntil, contractDays, freeCommandDates, breakDates);
        var assignments = BuildAssignments(works);
        var hints = BuildSofteningHints(softenings);

        return new BitmapInput(agents, request.PeriodFrom, request.PeriodUntil, assignments, hints, availability);
    }

    private static IReadOnlyList<SofteningHint> BuildSofteningHints(IReadOnlyList<WorkSoftening> softenings)
    {
        return softenings
            .GroupBy(s => (s.ClientId, s.CurrentDate, s.Kind))
            .Select(g => new SofteningHint(
                AgentId: g.Key.ClientId.ToString(),
                Date: g.Key.CurrentDate,
                Kind: g.Key.Kind,
                RuleNames: g.Select(s => s.RuleName).Distinct().ToList()))
            .ToList();
    }

    private async Task<List<Work>> LoadWorksAsync(
        List<Guid> agentIds,
        DateOnly from,
        DateOnly until,
        Guid? analyseToken,
        CancellationToken ct)
    {
        return await _context.Work
            .AsNoTracking()
            .Where(w => agentIds.Contains(w.ClientId)
                        && w.CurrentDate >= from
                        && w.CurrentDate <= until
                        && (w.AnalyseToken == analyseToken || (w.AnalyseToken == null && analyseToken == null)))
            .ToListAsync(ct);
    }

    private async Task<Dictionary<Guid, Client>> LoadClientsAsync(List<Guid> agentIds, CancellationToken ct)
    {
        var clients = await _context.Client
            .AsNoTracking()
            .Where(c => agentIds.Contains(c.Id))
            .ToListAsync(ct);
        return clients.ToDictionary(c => c.Id);
    }

    private async Task<Dictionary<Guid, HashSet<CellSymbol>>> LoadPreferredSymbolsAsync(
        List<Guid> agentIds,
        CancellationToken ct)
    {
        var preferredPairs = await _context.ClientShiftPreference
            .AsNoTracking()
            .Where(p => agentIds.Contains(p.ClientId) && p.PreferenceType == ShiftPreferenceType.Preferred)
            .Join(
                _context.Shift.AsNoTracking(),
                p => p.ShiftId,
                s => s.Id,
                (p, s) => new { p.ClientId, StartTime = s.StartShift })
            .ToListAsync(ct);

        var result = new Dictionary<Guid, HashSet<CellSymbol>>();
        foreach (var pair in preferredPairs)
        {
            if (!result.TryGetValue(pair.ClientId, out var set))
            {
                set = [];
                result[pair.ClientId] = set;
            }
            set.Add(SymbolFromStartTime(pair.StartTime));
        }
        return result;
    }

    private async Task<Dictionary<Guid, HashSet<Guid>>> LoadBlacklistByAgentAsync(
        List<Guid> agentIds,
        CancellationToken ct)
    {
        var blacklisted = await _context.ClientShiftPreference
            .AsNoTracking()
            .Where(p => agentIds.Contains(p.ClientId) && p.PreferenceType == ShiftPreferenceType.Blacklist)
            .Select(p => new { p.ClientId, p.ShiftId })
            .ToListAsync(ct);

        var result = new Dictionary<Guid, HashSet<Guid>>();
        foreach (var entry in blacklisted)
        {
            if (!result.TryGetValue(entry.ClientId, out var set))
            {
                set = [];
                result[entry.ClientId] = set;
            }
            set.Add(entry.ShiftId);
        }
        return result;
    }

    private async Task<HashSet<(Guid AgentId, DateOnly Date)>> LoadFreeCommandDatesAsync(
        List<Guid> agentIds,
        DateOnly from,
        DateOnly until,
        Guid? analyseToken,
        CancellationToken ct)
    {
        var rawCommands = await _context.ScheduleCommands
            .AsNoTracking()
            .Where(c => agentIds.Contains(c.ClientId)
                        && c.CurrentDate >= from
                        && c.CurrentDate <= until
                        && (c.AnalyseToken == analyseToken || (c.AnalyseToken == null && analyseToken == null)))
            .Select(c => new { c.ClientId, c.CurrentDate, c.CommandKeyword })
            .ToListAsync(ct);

        var result = new HashSet<(Guid, DateOnly)>();
        foreach (var cmd in rawCommands)
        {
            if (ScheduleCommandKeywordMapper.TryMap(cmd.CommandKeyword, out var keyword)
                && keyword == ScheduleOptimizer.Models.ScheduleCommandKeyword.Free)
            {
                result.Add((cmd.ClientId, cmd.CurrentDate));
            }
        }
        return result;
    }

    private async Task<HashSet<(Guid AgentId, DateOnly Date)>> LoadBreakDatesAsync(
        List<Guid> agentIds,
        DateOnly from,
        DateOnly until,
        Guid? analyseToken,
        CancellationToken ct)
    {
        var rawBreaks = await _context.Break
            .AsNoTracking()
            .Where(b => agentIds.Contains(b.ClientId)
                        && b.CurrentDate >= from
                        && b.CurrentDate <= until
                        && (b.AnalyseToken == analyseToken || (b.AnalyseToken == null && analyseToken == null)))
            .Select(b => new { b.ClientId, b.CurrentDate })
            .ToListAsync(ct);

        var result = new HashSet<(Guid, DateOnly)>();
        foreach (var br in rawBreaks)
        {
            result.Add((br.ClientId, br.CurrentDate));
        }
        return result;
    }

    private async Task<Dictionary<(Guid AgentId, DateOnly Date), bool>> LoadContractDaysAsync(
        List<Guid> agentIds,
        DateOnly from,
        DateOnly until,
        CancellationToken ct)
    {
        var result = new Dictionary<(Guid, DateOnly), bool>();
        for (var date = from; date <= until; date = date.AddDays(1))
        {
            ct.ThrowIfCancellationRequested();
            var perDay = await _contractProvider.GetEffectiveContractDataForClientsAsync(agentIds, date);
            foreach (var agentId in agentIds)
            {
                if (!perDay.TryGetValue(agentId, out var data))
                {
                    result[(agentId, date)] = false;
                    continue;
                }
                result[(agentId, date)] = WorksOnDay(data, date.DayOfWeek);
            }
        }
        return result;
    }

    private static bool WorksOnDay(EffectiveContractData data, DayOfWeek dayOfWeek) => dayOfWeek switch
    {
        DayOfWeek.Monday => data.WorkOnMonday,
        DayOfWeek.Tuesday => data.WorkOnTuesday,
        DayOfWeek.Wednesday => data.WorkOnWednesday,
        DayOfWeek.Thursday => data.WorkOnThursday,
        DayOfWeek.Friday => data.WorkOnFriday,
        DayOfWeek.Saturday => data.WorkOnSaturday,
        DayOfWeek.Sunday => data.WorkOnSunday,
        _ => false,
    };

    private static List<BitmapAgent> BuildAgents(
        List<Guid> agentIds,
        IReadOnlyDictionary<Guid, EffectiveContractData> contracts,
        IReadOnlyDictionary<Guid, Client> clients,
        IReadOnlyDictionary<Guid, HashSet<CellSymbol>> preferences,
        IReadOnlyDictionary<Guid, HashSet<Guid>> blacklists)
    {
        var result = new List<BitmapAgent>(agentIds.Count);
        foreach (var id in agentIds)
        {
            var displayName = clients.TryGetValue(id, out var c) ? BuildDisplayName(c) : id.ToString();
            var contract = contracts.TryGetValue(id, out var ct) ? ct : null;
            var targetHours = contract?.GuaranteedHours ?? 0m;
            var maxWeekly = contract?.MaxWeeklyHours ?? 0m;
            var maxConsec = contract?.MaxConsecutiveDays ?? 0;
            var minPause = contract?.MinPauseHours ?? 0m;
            var prefs = preferences.TryGetValue(id, out var p) ? p : [];
            var blacklist = blacklists.TryGetValue(id, out var b) ? (IReadOnlySet<Guid>)b : null;
            result.Add(new BitmapAgent(
                Id: id.ToString(),
                DisplayName: displayName,
                TargetHours: targetHours,
                PreferredShiftSymbols: prefs,
                MaxWeeklyHours: maxWeekly,
                MaxConsecutiveDays: maxConsec,
                MinPauseHours: minPause,
                BlacklistedShiftIds: blacklist));
        }
        return result;
    }

    private static Dictionary<(string AgentId, DateOnly Date), DayAvailability> BuildAvailability(
        List<Guid> agentIds,
        DateOnly from,
        DateOnly until,
        IReadOnlyDictionary<(Guid AgentId, DateOnly Date), bool> contractDays,
        IReadOnlySet<(Guid AgentId, DateOnly Date)> freeCommandDates,
        IReadOnlySet<(Guid AgentId, DateOnly Date)> breakDates)
    {
        var result = new Dictionary<(string, DateOnly), DayAvailability>();
        foreach (var agentId in agentIds)
        {
            for (var date = from; date <= until; date = date.AddDays(1))
            {
                var worksOnDay = contractDays.TryGetValue((agentId, date), out var w) && w;
                var hasFree = freeCommandDates.Contains((agentId, date));
                var hasBreak = breakDates.Contains((agentId, date));
                result[(agentId.ToString(), date)] = new DayAvailability(worksOnDay, hasFree, hasBreak);
            }
        }
        return result;
    }

    private static string BuildDisplayName(Client client)
    {
        var first = client.FirstName ?? string.Empty;
        var last = client.Name ?? string.Empty;
        var combined = $"{last}, {first}".Trim().TrimEnd(',', ' ');
        return string.IsNullOrEmpty(combined) ? client.Id.ToString() : combined;
    }

    private static List<BitmapAssignment> BuildAssignments(IReadOnlyList<Work> works)
    {
        return works
            .Select(w =>
            {
                var startAt = w.CurrentDate.ToDateTime(w.StartTime);
                var endAt = w.EndTime <= w.StartTime
                    ? w.CurrentDate.AddDays(1).ToDateTime(w.EndTime)
                    : w.CurrentDate.ToDateTime(w.EndTime);
                return new BitmapAssignment(
                    AgentId: w.ClientId.ToString(),
                    Date: w.CurrentDate,
                    Symbol: SymbolFromStartTime(w.StartTime),
                    ShiftRefId: w.ShiftId,
                    WorkIds: [w.Id],
                    IsLocked: w.LockLevel != WorkLockLevel.None,
                    StartAt: startAt,
                    EndAt: endAt,
                    Hours: w.WorkTime);
            })
            .ToList();
    }

    private static CellSymbol SymbolFromStartTime(TimeOnly start)
    {
        var typeIndex = ShiftTypeInference.FromStartTime(start);
        return typeIndex switch
        {
            0 => CellSymbol.Early,
            1 => CellSymbol.Late,
            2 => CellSymbol.Night,
            _ => CellSymbol.Other,
        };
    }
}
