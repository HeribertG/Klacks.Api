// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Services.Schedules;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Domain.Services.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Klacks.ScheduleOptimizer.Constraints.Rules;
using Klacks.ScheduleOptimizer.Harmonizer.Bitmap;
using Klacks.ScheduleOptimizer.Models;
using Klacks.ScheduleOptimizer.TokenEvolution.Initialization;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.Schedules;

/// <summary>
/// Loads the saved schedule plus everything the domain-aware validator needs: per-agent
/// contract caps, the contractual target prorated to the bitmap date range, per-(agent, date) availability (WorksOnDay flag, FREE keywords, break
/// blockers), and ClientShiftPreference Blacklist sets. Uses the same data sources as
/// Wizard 1 and the same scenario-isolation semantics for the AnalyseToken.
/// </summary>
/// <param name="context">EF Core database context</param>
/// <param name="contractProvider">Source of effective contract data per client/date</param>
/// <param name="ruleSetLoader">Single loader of the approved planning rules; with rules the agents also get the night window
/// and workload the API validators use, and BitmapInput.Rules carries the rules, the carry-in outside the boundary window and
/// the night minimum overlap</param>
public sealed class HarmonizerContextBuilder : IHarmonizerContextBuilder
{
    private readonly DataBaseContext _context;
    private readonly IClientContractDataProvider _contractProvider;
    private readonly IWorkSofteningRepository _softeningRepository;
    private readonly IEligibilityMatrixBuilder _eligibilityMatrixBuilder;
    private readonly IAvailabilityIneligibilityService _availabilityService;
    private readonly IWizardRestrictedWindowBuilder _restrictedWindowBuilder;
    private readonly IScheduleCommandKeywordProvider _keywordProvider;
    private readonly IPlanningRuleSetLoader _ruleSetLoader;

    public HarmonizerContextBuilder(
        DataBaseContext context,
        IClientContractDataProvider contractProvider,
        IWorkSofteningRepository softeningRepository,
        IEligibilityMatrixBuilder eligibilityMatrixBuilder,
        IAvailabilityIneligibilityService availabilityService,
        IWizardRestrictedWindowBuilder restrictedWindowBuilder,
        IScheduleCommandKeywordProvider keywordProvider,
        IPlanningRuleSetLoader ruleSetLoader)
    {
        _context = context;
        _contractProvider = contractProvider;
        _softeningRepository = softeningRepository;
        _eligibilityMatrixBuilder = eligibilityMatrixBuilder;
        _availabilityService = availabilityService;
        _restrictedWindowBuilder = restrictedWindowBuilder;
        _keywordProvider = keywordProvider;
        _ruleSetLoader = ruleSetLoader;
    }

