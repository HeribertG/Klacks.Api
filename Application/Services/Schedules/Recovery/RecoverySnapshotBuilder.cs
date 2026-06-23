// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.ScheduleRecovery.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Services.Schedules.Recovery;

/// <summary>
/// Builds the recovery <see cref="RecoverySnapshot"/> from the live plan. Reads the group's active
/// members (candidate pool), their effective contract constraints (max-weekly / max-consecutive /
/// min-pause / works-on-day), the period target-hours deficit (fairness), shift preferences (preferred /
/// blacklist), the mandatory-qualification gate and explicit availability windows, plus every work in a
/// context window around the absence. Candidates and days are frozen into a stable order by the snapshot;
/// availability-window conflicts and missing qualifications both collapse into the ineligible gate.
/// </summary>
public sealed class RecoverySnapshotBuilder : IRecoverySnapshotBuilder
{
    private readonly IClientRepository _clientRepository;
    private readonly IClientContractDataProvider _contractProvider;
    private readonly IPeriodHoursService _periodHoursService;
    private readonly IClientShiftPreferenceRepository _preferenceRepository;
    private readonly IEligibilityMatrixBuilder _eligibilityMatrixBuilder;
    private readonly IClientAvailabilityRepository _availabilityRepository;
    private readonly IScheduleEntriesService _scheduleEntriesService;
    private readonly IGroupRepository _groupRepository;
    private readonly IGetAllClientIdsFromGroupAndSubgroups _groupClientService;
    private readonly ILogger<RecoverySnapshotBuilder> _logger;

    public RecoverySnapshotBuilder(
        IClientRepository clientRepository,
        IClientContractDataProvider contractProvider,
        IPeriodHoursService periodHoursService,
        IClientShiftPreferenceRepository preferenceRepository,
        IEligibilityMatrixBuilder eligibilityMatrixBuilder,
        IClientAvailabilityRepository availabilityRepository,
        IScheduleEntriesService scheduleEntriesService,
        IGroupRepository groupRepository,
        IGetAllClientIdsFromGroupAndSubgroups groupClientService,
        ILogger<RecoverySnapshotBuilder> logger)
    {
        _clientRepository = clientRepository;
        _contractProvider = contractProvider;
        _periodHoursService = periodHoursService;
        _preferenceRepository = preferenceRepository;
        _eligibilityMatrixBuilder = eligibilityMatrixBuilder;
        _availabilityRepository = availabilityRepository;
        _scheduleEntriesService = scheduleEntriesService;
        _groupRepository = groupRepository;
        _groupClientService = groupClientService;
        _logger = logger;
    }

