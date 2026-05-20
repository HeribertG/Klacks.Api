// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for computing per-day employee headcount for the resource monitor dashboard card.
/// Y-axis unit: Mitarbeiter (MA). Five series per day:
///   Wunsch        = desired daily readiness (rosa gepunktet). Per employee:
///                   24/7 contracts smoothed to min(maxWorkDays, 7)/7 every day;
///                   restricted patterns realistic — 1.0 on flagged days, 0 on rest days.
///   Max           = maximum daily readiness (rot gestrichelt). Same logic with maxConsecutiveDays cap.
///   Total         = total headcount (blau). Count of distinct employees considered active on date
///                   (either has an active contract OR is a non-deleted Employee/ExternEmp client).
///   Dienste       = number of shifts scheduled on date (each shift = 1 employee position).
///                   Container shifts count as 1; sub-shifts referenced via ContainerTemplateItem excluded.
///   Absenzen      = sum of Absence.DefaultValue per active BreakPlaceholder, taken literally as entered
///                   (vacation/sickness include weekends — no FTE weighting, no weekday filter).
/// Per-employee WorkOn pattern + caps resolution rules:
///   • If the employee has at least one active contract on the date AND every active contract has
///     flaggedDays > 0 → use the first such contract's pattern and EffectiveCap (per-contract
///     SchedulingRule override with Settings fallback).
///   • Otherwise (no active contract, or any active contract has flaggedDays == 0 → Mischform) →
///     fall back to Settings.SCHEDULING_DEFAULT_WORK_ON_* and Settings caps.
/// </summary>
/// <param name="context">Database context for ClientContract, Client, Shift, BreakPlaceholder, ContainerTemplateItem, GroupItem, Settings queries</param>
/// <param name="logger">Logger for error handling via BaseHandler</param>
using System.Globalization;
using Klacks.Api.Application.DTOs.Dashboard;
using Klacks.Api.Application.Handlers;
using Klacks.Api.Application.Queries.Dashboard;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.Dashboard;

public class GetResourceMonitorQueryHandler : BaseHandler, IRequestHandler<GetResourceMonitorQuery, ResourceMonitorResource>
{
    private const int FALLBACK_MAX_WORK_DAYS = 5;
    private const int FALLBACK_MAX_CONSECUTIVE_DAYS = 6;
    private const int FULL_WEEK_DAYS = 7;

    private const bool DEFAULT_WORK_ON_MONDAY    = true;
    private const bool DEFAULT_WORK_ON_TUESDAY   = true;
    private const bool DEFAULT_WORK_ON_WEDNESDAY = true;
    private const bool DEFAULT_WORK_ON_THURSDAY  = true;
    private const bool DEFAULT_WORK_ON_FRIDAY    = true;
    private const bool DEFAULT_WORK_ON_SATURDAY  = false;
    private const bool DEFAULT_WORK_ON_SUNDAY    = false;

    private readonly DataBaseContext _context;

    public GetResourceMonitorQueryHandler(DataBaseContext context, ILogger<GetResourceMonitorQueryHandler> logger)
        : base(logger)
    {
        _context = context;
    }

