// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Emits ONE UnstaffedShiftSummaryTriggerEvent per staffed root group with a derivable pay cycle, covering the
/// window [today, max(end of the running pay period, today + 7)]. A shift day counts as a gap when
/// UnstaffedShiftPredicate says so (Quantity x SumEmployees, sporadic and container-template days excluded).
///
/// Per root the subtree is split into holiday-calendar clusters (CalendarClusterPartition); each cluster is
/// scanned through the shift schedule with its own official holidays, so holiday-only shifts still need
/// people on a holiday while regular shifts drop out. A shift in several clusters is counted once, in the
/// cluster of its most specific group. Days sealed globally or by any group of the subtree are removed
/// root-wide, and so are company holidays (GroupClosureDays: active members exist and all are away the whole
/// day). Ungrouped shifts are not reported here - UngroupedShiftsDetector covers them for the admins - and
/// container shifts are excluded by the filter. A root with PaymentInterval.Individual has no cycle and is
/// skipped with a log line.
///
/// The background tick runs DetectAsync and GetActiveFingerprintsAsync on the same scoped instance, so the
/// scan is memoised: the second call reuses the first result instead of repeating every schedule query.
/// The detector is uncapped (one event per root), so the fingerprint set equals the emitted event set.
/// </summary>
/// <param name="groupRepository">Lists all groups and the ids of groups that hold clients or shifts.</param>
/// <param name="weekConfiguration">Resolves the configured week start for weekly period boundaries.</param>
/// <param name="shiftScheduleRepository">Returns ShiftDayAssignment rows per cluster and window.</param>
/// <param name="groupScopeReader">Batched shift-to-groups lookup, used only when a root has several clusters.</param>
/// <param name="holidayCalendarResolver">Holiday calculator per calendar selection and year, global fallback.</param>
/// <param name="sealedDayRepository">All seal rows of the scanned range, read once per tick.</param>
/// <param name="groupAbsenceReadRepository">Membership windows and full-day absences for company holidays.</param>
/// <param name="companyClock">Resolves "today" as the company's own local day, not the server's UTC day.</param>
/// <param name="logger">Structured log per tick.</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.DTOs.Filter;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Services.Assistant;
using Klacks.Api.Domain.Services.Schedules;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public class UnstaffedShiftPeriodDetector : IAgentTriggerDetector, IAgentConditionFingerprintSource
{
    private const int MinimumLookaheadDays = 7;
    private const int UncappedRowCount = int.MaxValue;
    private const int FirstMonth = 1;
    private const int FirstDay = 1;

    private readonly IGroupRepository _groupRepository;
    private readonly IWeekConfiguration _weekConfiguration;
    private readonly IShiftScheduleRepository _shiftScheduleRepository;
    private readonly IShiftGroupScopeReader _groupScopeReader;
    private readonly IClientHolidayCalendarResolver _holidayCalendarResolver;
    private readonly ISealedDayRepository _sealedDayRepository;
    private readonly IGroupAbsenceReadRepository _groupAbsenceReadRepository;
    private readonly ICompanyClock _companyClock;
    private readonly ILogger<UnstaffedShiftPeriodDetector> _logger;

    private Task<IReadOnlyList<UnstaffedShiftSummaryTriggerEvent>>? _scan;

    public UnstaffedShiftPeriodDetector(
        IGroupRepository groupRepository,
        IWeekConfiguration weekConfiguration,
        IShiftScheduleRepository shiftScheduleRepository,
        IShiftGroupScopeReader groupScopeReader,
        IClientHolidayCalendarResolver holidayCalendarResolver,
        ISealedDayRepository sealedDayRepository,
        IGroupAbsenceReadRepository groupAbsenceReadRepository,
        ICompanyClock companyClock,
        ILogger<UnstaffedShiftPeriodDetector> logger)
    {
        _groupRepository = groupRepository;
        _weekConfiguration = weekConfiguration;
        _shiftScheduleRepository = shiftScheduleRepository;
        _groupScopeReader = groupScopeReader;
        _holidayCalendarResolver = holidayCalendarResolver;
        _sealedDayRepository = sealedDayRepository;
        _groupAbsenceReadRepository = groupAbsenceReadRepository;
        _companyClock = companyClock;
        _logger = logger;
    }

    public string Kind => AgentTriggerKinds.UnstaffedShift;

    public async Task<IReadOnlyList<IAgentTriggerEvent>> DetectAsync(CancellationToken cancellationToken = default)
    {
        var events = await ScanOnceAsync(cancellationToken);
        return events.Cast<IAgentTriggerEvent>().ToList();
    }

    public async Task<IReadOnlySet<string>> GetActiveFingerprintsAsync(CancellationToken cancellationToken = default)
    {
        var events = await ScanOnceAsync(cancellationToken);
        return events
            .Select(triggerEvent => AgentConditionLedgerPolicy.FingerprintFor(Kind, triggerEvent.DedupKey))
            .ToHashSet(StringComparer.Ordinal);
    }

    private Task<IReadOnlyList<UnstaffedShiftSummaryTriggerEvent>> ScanOnceAsync(CancellationToken cancellationToken) =>
        _scan ??= ScanAsync(cancellationToken);

    private async Task<IReadOnlyList<UnstaffedShiftSummaryTriggerEvent>> ScanAsync(CancellationToken cancellationToken)
    {
        var today = await _companyClock.GetTodayDateAsync(cancellationToken);
        var groups = await _groupRepository.List();
        if (groups.Count == 0)
        {
            return Array.Empty<UnstaffedShiftSummaryTriggerEvent>();
        }

        var weekStart = await _weekConfiguration.GetWeekStartAsync(today, cancellationToken);
        var nextWeekStart = weekStart.AddDays(NextPeriodBoundaries.WeeklyPeriodDays);
        var staffing = GroupStaffingLookup.Build(
            groups,
            await _groupRepository.GetGroupIdsWithMembersAsync(cancellationToken));
        var holidayCache = new Dictionary<(Guid?, int), IReadOnlySet<DateOnly>>();

        var findings = new List<RootGaps>();
        var skippedIndividual = 0;
        foreach (var root in groups.Where(group => group.Parent == null).OrderBy(group => group.Lft))
        {
            if (!NextPeriodBoundaries.HasDerivableCycle(root.PaymentInterval))
            {
                skippedIndividual++;
                _logger.LogInformation(
                    "UnstaffedShiftPeriod: root group {GroupName} uses PaymentInterval Individual, which has no derivable cycle — skipped",
                    root.Name);
                continue;
            }

            if (!staffing.IsStaffed(root.Id)) continue;

            var (periodStart, periodEnd) = PeriodBoundaries.CurrentFor(root, today, nextWeekStart);
            var minimumEnd = today.AddDays(MinimumLookaheadDays);
            var windowEnd = periodEnd > minimumEnd ? periodEnd : minimumEnd;
            var rootKey = TreeKey(root);
            var subtree = groups.Where(group => TreeKey(group) == rootKey).ToList();

            var gaps = await CollectGapsAsync(root, subtree, today, windowEnd, holidayCache, cancellationToken);
            if (gaps.Count > 0)
            {
                findings.Add(new RootGaps(root, subtree.Select(group => group.Id).ToHashSet(), periodStart, windowEnd, gaps));
            }
        }

        if (findings.Count == 0)
        {
            LogTick(groups.Count, 0, skippedIndividual);
            return Array.Empty<UnstaffedShiftSummaryTriggerEvent>();
        }

        var seals = await _sealedDayRepository.GetRangeAsync(
            today, findings.Max(finding => finding.WindowEnd), null, cancellationToken);

        var events = new List<UnstaffedShiftSummaryTriggerEvent>();
        foreach (var finding in findings)
        {
            var sealedDays = seals
                .Where(seal => seal.GroupId == null || finding.SubtreeGroupIds.Contains(seal.GroupId.Value))
                .Select(seal => seal.Date)
                .ToHashSet();
            var remaining = finding.Gaps.Where(gap => !sealedDays.Contains(gap.Date)).ToList();
            if (remaining.Count == 0) continue;

            var closureDays = await ClosureDaysAsync(finding.Root, today, finding.WindowEnd, cancellationToken);
            remaining = remaining.Where(gap => !closureDays.Contains(gap.Date)).ToList();
            if (remaining.Count == 0) continue;

            var firstGapDay = remaining.Min(gap => gap.Date);
            events.Add(new UnstaffedShiftSummaryTriggerEvent(
                finding.Root.Id,
                finding.Root.Name,
                finding.PeriodStart,
                finding.WindowEnd,
                remaining.Count,
                remaining.Select(gap => gap.Date).Distinct().Count(),
                firstGapDay,
                firstGapDay.DayNumber - today.DayNumber));
        }

        LogTick(groups.Count, events.Count, skippedIndividual);
        return events;
    }

    private async Task<List<ShiftDayAssignment>> CollectGapsAsync(
        Group root,
        IReadOnlyCollection<Group> subtree,
        DateOnly today,
        DateOnly windowEnd,
        Dictionary<(Guid?, int), IReadOnlySet<DateOnly>> holidayCache,
        CancellationToken cancellationToken)
    {
        var partition = CalendarClusterPartition.Build(root, subtree);
        var rowsByCluster = new Dictionary<Guid, List<ShiftDayAssignment>>();
        foreach (var cluster in partition.Clusters)
        {
            var holidays = await HolidaysAsync(cluster.CalendarSelectionId, today, windowEnd, holidayCache);
            var (rows, _) = await _shiftScheduleRepository.GetShiftScheduleAsync(
                BuildFilter(cluster.Head.Id, today, windowEnd, holidays), cancellationToken);
            rowsByCluster[cluster.Head.Id] = rows;
        }

        var owned = partition.Clusters.Count == 1
            ? rowsByCluster[root.Id]
            : await KeepOwnedRowsAsync(partition, rowsByCluster, cancellationToken);

        return owned
            .Where(row => row.Date >= today && row.Date <= windowEnd && UnstaffedShiftPredicate.IsUnstaffed(row))
            .GroupBy(row => (row.ShiftId, row.Date))
            .Select(duplicates => duplicates.First())
            .ToList();
    }

    private async Task<List<ShiftDayAssignment>> KeepOwnedRowsAsync(
        CalendarClusterPartition partition,
        Dictionary<Guid, List<ShiftDayAssignment>> rowsByCluster,
        CancellationToken cancellationToken)
    {
        var shiftIds = rowsByCluster.Values.SelectMany(rows => rows).Select(row => row.ShiftId).Distinct().ToList();
        var groupsByShift = await _groupScopeReader.GetGroupIdsByShiftIdsAsync(shiftIds, cancellationToken);
        var rootHeadId = partition.Clusters[0].Head.Id;

        var owned = new List<ShiftDayAssignment>();
        foreach (var (headId, rows) in rowsByCluster)
        {
            owned.AddRange(rows.Where(row =>
            {
                var owner = groupsByShift.TryGetValue(row.ShiftId, out var groupIds)
                    ? partition.OwnerOf(groupIds)
                    : null;
                return (owner?.Head.Id ?? rootHeadId) == headId;
            }));
        }

        return owned;
    }

    private async Task<IReadOnlySet<DateOnly>> ClosureDaysAsync(
        Group root, DateOnly from, DateOnly until, CancellationToken cancellationToken)
    {
        var windows = await _groupAbsenceReadRepository.GetMembershipWindowsAsync(root.Id, from, until, cancellationToken);
        if (windows.Count == 0)
        {
            return new HashSet<DateOnly>();
        }

        var absences = await _groupAbsenceReadRepository.GetFullDayAbsencesAsync(
            windows.Select(window => window.ClientId).Distinct().ToList(), from, until, cancellationToken);

        return GroupClosureDays.Compute(windows, absences, from, until);
    }

    private async Task<List<DateTime>> HolidaysAsync(
        Guid? calendarSelectionId,
        DateOnly from,
        DateOnly until,
        Dictionary<(Guid?, int), IReadOnlySet<DateOnly>> holidayCache)
    {
        var holidays = new List<DateTime>();
        for (var year = from.Year; year <= until.Year; year++)
        {
            if (!holidayCache.TryGetValue((calendarSelectionId, year), out var yearHolidays))
            {
                yearHolidays = await OfficialHolidaysOfYearAsync(calendarSelectionId, year);
                holidayCache[(calendarSelectionId, year)] = yearHolidays;
            }

            holidays.AddRange(yearHolidays
                .Where(day => day >= from && day <= until)
                .OrderBy(day => day)
                .Select(day => day.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)));
        }

        return holidays;
    }

    private async Task<IReadOnlySet<DateOnly>> OfficialHolidaysOfYearAsync(Guid? calendarSelectionId, int year)
    {
        var calculator = await _holidayCalendarResolver.GetCalculatorAsync(calendarSelectionId, year);
        var days = new HashSet<DateOnly>();
        if (calculator == null)
        {
            return days;
        }

        for (var day = new DateOnly(year, FirstMonth, FirstDay); day.Year == year; day = day.AddDays(1))
        {
            if (calculator.IsHoliday(day) == HolidayStatus.OfficialHoliday)
            {
                days.Add(day);
            }
        }

        return days;
    }

    private static ShiftScheduleFilter BuildFilter(Guid headGroupId, DateOnly from, DateOnly until, List<DateTime> holidays) => new()
    {
        StartDate = from,
        EndDate = until,
        SelectedGroup = headGroupId,
        HolidayDates = holidays,
        IsStandartShift = true,
        IsTimeRange = true,
        IsSporadic = false,
        Container = false,
        ShowUngroupedShifts = false,
        AnalyseToken = null,
        StartRow = 0,
        RowCount = UncappedRowCount
    };

    private static Guid TreeKey(Group group) => group.Root ?? group.Id;

    private void LogTick(int groupCount, int eventCount, int skippedIndividual) =>
        _logger.LogInformation(
            "UnstaffedShiftPeriod scan: {Groups} group(s) read, {Events} root summary event(s) emitted, {Skipped} root(s) skipped for PaymentInterval Individual",
            groupCount, eventCount, skippedIndividual);

    private sealed record RootGaps(
        Group Root,
        IReadOnlySet<Guid> SubtreeGroupIds,
        DateOnly PeriodStart,
        DateOnly WindowEnd,
        IReadOnlyList<ShiftDayAssignment> Gaps);
}