    public async Task<RecoverySnapshot> BuildAsync(
        Guid groupId,
        Guid absentClientId,
        IReadOnlyList<DateOnly> absenceDates,
        CancellationToken cancellationToken)
    {
        var dates = absenceDates.OrderBy(d => d.DayNumber).ToList();
        if (dates.Count == 0)
        {
            return new RecoverySnapshot([], [], new Dictionary<CellKey, IReadOnlyList<RecoveryWork>>());
        }

        var referenceDate = dates[0];
        var windowStart = dates[0].AddDays(-RecoveryBuildConstants.ContextWindowDays);
        var windowEnd = dates[^1].AddDays(RecoveryBuildConstants.ContextWindowDays);

        // Candidate pool = everyone under the receiving group's ROOT. In-group = the receiving group's
        // own subtree; the rest of the root are borrowable cross-group candidates (bounded). The works are
        // read over the whole root so a borrowed candidate's HOME commitments are in the snapshot and the
        // engine cannot double-book them.
        var rootGroupId = await ResolveRootAsync(groupId);
        var inGroupSet = (await _groupClientService.GetAllClientIdsFromGroupAndSubgroups(groupId)).ToHashSet();

        var underRoot = await _clientRepository.GetActiveClientsWithAddressesForGroupsAsync([rootGroupId], cancellationToken);
        var members = SelectCandidatePool(underRoot, inGroupSet, groupId);
        var memberIds = members.Select(m => m.Id).ToList();

        var contracts = await _contractProvider.GetEffectiveContractDataForClientsAsync(memberIds, referenceDate);
        var (periodFrom, periodUntil) = await _periodHoursService.GetPeriodBoundariesAsync(referenceDate);
        var periodHours = await _periodHoursService.GetPeriodHoursAsync(memberIds, periodFrom, periodUntil, null);
        var (preferred, blacklisted) = await LoadPreferencesAsync(memberIds, cancellationToken);

        var cells = await _scheduleEntriesService
            .GetScheduleEntriesQuery(windowStart, windowEnd, [rootGroupId], null)
            .ToListAsync(cancellationToken);

        var works = BuildWorks(cells, out var breakDates);
        var availability = BuildAvailability(memberIds, contracts, breakDates, windowStart, windowEnd);
        var ineligible = await BuildIneligibleAsync(
            memberIds, dates, works, windowStart, windowEnd, cancellationToken);

        var agents = members
            .Select(m => new RecoveryAgent(
                m.Id,
                DisplayName(m),
                ContractOf(contracts, m.Id).MaxWeeklyHours,
                ContractOf(contracts, m.Id).MaxConsecutiveDays,
                ContractOf(contracts, m.Id).MinPauseHours,
                DeficitOf(periodHours, m.Id),
                preferred.TryGetValue(m.Id, out var pref) ? pref : new HashSet<Guid>(),
                blacklisted.TryGetValue(m.Id, out var black) ? black : new HashSet<Guid>(),
                IsInGroup: inGroupSet.Contains(m.Id)))
            .ToList();

        return new RecoverySnapshot(
            EnumerateDays(windowStart, windowEnd), agents, works, availability, ineligible,
            receivingGroupId: groupId);
    }

    private async Task<Guid> ResolveRootAsync(Guid groupId)
    {
        var group = await _groupRepository.Get(groupId);
        return group?.Root ?? groupId;
    }

    private List<Client> SelectCandidatePool(IReadOnlyList<Client> underRoot, IReadOnlySet<Guid> inGroupSet, Guid groupId)
    {
        var inGroup = underRoot.Where(c => inGroupSet.Contains(c.Id)).ToList();
        var crossGroup = underRoot.Where(c => !inGroupSet.Contains(c.Id)).OrderBy(c => c.Id).ToList();

        if (crossGroup.Count > RecoveryBuildConstants.CrossGroupPoolCap)
        {
            _logger.LogInformation(
                "Recovery cross-group pool for group {GroupId} capped at {Cap} of {Total} candidates.",
                groupId, RecoveryBuildConstants.CrossGroupPoolCap, crossGroup.Count);
            crossGroup = crossGroup.Take(RecoveryBuildConstants.CrossGroupPoolCap).ToList();
        }

        return inGroup.Concat(crossGroup).ToList();
    }

    private async Task<(Dictionary<Guid, HashSet<Guid>> Preferred, Dictionary<Guid, HashSet<Guid>> Blacklisted)>
        LoadPreferencesAsync(IReadOnlyList<Guid> memberIds, CancellationToken cancellationToken)
    {
        var preferred = new Dictionary<Guid, HashSet<Guid>>();
        var blacklisted = new Dictionary<Guid, HashSet<Guid>>();
        // One read per member: the candidate pool is a single group (bounded), and the preference
        // repository exposes no batch-by-clients accessor. Promote to a batch query if the pool grows large.
        foreach (var id in memberIds)
        {
            var prefs = await _preferenceRepository.GetByClientIdAsync(id, cancellationToken);
            foreach (var pref in prefs)
            {
                var target = pref.PreferenceType == ShiftPreferenceType.Blacklist ? blacklisted : preferred;
                if (!target.TryGetValue(id, out var set))
                {
                    set = [];
                    target[id] = set;
                }
                set.Add(pref.ShiftId);
            }
        }
        return (preferred, blacklisted);
    }