    public async Task<BitmapInput> BuildContextAsync(HarmonizerContextRequest request, CancellationToken ct)
    {
        var agentIds = request.AgentIds.ToList();
        var keywordMap = ScheduleCommandKeywordMapper.BuildMap(await _keywordProvider.GetAsync(ct));

        var works = await LoadWorksAsync(agentIds, request.PeriodFrom, request.PeriodUntil, request.AnalyseToken, ct);
        var clients = await LoadClientsAsync(agentIds, ct);
        var firstDayContracts = await _contractProvider.GetEffectiveContractDataForClientsAsync(agentIds, request.PeriodFrom);
        var preferredSymbols = await LoadPreferredSymbolsAsync(agentIds, ct);
        var blacklistByAgent = await LoadBlacklistByAgentAsync(agentIds, ct);
        var freeCommandDates = await LoadFreeCommandDatesAsync(agentIds, request.PeriodFrom, request.PeriodUntil, request.AnalyseToken, keywordMap, ct);
        var keywordRestrictions = await LoadKeywordRestrictionsAsync(agentIds, request.PeriodFrom, request.PeriodUntil, request.AnalyseToken, keywordMap, ct);
        var breaks = await LoadBreaksAsync(agentIds, request.PeriodFrom, request.PeriodUntil, request.AnalyseToken, ct);
        var breakDates = breaks.Select(b => (b.ClientId, b.CurrentDate)).ToHashSet();
        var contractDataByDate = await _contractProvider.GetEffectiveContractDataForClientsRangeAsync(
            agentIds, request.PeriodFrom, request.PeriodUntil);
        var contractDays = BuildContractDays(agentIds, request.PeriodFrom, request.PeriodUntil, contractDataByDate, ct);
        var individualPeriods = await LoadIndividualPeriodsAsync(contractDataByDate, ct);
        var periodTargetHours = ComputePeriodTargetHours(
            agentIds, request.PeriodFrom, request.PeriodUntil, contractDataByDate, individualPeriods);
        var softenings = await _softeningRepository.LoadAsync(agentIds, request.PeriodFrom, request.PeriodUntil, request.AnalyseToken, ct);

        var agents = BuildAgents(agentIds, firstDayContracts, periodTargetHours, clients, preferredSymbols, blacklistByAgent);
        var availability = BuildAvailability(agentIds, request.PeriodFrom, request.PeriodUntil, contractDays, freeCommandDates, breakDates, keywordRestrictions);
        var agentIdSet = agentIds.ToHashSet();
        var covers = await ReplacementCoverQuery.LoadAsync(_context, request.AnalyseToken, request.PeriodFrom, request.PeriodUntil, agentIds, ct);
        var assignments = BuildAssignments(works, breaks, covers, agentIdSet);
        var hints = BuildSofteningHints(softenings);

        // The harmonizer only swaps within existing assignments, so the eligibility slots that matter
        // are the (shift, date) pairs already present in the plan; a swap may move any agent onto them.
        var eligibilitySlots = assignments
            .Where(a => a.ShiftRefId != Guid.Empty)
            .Select(a => new EligibilitySlot(a.ShiftRefId, a.Date))
            .Distinct()
            .ToList();
        // Pre-Commit-Diff-Prinzip: an unlocked assignment the plan already holds is the incumbent for
        // its (agent, shift, date) triple — the opt-in expired-mandatory escalation must not retroactively
        // veto it; a locked cell is immutable already so it never needs the protection.
        var incumbentAssignments = assignments
            .Where(a => a.ShiftRefId != Guid.Empty && !a.IsLocked)
            .Select(a => (a.AgentId, a.ShiftRefId, a.Date))
            .ToHashSet();
        var eligibilityMatrix = await _eligibilityMatrixBuilder.BuildAsync(agentIds, eligibilitySlots, incumbentAssignments, ct);

        // C2: union qualification-ineligibility with availability-ineligibility (agent unavailable during
        // a shift's hours). The slots carry the existing works' time windows; any agent could be swapped
        // onto them, so all are checked. Availability is the WEAKEST scheduling layer: on any (agent, date)
        // governed by a Keyword command (FREE / OnlyEarly… / NoEarly…) or a Break, the availability block
        // is dropped so the higher layer rules the day. Qualification ineligibility is never suppressed.
        var availabilitySlots = works
            .Select(w => new AvailabilityShiftSlot(w.ShiftId, w.CurrentDate, w.StartTime, w.EndTime))
            .Distinct()
            .ToList();
        var availabilityIneligible = await _availabilityService.GetAsync(agentIds, availabilitySlots, ct);
        availabilityIneligible = AvailabilitySuppression.RemoveGovernedDays(
            availabilityIneligible,
            CollectGovernedDays(freeCommandDates, keywordRestrictions.Keys, breakDates));
        var ineligible = MergeIneligible(eligibilityMatrix.Ineligible, availabilityIneligible);

        // Boundary context: load works + breaks on the days adjacent to the period and place them in
        // BitmapInput.BoundaryAssignments. The bitmap itself stays sized to [PeriodFrom, PeriodUntil] —
        // boundary entries are never rendered into cells, never mutated, and never scored. They are
        // available for boundary-aware validators (MaxConsecutiveDays / MinRestHours crossing period edges).
        var contextDaysBefore = Math.Max(0, request.ContextDaysBefore);
        var contextDaysAfter = Math.Max(0, request.ContextDaysAfter);
        var contextFrom = request.PeriodFrom.AddDays(-contextDaysBefore);
        var contextUntil = request.PeriodUntil.AddDays(contextDaysAfter);

        IReadOnlyList<BitmapAssignment> boundaryAssignments = [];
        List<Work> boundaryWorks = [];
        if (contextFrom < request.PeriodFrom || contextUntil > request.PeriodUntil)
        {
            boundaryWorks = (await LoadWorksAsync(agentIds, contextFrom, contextUntil, request.AnalyseToken, ct))
                .Where(w => w.CurrentDate < request.PeriodFrom || w.CurrentDate > request.PeriodUntil)
                .ToList();
            var boundaryBreaks = (await LoadBreaksAsync(agentIds, contextFrom, contextUntil, request.AnalyseToken, ct))
                .Where(b => b.CurrentDate < request.PeriodFrom || b.CurrentDate > request.PeriodUntil)
                .ToList();
            var boundaryCovers = (await ReplacementCoverQuery.LoadAsync(_context, request.AnalyseToken, contextFrom, contextUntil, agentIds, ct))
                .Where(c => c.Date < request.PeriodFrom || c.Date > request.PeriodUntil)
                .ToList();
            boundaryAssignments = BuildAssignments(boundaryWorks, boundaryBreaks, boundaryCovers, agentIdSet);
        }

        // K16 restricted time windows: resolved once per run from the active rule set and the period's shift
        // ids present in the plan (group-tag scope is token-independent master data). Wizard 3's cross-day
        // veto reads them; Wizard 2/4 use the same-day validator directly and ignore the field.
        var periodShiftIds = assignments
            .Where(a => a.ShiftRefId != Guid.Empty)
            .Select(a => a.ShiftRefId)
            .Distinct()
            .ToList();
        var restrictedTimeWindows = await _restrictedWindowBuilder.BuildAsync(
            periodShiftIds, request.PeriodFrom, request.PeriodUntil, ct);

        BitmapPlanningRules? planningRules = null;
        if (request.LoadPlanningRules)
        {
            var ruleSet = await _ruleSetLoader.LoadRuleSetAsync(
                agentIds,
                request.PeriodFrom,
                request.PeriodUntil,
                request.AnalyseToken,
                Math.Min(contextDaysBefore, contextDaysAfter),
                PlanningRuleSources.All,
                InvalidHardRuleHandling.Report,
                ct);
            if (ruleSet.Rules.Count > 0 || ruleSet.InvalidHardRuleIds is { Count: > 0 })
            {
                planningRules = ToBitmapPlanningRules(ruleSet, contextFrom, contextUntil, works.Concat(boundaryWorks));
            }

            if (ruleSet.Rules.Count > 0)
            {
                agents = WithRuleAgents(agents, ruleSet.Agents);
            }
        }

        return new BitmapInput(
            agents,
            request.PeriodFrom,
            request.PeriodUntil,
            assignments,
            hints,
            availability,
            boundaryAssignments,
            ineligible,
            restrictedTimeWindows,
            planningRules);
    }