    public async Task<ResourceMonitorResource> Handle(GetResourceMonitorQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var startDate = new DateOnly(request.Year, 1, 1);
            var endDate = new DateOnly(request.Year, 12, 31);

            var settingMaxWorkDays         = await ReadIntSettingAsync(Klacks.Api.Application.Constants.Settings.SCHEDULING_MAX_WORK_DAYS,         FALLBACK_MAX_WORK_DAYS,         cancellationToken);
            var settingMaxConsecutiveDays  = await ReadIntSettingAsync(Klacks.Api.Application.Constants.Settings.SCHEDULING_MAX_CONSECUTIVE_DAYS, FALLBACK_MAX_CONSECUTIVE_DAYS,  cancellationToken);

            var defaultPattern = new WeekdayPattern(
                Mon: await ReadBoolSettingAsync(Klacks.Api.Application.Constants.Settings.SCHEDULING_DEFAULT_WORK_ON_MONDAY,    DEFAULT_WORK_ON_MONDAY,    cancellationToken),
                Tue: await ReadBoolSettingAsync(Klacks.Api.Application.Constants.Settings.SCHEDULING_DEFAULT_WORK_ON_TUESDAY,   DEFAULT_WORK_ON_TUESDAY,   cancellationToken),
                Wed: await ReadBoolSettingAsync(Klacks.Api.Application.Constants.Settings.SCHEDULING_DEFAULT_WORK_ON_WEDNESDAY, DEFAULT_WORK_ON_WEDNESDAY, cancellationToken),
                Thu: await ReadBoolSettingAsync(Klacks.Api.Application.Constants.Settings.SCHEDULING_DEFAULT_WORK_ON_THURSDAY,  DEFAULT_WORK_ON_THURSDAY,  cancellationToken),
                Fri: await ReadBoolSettingAsync(Klacks.Api.Application.Constants.Settings.SCHEDULING_DEFAULT_WORK_ON_FRIDAY,    DEFAULT_WORK_ON_FRIDAY,    cancellationToken),
                Sat: await ReadBoolSettingAsync(Klacks.Api.Application.Constants.Settings.SCHEDULING_DEFAULT_WORK_ON_SATURDAY,  DEFAULT_WORK_ON_SATURDAY,  cancellationToken),
                Sun: await ReadBoolSettingAsync(Klacks.Api.Application.Constants.Settings.SCHEDULING_DEFAULT_WORK_ON_SUNDAY,    DEFAULT_WORK_ON_SUNDAY,    cancellationToken));

            HashSet<Guid>? groupShiftIds = null;
            if (request.GroupId.HasValue)
            {
                groupShiftIds = await _context.GroupItem
                    .Where(gi => gi.GroupId == request.GroupId && !gi.IsDeleted && gi.ShiftId != null)
                    .Select(gi => gi.ShiftId!.Value)
                    .ToHashSetAsync(cancellationToken);
            }

            HashSet<Guid>? groupClientIds = null;
            if (groupShiftIds != null)
            {
                groupClientIds = await _context.Work
                    .Where(w => !w.IsDeleted
                        && w.CurrentDate >= startDate
                        && w.CurrentDate <= endDate
                        && w.AnalyseToken == null
                        && groupShiftIds.Contains(w.ShiftId))
                    .Select(w => w.ClientId)
                    .Distinct()
                    .ToHashSetAsync(cancellationToken);
            }

            var contractQuery = _context.ClientContract
                .Include(cc => cc.Contract).ThenInclude(c => c!.SchedulingRule)
                .Where(cc => !cc.IsDeleted
                    && cc.IsActive
                    && cc.FromDate <= endDate
                    && (cc.UntilDate == null || cc.UntilDate >= startDate));

            if (groupClientIds != null)
                contractQuery = contractQuery.Where(cc => groupClientIds.Contains(cc.ClientId));

            var contracts = await contractQuery.ToListAsync(cancellationToken);

            var contractsByClient = contracts
                .GroupBy(cc => cc.ClientId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var employeeClientQuery = _context.Client
                .Where(c => c.Type != EntityTypeEnum.Customer);

            if (groupClientIds != null)
                employeeClientQuery = employeeClientQuery.Where(c => groupClientIds.Contains(c.Id));

            var employeeClientIds = await employeeClientQuery
                .Select(c => c.Id)
                .ToHashSetAsync(cancellationToken);

            var allClientIds = new HashSet<Guid>(employeeClientIds);
            foreach (var cc in contracts)
                allClientIds.Add(cc.ClientId);

            var containedShiftIds = await _context.ContainerTemplateItem
                .Where(cti => !cti.IsDeleted
                    && cti.ShiftId != null
                    && !cti.ContainerTemplate.IsDeleted)
                .Select(cti => cti.ShiftId!.Value)
                .Distinct()
                .ToHashSetAsync(cancellationToken);

            var shiftQuery = _context.Shift
                .Where(s => !s.IsDeleted
                    && !s.IsTimeRange
                    && !s.IsSporadic
                    && s.AnalyseToken == null
                    && s.Status != ShiftStatus.SealedOrder
                    && !containedShiftIds.Contains(s.Id)
                    && s.FromDate <= endDate
                    && (s.UntilDate == null || s.UntilDate >= startDate));

            if (groupShiftIds != null)
                shiftQuery = shiftQuery.Where(s => groupShiftIds.Contains(s.Id));

            var shifts = await shiftQuery
                .Select(s => new
                {
                    s.FromDate,
                    s.UntilDate,
                    s.IsMonday,
                    s.IsTuesday,
                    s.IsWednesday,
                    s.IsThursday,
                    s.IsFriday,
                    s.IsSaturday,
                    s.IsSunday,
                })
                .ToListAsync(cancellationToken);

            var periodStart = startDate.ToDateTime(TimeOnly.MinValue);
            var periodEnd   = endDate.ToDateTime(TimeOnly.MaxValue);

            var absenzQuery = _context.BreakPlaceholder
                .Include(bp => bp.Absence)
                .Where(bp => !bp.IsDeleted
                    && bp.From < periodEnd
                    && bp.Until > periodStart);

            if (groupClientIds != null)
                absenzQuery = absenzQuery.Where(bp => groupClientIds.Contains(bp.ClientId));

            var absences = await absenzQuery
                .Select(bp => new { bp.From, bp.Until, DefaultValue = bp.Absence.DefaultValue })
                .ToListAsync(cancellationToken);

            var absenzByDate = new Dictionary<DateOnly, double>();
            foreach (var bp in absences)
            {
                if (bp.DefaultValue <= 0) continue;

                var fromDay = DateOnly.FromDateTime(bp.From);
                var untilDay = DateOnly.FromDateTime(bp.Until);
                if (fromDay < startDate) fromDay = startDate;
                if (untilDay > endDate) untilDay = endDate;

                for (var d = fromDay; d <= untilDay; d = d.AddDays(1))
                {
                    absenzByDate[d] = absenzByDate.GetValueOrDefault(d) + bp.DefaultValue;
                }
            }

            var totalDays = endDate.DayNumber - startDate.DayNumber + 1;
            var dailyData = new List<ResourceMonitorDayResource>(totalDays);

            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                double wunschCount = 0;
                double maxCount = 0;
                double totalCount = 0;

                foreach (var clientId in allClientIds)
                {
                    var hasContract = contractsByClient.TryGetValue(clientId, out var clientContracts);
                    var activeContracts = hasContract
                        ? clientContracts!.Where(cc => cc.FromDate <= date && (!cc.UntilDate.HasValue || cc.UntilDate.Value >= date)).ToList()
                        : new List<ClientContract>();

                    if (activeContracts.Count == 0 && !employeeClientIds.Contains(clientId))
                        continue;

                    totalCount += 1;

                    WeekdayPattern pattern;
                    int wunschCap;
                    int maxCap;

                    if (ShouldUseSettings(activeContracts))
                    {
                        pattern = defaultPattern;
                        wunschCap = settingMaxWorkDays;
                        maxCap = settingMaxConsecutiveDays;
                    }
                    else
                    {
                        var primary = activeContracts[0];
                        pattern = WeekdayPattern.FromContract(primary.Contract!);
                        wunschCap = EffectiveCap(primary, settingMaxWorkDays, useConsecutive: false);
                        maxCap    = EffectiveCap(primary, settingMaxConsecutiveDays, useConsecutive: true);
                    }

                    wunschCount += ContributionForPattern(pattern, date, wunschCap);
                    maxCount    += ContributionForPattern(pattern, date, maxCap);
                }

                double dienstCount = 0;
                foreach (var s in shifts)
                {
                    if (s.FromDate > date || (s.UntilDate.HasValue && s.UntilDate.Value < date)) continue;
                    if (IsWeekdayActive(date, s.IsMonday, s.IsTuesday, s.IsWednesday, s.IsThursday, s.IsFriday, s.IsSaturday, s.IsSunday))
                        dienstCount += 1;
                }

                absenzByDate.TryGetValue(date, out var absenzCount);

                dailyData.Add(new ResourceMonitorDayResource
                {
                    Date = date,
                    WunschCount = Math.Round(wunschCount, 2),
                    MaxCount    = Math.Round(maxCount, 2),
                    TotalCount  = totalCount,
                    DienstCount = dienstCount,
                    AbsenzCount = Math.Round(absenzCount, 2),
                });
            }

            return new ResourceMonitorResource { DailyData = dailyData };
        }, nameof(Handle));
    }

    private async Task<int> ReadIntSettingAsync(string type, int fallback, CancellationToken cancellationToken)
    {
        var raw = await _context.Settings
            .Where(s => s.Type == type)
            .Select(s => s.Value)
            .FirstOrDefaultAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(raw)
            && int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            && parsed > 0)
        {
            return parsed;
        }
        return fallback;
    }

    private async Task<bool> ReadBoolSettingAsync(string type, bool fallback, CancellationToken cancellationToken)
    {
        var raw = await _context.Settings
            .Where(s => s.Type == type)
            .Select(s => s.Value)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(raw))
            return fallback;

        return bool.TryParse(raw, out var parsed) ? parsed : fallback;
    }

    private static int EffectiveCap(ClientContract cc, int settingFallback, bool useConsecutive)
    {
        var rule = cc.Contract?.SchedulingRule;
        if (rule != null)
        {
            var ruleValue = useConsecutive ? rule.MaxConsecutiveDays : rule.MaxWorkDays;
            if (ruleValue.HasValue && ruleValue.Value > 0)
                return ruleValue.Value;
        }
        return settingFallback;
    }

    private static bool ShouldUseSettings(List<ClientContract> activeContracts)
    {
        if (activeContracts.Count == 0) return true;
        foreach (var cc in activeContracts)
        {
            if (cc.Contract is null) return true;
            if (FlaggedDays(cc.Contract) == 0) return true;
        }
        return false;
    }

    private static int FlaggedDays(Contract contract) =>
        (contract.WorkOnMonday    ? 1 : 0) +
        (contract.WorkOnTuesday   ? 1 : 0) +
        (contract.WorkOnWednesday ? 1 : 0) +
        (contract.WorkOnThursday  ? 1 : 0) +
        (contract.WorkOnFriday    ? 1 : 0) +
        (contract.WorkOnSaturday  ? 1 : 0) +
        (contract.WorkOnSunday    ? 1 : 0);

    private static double ContributionForPattern(WeekdayPattern pattern, DateOnly date, int cap)
    {
        var flaggedDays = pattern.FlaggedDays;
        if (flaggedDays == 0) return 0;

        if (flaggedDays == FULL_WEEK_DAYS)
        {
            return Math.Min(cap, FULL_WEEK_DAYS) / (double)FULL_WEEK_DAYS;
        }

        return IsWeekdayActive(date, pattern.Mon, pattern.Tue, pattern.Wed, pattern.Thu, pattern.Fri, pattern.Sat, pattern.Sun)
            ? 1.0
            : 0.0;
    }

    private static bool IsWeekdayActive(
        DateOnly date,
        bool mon, bool tue, bool wed, bool thu, bool fri, bool sat, bool sun) => date.DayOfWeek switch
        {
            DayOfWeek.Monday    => mon,
            DayOfWeek.Tuesday   => tue,
            DayOfWeek.Wednesday => wed,
            DayOfWeek.Thursday  => thu,
            DayOfWeek.Friday    => fri,
            DayOfWeek.Saturday  => sat,
            DayOfWeek.Sunday    => sun,
            _                   => false,
        };

    private readonly record struct WeekdayPattern(bool Mon, bool Tue, bool Wed, bool Thu, bool Fri, bool Sat, bool Sun)
    {
        public int FlaggedDays =>
            (Mon ? 1 : 0) + (Tue ? 1 : 0) + (Wed ? 1 : 0) +
            (Thu ? 1 : 0) + (Fri ? 1 : 0) + (Sat ? 1 : 0) + (Sun ? 1 : 0);

        public static WeekdayPattern FromContract(Contract contract) => new(
            contract.WorkOnMonday,
            contract.WorkOnTuesday,
            contract.WorkOnWednesday,
            contract.WorkOnThursday,
            contract.WorkOnFriday,
            contract.WorkOnSaturday,
            contract.WorkOnSunday);
    }
}