    private static IReadOnlyDictionary<CellKey, IReadOnlyList<RecoveryWork>> BuildWorks(
        IReadOnlyList<Domain.Models.Schedules.ScheduleCell> cells,
        out HashSet<(Guid ClientId, DateOnly Date)> breakDates)
    {
        var works = new Dictionary<CellKey, List<RecoveryWork>>();
        breakDates = [];

        foreach (var cell in cells)
        {
            var date = DateOnly.FromDateTime(cell.EntryDate);
            if (cell.EntryType == (int)ScheduleEntryType.Break)
            {
                breakDates.Add((cell.ClientId, date));
                continue;
            }
            if (cell.EntryType != (int)ScheduleEntryType.Work)
            {
                continue;
            }

            var (startAt, endAt) = Interval(date, cell.StartTime, cell.EndTime);
            // A group-restricted (sealed cross-group) work is immutable for the recovery engine, exactly
            // like a locked work (schedule.md / wizard-fixed-cells.md). get_schedule_entries returns such
            // works to this group flagged but with LockLevel=None, and they are NOT cloned into the
            // scenario; treating them as movable would over-report coverage and half-apply swaps.
            var immutable = cell.LockLevel != (int)WorkLockLevel.None || cell.IsGroupRestricted;
            var work = new RecoveryWork(
                CategoryOf(cell.StartTime),
                cell.EntryId,
                immutable,
                startAt,
                endAt,
                (decimal)(endAt - startAt).TotalHours,
                [cell.SourceId]);

            var key = new CellKey(cell.ClientId, date);
            if (!works.TryGetValue(key, out var list))
            {
                list = [];
                works[key] = list;
            }
            list.Add(work);
        }

        return works.ToDictionary(kv => kv.Key, kv => (IReadOnlyList<RecoveryWork>)kv.Value);
    }

    private static Dictionary<CellKey, DayAvailability> BuildAvailability(
        IReadOnlyList<Guid> memberIds,
        IReadOnlyDictionary<Guid, EffectiveContractData> contracts,
        IReadOnlySet<(Guid ClientId, DateOnly Date)> breakDates,
        DateOnly windowStart,
        DateOnly windowEnd)
    {
        var availability = new Dictionary<CellKey, DayAvailability>();
        foreach (var id in memberIds)
        {
            var contract = ContractOf(contracts, id);
            for (var date = windowStart; date <= windowEnd; date = date.AddDays(1))
            {
                // Intentional, stricter-than-find_replacement gate: an active contract's calendar bars its
                // off-days (mirroring the Wizard-2 WorksOnDay rule), so recovery will not place a candidate
                // on a contractual day off. find_replacement has no day-of-week gate; members without an
                // active contract are not gated here either, matching it for that case.
                var worksOnDay = !contract.HasActiveContract || WorksOnDay(contract, date.DayOfWeek);
                var hasBreak = breakDates.Contains((id, date));
                availability[new CellKey(id, date)] = new DayAvailability(worksOnDay, false, hasBreak);
            }
        }
        return availability;
    }

    private async Task<HashSet<IneligibleKey>> BuildIneligibleAsync(
        IReadOnlyList<Guid> memberIds,
        IReadOnlyList<DateOnly> dates,
        IReadOnlyDictionary<CellKey, IReadOnlyList<RecoveryWork>> works,
        DateOnly windowStart,
        DateOnly windowEnd,
        CancellationToken cancellationToken)
    {
        var ineligible = new HashSet<IneligibleKey>();

        // Every distinct working shift on the absence dates — the absent agent's demands AND any other
        // member's works that a swap could relocate. Gating only the demand shifts would leave swap
        // recipients unchecked, letting the engine relocate a shift to someone unqualified/unavailable.
        var shifts = new Dictionary<(Guid ShiftId, DateOnly Date), (DateTime Start, DateTime End)>();
        foreach (var date in dates)
        {
            foreach (var memberId in memberIds)
            {
                if (!works.TryGetValue(new CellKey(memberId, date), out var list))
                {
                    continue;
                }
                foreach (var work in list)
                {
                    if (work.IsWorking && work.ShiftId is Guid id && id != Guid.Empty)
                    {
                        shifts[(id, date)] = (work.StartAt, work.EndAt);
                    }
                }
            }
        }

        if (shifts.Count > 0)
        {
            var slots = shifts.Keys.Select(k => new EligibilitySlot(k.ShiftId, k.Date)).ToList();
            var matrix = await _eligibilityMatrixBuilder.BuildAsync(memberIds, slots, cancellationToken);
            foreach (var (agentId, shiftId, date) in matrix.Ineligible)
            {
                if (Guid.TryParse(agentId, out var parsed))
                {
                    ineligible.Add(new IneligibleKey(parsed, shiftId, date));
                }
            }
        }

        var availabilities = await _availabilityRepository.GetByDateRange(windowStart, windowEnd);
        var availabilityByClient = availabilities
            .GroupBy(a => a.ClientId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<ClientAvailability>)g.ToList());