    /// <summary>
    /// An invalid approved hard constraint is skipped (Report mode, as in the pre-commit check) and travels along as
    /// InvalidHardRuleIds, so the run still honours every valid rule and the caller can warn instead of failing.
    /// The loader skips the carry-in of the symmetric covered window (the shorter ContextDays side); the engine
    /// boundary already holds every day in [contextFrom, contextUntil], so carry-in inside it is dropped here and no
    /// day counts twice. Container sub-works are ignored, exactly like the API rule readers do.
    /// </summary>
    private static BitmapPlanningRules ToBitmapPlanningRules(
        PlanningRuleSet ruleSet, DateOnly contextFrom, DateOnly contextUntil, IEnumerable<Work> loadedWorks)
    {
        var carryIn = ruleSet.CarryIn
            .Where(segment => segment.Date < contextFrom || segment.Date > contextUntil)
            .ToList();
        var ignoredWorkIds = loadedWorks
            .Where(work => work.ParentWorkId != null)
            .Select(work => work.Id)
            .ToHashSet();
        var nightRuleMinOverlap = ruleSet.Agents.Count > 0
            ? ruleSet.Agents[0].NightRuleMinOverlapMinutes
            : PlanningConstraintDefaults.DefaultNightRuleMinOverlapMinutes;
        return new BitmapPlanningRules(ruleSet.Rules, carryIn, nightRuleMinOverlap, ignoredWorkIds, ruleSet.InvalidHardRuleIds);
    }