        foreach (var ((shiftId, date), (startAt, endAt)) in shifts)
        {
            var start = TimeOnly.FromDateTime(startAt);
            var end = TimeOnly.FromDateTime(endAt);
            foreach (var memberId in memberIds)
            {
                if (availabilityByClient.TryGetValue(memberId, out var entries)
                    && AvailabilityMatcher.IsExplicitlyUnavailable(entries, start, end))
                {
                    ineligible.Add(new IneligibleKey(memberId, shiftId, date));
                }
            }
        }

        return ineligible;
    }

    private static (DateTime StartAt, DateTime EndAt) Interval(DateOnly date, TimeSpan start, TimeSpan end)
    {
        var startAt = date.ToDateTime(TimeOnly.FromTimeSpan(start));
        var endAt = date.ToDateTime(TimeOnly.FromTimeSpan(end));
        if (endAt <= startAt)
        {
            endAt = endAt.AddDays(1);
        }
        return (startAt, endAt);
    }

    // Coarse start-hour placeholder: classifies into Early/Late/Night only (never Other) and is currently
    // inert because BuildAvailability leaves RequiredCategory/ForbiddenCategory unset, so no keyword gate
    // reads it. Derive the category from the shift definition before wiring up category keyword restrictions.
    private static ShiftCategory CategoryOf(TimeSpan start)
    {
        var hour = start.Hours;
        if (hour < RecoveryBuildConstants.EarlyBeforeHour)
        {
            return ShiftCategory.Early;
        }
        return hour < RecoveryBuildConstants.LateBeforeHour ? ShiftCategory.Late : ShiftCategory.Night;
    }

    private static EffectiveContractData ContractOf(
        IReadOnlyDictionary<Guid, EffectiveContractData> contracts, Guid clientId)
        => contracts.TryGetValue(clientId, out var data) ? data : new EffectiveContractData();

    private static decimal DeficitOf(
        IReadOnlyDictionary<Guid, Domain.DTOs.Schedules.PeriodHoursResource> periodHours, Guid clientId)
        => periodHours.TryGetValue(clientId, out var hours) ? hours.GuaranteedHours - hours.Hours : 0m;

    private static bool WorksOnDay(EffectiveContractData contract, DayOfWeek dayOfWeek) => dayOfWeek switch
    {
        DayOfWeek.Monday => contract.WorkOnMonday,
        DayOfWeek.Tuesday => contract.WorkOnTuesday,
        DayOfWeek.Wednesday => contract.WorkOnWednesday,
        DayOfWeek.Thursday => contract.WorkOnThursday,
        DayOfWeek.Friday => contract.WorkOnFriday,
        DayOfWeek.Saturday => contract.WorkOnSaturday,
        DayOfWeek.Sunday => contract.WorkOnSunday,
        _ => false
    };

    private static IReadOnlyList<DateOnly> EnumerateDays(DateOnly start, DateOnly end)
    {
        var days = new List<DateOnly>();
        for (var date = start; date <= end; date = date.AddDays(1))
        {
            days.Add(date);
        }
        return days;
    }

    private static string DisplayName(Client client)
        => string.IsNullOrWhiteSpace(client.FirstName) ? client.Name : $"{client.FirstName} {client.Name}";
}