    /// <summary>Night window and workload of the planning rules come from the loader, the same source the validators use.</summary>
    private static List<BitmapAgent> WithRuleAgents(List<BitmapAgent> agents, IReadOnlyList<RuleAgent> ruleAgents)
    {
        var byId = ruleAgents.ToDictionary(agent => agent.Id, StringComparer.Ordinal);
        return agents
            .Select(agent => byId.TryGetValue(agent.Id, out var ruleAgent)
                ? agent with { NightWindow = ruleAgent.NightWindow, WorkloadPercent = ruleAgent.WorkloadPercent }
                : agent)
            .ToList();
    }

    private static IReadOnlySet<(string AgentId, Guid ShiftId, DateOnly Date)> MergeIneligible(
        IReadOnlySet<(string AgentId, Guid ShiftId, DateOnly Date)> qualification,
        IReadOnlySet<(string AgentId, Guid ShiftId, DateOnly Date)> availability)
    {
        if (availability.Count == 0)
        {
            return qualification;
        }

        var merged = new HashSet<(string, Guid, DateOnly)>(qualification);
        merged.UnionWith(availability);
        return merged;
    }

    /// <summary>
    /// Collects every (agent, date) ruled by a higher scheduling layer so <see cref="AvailabilitySuppression"/>
    /// can drop the weakest-layer availability blocks on those days. The sources are disjoint by construction:
    /// FREE keywords arrive via the freeCommandDates set, the EARLY/LATE/NIGHT (and negation) keywords via the
    /// keywordRestrictions keys (LoadKeywordRestrictionsAsync maps only the category keywords, never FREE), and
    /// breaks via the breakDates set. AgentId is normalised to the ClientId string used by the triples.
    /// </summary>
    private static IReadOnlySet<(string AgentId, DateOnly Date)> CollectGovernedDays(
        params IEnumerable<(Guid AgentId, DateOnly Date)>[] governedSources)
    {
        var governedDays = new HashSet<(string AgentId, DateOnly Date)>();
        foreach (var source in governedSources)
        {
            foreach (var (agentId, date) in source)
            {
                governedDays.Add((agentId.ToString(), date));
            }
        }

        return governedDays;
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
                (p, s) => new { p.ClientId, StartTime = s.StartShift, EndTime = s.EndShift })
            .ToListAsync(ct);

        var result = new Dictionary<Guid, HashSet<CellSymbol>>();
        foreach (var pair in preferredPairs)
        {
            if (!result.TryGetValue(pair.ClientId, out var set))
            {
                set = [];
                result[pair.ClientId] = set;
            }
            set.Add(SymbolOfSpan(pair.StartTime, pair.EndTime));
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
        IReadOnlyDictionary<string, ScheduleCommandKeyword> keywordMap,
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
            if (ScheduleCommandKeywordMapper.TryMap(cmd.CommandKeyword, keywordMap, out var keyword)
                && keyword == ScheduleCommandKeyword.Free)
            {
                result.Add((cmd.ClientId, cmd.CurrentDate));
            }
        }
        return result;
    }

    private async Task<List<Break>> LoadBreaksAsync(
        List<Guid> agentIds,
        DateOnly from,
        DateOnly until,
        Guid? analyseToken,
        CancellationToken ct)
    {
        return await _context.Break
            .AsNoTracking()
            .Where(b => agentIds.Contains(b.ClientId)
                        && b.CurrentDate >= from
                        && b.CurrentDate <= until
                        && (b.AnalyseToken == analyseToken || (b.AnalyseToken == null && analyseToken == null)))
            .ToListAsync(ct);
    }

    private static Dictionary<(Guid AgentId, DateOnly Date), bool> BuildContractDays(
        List<Guid> agentIds,
        DateOnly from,
        DateOnly until,
        IReadOnlyDictionary<DateOnly, Dictionary<Guid, EffectiveContractData>> contractDataByDate,
        CancellationToken ct)
    {
        var result = new Dictionary<(Guid, DateOnly), bool>();

        for (var date = from; date <= until; date = date.AddDays(1))
        {
            ct.ThrowIfCancellationRequested();
            var perDay = contractDataByDate[date];
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

    /// <summary>
    /// Target hours of every agent over exactly the bitmap range [from, until]. GuaranteedHours is stated per
    /// pay period (GuaranteedHoursBasisInterval, else PaymentInterval), while the bitmap only holds the planned range, so each day contributes its
    /// pay-period share via <see cref="PayPeriodTargetHoursProrator"/>. Each day uses that day's own contract data,
    /// so a contract change or a month-specific company value inside the range is honoured. An agent without
    /// contract data on a day contributes nothing for that day.
    /// </summary>
    internal static Dictionary<Guid, decimal> ComputePeriodTargetHours(
        IReadOnlyList<Guid> agentIds,
        DateOnly from,
        DateOnly until,
        IReadOnlyDictionary<DateOnly, Dictionary<Guid, EffectiveContractData>> contractDataByDate,
        IReadOnlyDictionary<Guid, IReadOnlyCollection<Period>> individualPeriodsByContract)
    {
        var result = agentIds.ToDictionary(id => id, _ => 0m);
        for (var date = from; date <= until; date = date.AddDays(1))
        {
            if (!contractDataByDate.TryGetValue(date, out var perDay))
            {
                continue;
            }

            foreach (var agentId in agentIds)
            {
                if (!perDay.TryGetValue(agentId, out var data))
                {
                    continue;
                }

                var interval = (PaymentInterval)(data.GuaranteedHoursBasisInterval ?? data.PaymentInterval);
                var periods = data.ContractId is { } contractId
                    && individualPeriodsByContract.TryGetValue(contractId, out var found)
                        ? found
                        : null;
                result[agentId] += PayPeriodTargetHoursProrator.DailyShare(data.GuaranteedHours, interval, date, periods);
            }
        }

        return result;
    }

    private async Task<IReadOnlyDictionary<Guid, IReadOnlyCollection<Period>>> LoadIndividualPeriodsAsync(
        IReadOnlyDictionary<DateOnly, Dictionary<Guid, EffectiveContractData>> contractDataByDate,
        CancellationToken ct)
    {
        var individualContractIds = contractDataByDate.Values
            .SelectMany(perDay => perDay.Values)
            .Where(d => (d.GuaranteedHoursBasisInterval ?? d.PaymentInterval) == (int)PaymentInterval.Individual && d.ContractId.HasValue)
            .Select(d => d.ContractId!.Value)
            .Distinct()
            .ToList();

        if (individualContractIds.Count == 0)
        {
            return new Dictionary<Guid, IReadOnlyCollection<Period>>();
        }

        var contracts = await _context.Contract
            .AsNoTracking()
            .Where(c => individualContractIds.Contains(c.Id) && c.IndividualPeriod != null)
            .Include(c => c.IndividualPeriod!)
                .ThenInclude(i => i.Periods)
            .ToListAsync(ct);

        return contracts.ToDictionary(
            c => c.Id,
            c => (IReadOnlyCollection<Period>)c.IndividualPeriod!.Periods.ToList());
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
        IReadOnlyDictionary<Guid, decimal> periodTargetHours,
        IReadOnlyDictionary<Guid, Client> clients,
        IReadOnlyDictionary<Guid, HashSet<CellSymbol>> preferences,
        IReadOnlyDictionary<Guid, HashSet<Guid>> blacklists)
    {
        var result = new List<BitmapAgent>(agentIds.Count);
        foreach (var id in agentIds)
        {
            var displayName = clients.TryGetValue(id, out var c) ? BuildDisplayName(c) : id.ToString();
            var contract = contracts.TryGetValue(id, out var ct) ? ct : null;
            var targetHours = periodTargetHours.GetValueOrDefault(id);
            var maxWeekly = contract?.MaxWeeklyHours ?? 0m;
            var maxConsec = contract?.MaxConsecutiveDays > 0 ? contract.MaxConsecutiveDays : 6;
            var minPause = contract?.MinPauseHours ?? 0m;
            var minRestDays = contract?.MinRestDays > 0 ? contract.MinRestDays : SchedulingPolicyDefaults.MinRestDays;
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
                BlacklistedShiftIds: blacklist,
                MinRestDays: (int)Math.Ceiling(minRestDays)));
        }
        return result;
    }

    private static Dictionary<(string AgentId, DateOnly Date), DayAvailability> BuildAvailability(
        List<Guid> agentIds,
        DateOnly from,
        DateOnly until,
        IReadOnlyDictionary<(Guid AgentId, DateOnly Date), bool> contractDays,
        IReadOnlySet<(Guid AgentId, DateOnly Date)> freeCommandDates,
        IReadOnlySet<(Guid AgentId, DateOnly Date)> breakDates,
        IReadOnlyDictionary<(Guid AgentId, DateOnly Date), (CellSymbol? Required, CellSymbol? Forbidden)> keywordRestrictions)
    {
        var result = new Dictionary<(string, DateOnly), DayAvailability>();
        foreach (var agentId in agentIds)
        {
            for (var date = from; date <= until; date = date.AddDays(1))
            {
                var worksOnDay = contractDays.TryGetValue((agentId, date), out var w) && w;
                var hasFree = freeCommandDates.Contains((agentId, date));
                var hasBreak = breakDates.Contains((agentId, date));
                CellSymbol? required = null;
                CellSymbol? forbidden = null;
                if (keywordRestrictions.TryGetValue((agentId, date), out var restriction))
                {
                    required = restriction.Required;
                    forbidden = restriction.Forbidden;
                }
                result[(agentId.ToString(), date)] = new DayAvailability(worksOnDay, hasFree, hasBreak, required, forbidden);
            }
        }
        return result;
    }

    private async Task<Dictionary<(Guid AgentId, DateOnly Date), (CellSymbol? Required, CellSymbol? Forbidden)>> LoadKeywordRestrictionsAsync(
        List<Guid> agentIds,
        DateOnly from,
        DateOnly until,
        Guid? analyseToken,
        IReadOnlyDictionary<string, ScheduleCommandKeyword> keywordMap,
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

        var result = new Dictionary<(Guid, DateOnly), (CellSymbol? Required, CellSymbol? Forbidden)>();
        foreach (var cmd in rawCommands)
        {
            if (!ScheduleCommandKeywordMapper.TryMap(cmd.CommandKeyword, keywordMap, out var keyword))
            {
                continue;
            }
            CellSymbol? required = null;
            CellSymbol? forbidden = null;
            switch (keyword)
            {
                case ScheduleOptimizer.Models.ScheduleCommandKeyword.OnlyEarly: required = CellSymbol.Early; break;
                case ScheduleOptimizer.Models.ScheduleCommandKeyword.OnlyLate: required = CellSymbol.Late; break;
                case ScheduleOptimizer.Models.ScheduleCommandKeyword.OnlyNight: required = CellSymbol.Night; break;
                case ScheduleOptimizer.Models.ScheduleCommandKeyword.NoEarly: forbidden = CellSymbol.Early; break;
                case ScheduleOptimizer.Models.ScheduleCommandKeyword.NoLate: forbidden = CellSymbol.Late; break;
                case ScheduleOptimizer.Models.ScheduleCommandKeyword.NoNight: forbidden = CellSymbol.Night; break;
                default: continue;
            }
            result[(cmd.ClientId, cmd.CurrentDate)] = (required, forbidden);
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

    /// <summary>
    /// A work handed to a substitute by a replacement WorkChange is locked like a sealed work and keeps only the hours
    /// it still works; the substitute's replaced span becomes a locked occupied cell of the substitute carrying the
    /// handed-over hours (no work ids, so apply never re-points it). Without this the wizards would move or delete the
    /// cover, count its hours twice and see the substitute as free that day.
    /// </summary>
    internal static List<BitmapAssignment> BuildAssignments(
        IReadOnlyList<Work> works,
        IReadOnlyList<Break> breaks,
        IReadOnlyList<ReplacementCover> covers,
        IReadOnlySet<Guid> agentIds)
    {
        var handedOverHours = covers
            .GroupBy(c => c.WorkId)
            .ToDictionary(g => g.Key, g => g.Sum(c => c.Hours));
        var workAssignments = works.Select(w =>
        {
            var (startAt, endAt) = Span(w.CurrentDate, w.StartTime, w.EndTime);
            return new BitmapAssignment(
                AgentId: w.ClientId.ToString(),
                Date: w.CurrentDate,
                Symbol: SymbolOfSpan(w.StartTime, w.EndTime),
                ShiftRefId: w.ShiftId,
                WorkIds: [w.Id],
                IsLocked: w.LockLevel != WorkLockLevel.None || handedOverHours.ContainsKey(w.Id),
                StartAt: startAt,
                EndAt: endAt,
                Hours: Math.Max(0m, w.WorkTime - handedOverHours.GetValueOrDefault(w.Id)));
        });

        var breakAssignments = breaks.Select(b => new BitmapAssignment(
            AgentId: b.ClientId.ToString(),
            Date: b.CurrentDate,
            Symbol: CellSymbol.Break,
            ShiftRefId: Guid.Empty,
            WorkIds: [b.Id],
            IsLocked: true,
            StartAt: default,
            EndAt: default,
            Hours: b.WorkTime));

        var substituteAssignments = covers
            .Where(c => agentIds.Contains(c.SubstituteClientId))
            .Select(c => new BitmapAssignment(
                AgentId: c.SubstituteClientId.ToString(),
                Date: c.Date,
                Symbol: SymbolOfSpan(c.Start, c.End),
                ShiftRefId: c.ShiftId,
                WorkIds: [],
                IsLocked: true,
                StartAt: c.StartAt,
                EndAt: c.EndAt,
                Hours: c.Hours));

        return workAssignments.Concat(breakAssignments).Concat(substituteAssignments).ToList();
    }

    private static (DateTime StartAt, DateTime EndAt) Span(DateOnly date, TimeOnly start, TimeOnly end) =>
        (date.ToDateTime(start), end <= start ? date.AddDays(1).ToDateTime(end) : date.ToDateTime(end));

    private static CellSymbol SymbolOfSpan(TimeOnly start, TimeOnly end)
    {
        var typeIndex = ShiftTypeInference.FromSpan(start, end);
        return typeIndex switch
        {
            0 => CellSymbol.Early,
            1 => CellSymbol.Late,
            2 => CellSymbol.Night,
            _ => CellSymbol.Other,
        };
    }
}
