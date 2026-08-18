// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Globalization;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Scheduling;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.Associations;

/// <summary>
/// Single choke point translating contracts, scheduling rules, rate revisions, the company-wide
/// monthly target hours table and default settings into effective contract data. Clients without a
/// contract resolve to the monthly value for that month when a row exists, otherwise to the settings.
/// </summary>
public class ClientContractDataProvider : IClientContractDataProvider
{
    private readonly DataBaseContext _context;
    private readonly Dictionary<(int Year, int Month), MonthlyTargetHours?> _monthlyTargetHoursByMonth = new();

    // Resolved once per scoped provider. The recalculation pipeline asks for contract data once per
    // Work, so without this every single work paid the 40-key settings query and the revision probe.
    // Safe because no code path writes settings and resolves contract data in the same scope: the
    // settings handler only QUEUES a recalculation, and ThoroughRecalculationBackgroundService opens a
    // fresh scope per request - a settings change is therefore always picked up by the next scope.
    // The monthly target hours probe follows the same argument: the settings card commits the row in
    // its own request scope, so the next resolving scope always sees it.
    private DefaultSettings? _defaultSettings;
    private bool? _hasRateRevisions;
    private bool? _hasMonthlyTargetHours;


    public ClientContractDataProvider(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<EffectiveContractData> GetEffectiveContractDataAsync(Guid clientId, DateOnly date, int? paymentInterval = null)
    {
        var result = await GetEffectiveContractDataForClientsAsync(new List<Guid> { clientId }, date, paymentInterval);
        return result.GetValueOrDefault(clientId)
            ?? BuildFromDefaults(await LoadDefaultSettingsAsync(), await LoadMonthlyTargetHoursAsync(date));
    }

    public async Task<Dictionary<Guid, EffectiveContractData>> GetEffectiveContractDataForClientsAsync(
        List<Guid> clientIds, DateOnly date, int? paymentInterval = null)
    {
        var contracts = await LoadActiveContractsByClientAsync(clientIds, date, paymentInterval);
        var defaults = await LoadDefaultSettingsAsync();
        var rateSnapshotByRuleId = await LoadApplicableRateSnapshotsAsync(contracts.Values, date);
        var monthlyTargetHours = await LoadMonthlyTargetHoursAsync(date);

        // The contract path keeps its original gate: only a winning contract on the MonthlyTargetHours
        // interval sees the row (BuildEffectiveData re-tests the interval per contract anyway). Clients
        // without a contract take the raw company value instead of the settings fallback.
        var contractMonthlyTargetHours = contracts.Values.Any(c => c.PaymentInterval == PaymentInterval.MonthlyTargetHours)
            ? monthlyTargetHours
            : null;

        var result = new Dictionary<Guid, EffectiveContractData>();

        foreach (var clientId in clientIds)
        {
            result[clientId] = contracts.TryGetValue(clientId, out var contract)
                ? BuildEffectiveData(contract, defaults, ResolveRateSnapshot(contract, rateSnapshotByRuleId), contractMonthlyTargetHours)
                : BuildFromDefaults(defaults, monthlyTargetHours);
        }

        return result;
    }

    public async Task<Dictionary<DateOnly, Dictionary<Guid, EffectiveContractData>>> GetEffectiveContractDataForClientsRangeAsync(
        List<Guid> clientIds, DateOnly from, DateOnly until, int? paymentInterval = null)
    {
        var result = new Dictionary<DateOnly, Dictionary<Guid, EffectiveContractData>>();
        if (from > until)
        {
            return result;
        }

        var defaults = await LoadDefaultSettingsAsync();
        var contractsByClient = await LoadOverlappingContractsByClientAsync(clientIds, from, until, paymentInterval);
        var revisionsByRule = await LoadRateRevisionsUpToAsync(contractsByClient, until);
        var monthlyTargetHoursByMonth = await LoadMonthlyTargetHoursForRangeAsync(from, until);

        for (var date = from; date <= until; date = date.AddDays(1))
        {
            var perDay = new Dictionary<Guid, EffectiveContractData>(clientIds.Count);

            var winners = new Dictionary<Guid, Contract>(clientIds.Count);
            foreach (var clientId in clientIds)
            {
                var contract = ResolveContractOn(contractsByClient, clientId, date);
                if (contract is not null)
                {
                    winners[clientId] = contract;
                }
            }

            // The per-day path gates the monthly override on the RESOLVED contracts, not on every row
            // active that day, and passes the single value to every client. A row that lost the FromDate
            // race must not open the gate. BuildEffectiveData re-tests the interval per contract, so this
            // gate only decides whether a contract sees the row at all. Clients without a contract on
            // this day always take the raw company value over the settings fallback.
            var monthlyRowOfMonth = monthlyTargetHoursByMonth.GetValueOrDefault((date.Year, date.Month));
            var monthlyTargetHours = winners.Values.Any(c => c.PaymentInterval == PaymentInterval.MonthlyTargetHours)
                ? monthlyRowOfMonth
                : null;

            foreach (var clientId in clientIds)
            {
                if (!winners.TryGetValue(clientId, out var contract))
                {
                    perDay[clientId] = BuildFromDefaults(defaults, monthlyRowOfMonth);
                    continue;
                }

                perDay[clientId] = BuildEffectiveData(
                    contract,
                    defaults,
                    ResolveRateSnapshotOn(contract, revisionsByRule, date),
                    monthlyTargetHours);
            }

            result[date] = perDay;
        }

        return result;
    }

    /// <summary>
    /// Loads every contract row overlapping the range once. The per-day resolution then picks the row
    /// active on that date with the latest FromDate - the same rule the per-day query expressed as
    /// GroupBy plus OrderByDescending.
    /// </summary>
    private async Task<Dictionary<Guid, List<ClientContract>>> LoadOverlappingContractsByClientAsync(
        List<Guid> clientIds, DateOnly from, DateOnly until, int? paymentInterval)
    {
        var query = _context.ClientContract
            .Where(cc => clientIds.Contains(cc.ClientId)
                && cc.IsActive
                && cc.FromDate <= until
                && (cc.UntilDate == null || cc.UntilDate >= from));

        if (paymentInterval.HasValue)
        {
            var interval = (Domain.Enums.PaymentInterval)paymentInterval.Value;
            query = query.Where(cc => cc.Contract.PaymentInterval == interval);
        }

        var rows = await query
            .Include(cc => cc.Contract)
                .ThenInclude(c => c.SchedulingRule)
            .ToListAsync();

        return rows
            .GroupBy(cc => cc.ClientId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(cc => cc.FromDate).ToList());
    }

    private static Contract? ResolveContractOn(
        IReadOnlyDictionary<Guid, List<ClientContract>> contractsByClient, Guid clientId, DateOnly date)
    {
        if (!contractsByClient.TryGetValue(clientId, out var rows))
        {
            return null;
        }

        foreach (var row in rows)
        {
            if (row.FromDate <= date && (row.UntilDate == null || row.UntilDate >= date))
            {
                return row.Contract;
            }
        }

        return null;
    }

    private async Task<IReadOnlyDictionary<Guid, List<SchedulingRuleRateRevision>>> LoadRateRevisionsUpToAsync(
        IReadOnlyDictionary<Guid, List<ClientContract>> contractsByClient, DateOnly until)
    {
        var empty = new Dictionary<Guid, List<SchedulingRuleRateRevision>>();

        _hasRateRevisions ??= await _context.SchedulingRuleRateRevisions.AnyAsync();
        if (!_hasRateRevisions.Value)
        {
            return empty;
        }

        var ruleIds = contractsByClient.Values
            .SelectMany(rows => rows)
            .Where(cc => cc.Contract.SchedulingRuleId.HasValue)
            .Select(cc => cc.Contract.SchedulingRuleId!.Value)
            .Distinct()
            .ToList();

        if (ruleIds.Count == 0)
        {
            return empty;
        }

        var applicable = await _context.SchedulingRuleRateRevisions
            .Where(r => ruleIds.Contains(r.SchedulingRuleId) && r.ValidFrom <= until)
            .ToListAsync();

        return applicable
            .GroupBy(r => r.SchedulingRuleId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(r => r.ValidFrom).ToList());
    }

    private static SchedulingRuleRateRevision? ResolveRateSnapshotOn(
        Contract contract,
        IReadOnlyDictionary<Guid, List<SchedulingRuleRateRevision>> revisionsByRule,
        DateOnly date)
    {
        if (!contract.SchedulingRuleId.HasValue
            || !revisionsByRule.TryGetValue(contract.SchedulingRuleId.Value, out var revisions))
        {
            return null;
        }

        foreach (var revision in revisions)
        {
            if (revision.ValidFrom <= date)
            {
                return revision;
            }
        }

        return null;
    }

    /// <summary>
    /// Loads the monthly override for every month the range touches, reusing the per-month memo shared
    /// with the per-day path. The month rows also feed clients without a contract, so the load no longer
    /// depends on any contract's payment interval; an installation whose table is empty pays a single
    /// existence probe per scope and never queries the months.
    /// </summary>
    private async Task<Dictionary<(int Year, int Month), MonthlyTargetHours?>> LoadMonthlyTargetHoursForRangeAsync(
        DateOnly from, DateOnly until)
    {
        var result = new Dictionary<(int Year, int Month), MonthlyTargetHours?>();

        _hasMonthlyTargetHours ??= await _context.MonthlyTargetHours.AnyAsync();
        if (!_hasMonthlyTargetHours.Value)
        {
            return result;
        }

        for (var date = new DateOnly(from.Year, from.Month, 1); date <= until; date = date.AddMonths(1))
        {
            var key = (date.Year, date.Month);
            if (_monthlyTargetHoursByMonth.TryGetValue(key, out var cached))
            {
                result[key] = cached;
                continue;
            }

            var row = await _context.MonthlyTargetHours
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Year == date.Year && m.Month == date.Month);

            _monthlyTargetHoursByMonth[key] = row;
            result[key] = row;
        }

        return result;
    }

    private static SchedulingRuleRateRevision? ResolveRateSnapshot(
        Contract contract, IReadOnlyDictionary<Guid, SchedulingRuleRateRevision> rateSnapshotByRuleId)
    {
        return contract.SchedulingRuleId.HasValue
               && rateSnapshotByRuleId.TryGetValue(contract.SchedulingRuleId.Value, out var snapshot)
            ? snapshot
            : null;
    }

    // Cheap existence probe first: an installation that never ships a dated rate revision (every current
    // customer) pays a single AnyAsync returning false, never a join on the hot recompute path. Only when
    // revisions exist do we load the latest revision effective on or before the work date per referenced rule.
    private async Task<IReadOnlyDictionary<Guid, SchedulingRuleRateRevision>> LoadApplicableRateSnapshotsAsync(
        IEnumerable<Contract> contracts, DateOnly date)
    {
        var empty = new Dictionary<Guid, SchedulingRuleRateRevision>();

        _hasRateRevisions ??= await _context.SchedulingRuleRateRevisions.AnyAsync();
        if (!_hasRateRevisions.Value)
        {
            return empty;
        }

        var ruleIds = contracts
            .Where(c => c.SchedulingRuleId.HasValue)
            .Select(c => c.SchedulingRuleId!.Value)
            .Distinct()
            .ToList();

        if (ruleIds.Count == 0)
        {
            return empty;
        }

        var applicable = await _context.SchedulingRuleRateRevisions
            .Where(r => ruleIds.Contains(r.SchedulingRuleId) && r.ValidFrom <= date)
            .ToListAsync();

        return applicable
            .GroupBy(r => r.SchedulingRuleId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(r => r.ValidFrom).First());
    }

    // The month row feeds contracts on the MonthlyTargetHours interval AND clients without a contract,
    // so it is loaded regardless of the intervals in play. An installation whose table is empty pays a
    // single existence probe per scope. Callers like the harmonizer resolve contract data once per day
    // of a period, so the lookup is memoised per month for the lifetime of this scoped provider,
    // including the "no row" answer.
    private async Task<MonthlyTargetHours?> LoadMonthlyTargetHoursAsync(DateOnly date)
    {
        _hasMonthlyTargetHours ??= await _context.MonthlyTargetHours.AnyAsync();
        if (!_hasMonthlyTargetHours.Value)
        {
            return null;
        }

        var key = (date.Year, date.Month);
        if (_monthlyTargetHoursByMonth.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var row = await _context.MonthlyTargetHours
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Year == date.Year && m.Month == date.Month);

        _monthlyTargetHoursByMonth[key] = row;
        return row;
    }

    private async Task<Dictionary<Guid, Contract>> LoadActiveContractsByClientAsync(
        List<Guid> clientIds, DateOnly date, int? paymentInterval = null)
    {
        var query = _context.ClientContract
            .Where(cc => clientIds.Contains(cc.ClientId)
                && cc.IsActive
                && cc.FromDate <= date
                && (cc.UntilDate == null || cc.UntilDate >= date));

        if (paymentInterval.HasValue)
        {
            var interval = (Domain.Enums.PaymentInterval)paymentInterval.Value;
            query = query.Where(cc => cc.Contract.PaymentInterval == interval);
        }

        var clientContracts = await query
            .Include(cc => cc.Contract)
                .ThenInclude(c => c.SchedulingRule)
            .ToListAsync();

        return clientContracts
            .GroupBy(cc => cc.ClientId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(cc => cc.FromDate).First().Contract);
    }

    private async Task<DefaultSettings> LoadDefaultSettingsAsync()
    {
        if (_defaultSettings is not null)
        {
            return _defaultSettings;
        }

        var keys = new[]
        {
            SettingKeys.NightRate, SettingKeys.HolidayRate, SettingKeys.WE1Rate, SettingKeys.WE2Rate, SettingKeys.WE3Rate,
            SettingKeys.SurchargeNightStart, SettingKeys.SurchargeNightEnd,
            SettingKeys.SurchargeNightRateMode, SettingKeys.SurchargeHolidayRateMode,
            SettingKeys.SurchargeWE1RateMode, SettingKeys.SurchargeWE2RateMode, SettingKeys.SurchargeWE3RateMode,
            SettingKeys.SurchargeNightMinimumPerHour, SettingKeys.SurchargeHolidayMinimumPerHour,
            SettingKeys.SurchargeWE1MinimumPerHour, SettingKeys.SurchargeWE2MinimumPerHour, SettingKeys.SurchargeWE3MinimumPerHour,
            SettingKeys.GuaranteedHours, SettingKeys.FullTime, SettingKeys.DefaultWorkingHours,
            SettingKeys.OvertimeThreshold, SettingKeys.MaximumHours, SettingKeys.MinimumHours,
            SettingKeys.PaymentInterval, SettingKeys.VacationDaysPerYear,
            SettingKeys.SchedulingMaxWorkDays, SettingKeys.SchedulingMinRestDays,
            SettingKeys.SchedulingMinPauseHours, SettingKeys.SchedulingMaxOptimalGap,
            SettingKeys.SchedulingMaxDailyHours, SettingKeys.SchedulingMaxWeeklyHours,
            SettingKeys.SchedulingMaxConsecutiveDays,
            SettingKeys.SchedulingDefaultWorkOnMonday, SettingKeys.SchedulingDefaultWorkOnTuesday,
            SettingKeys.SchedulingDefaultWorkOnWednesday, SettingKeys.SchedulingDefaultWorkOnThursday,
            SettingKeys.SchedulingDefaultWorkOnFriday, SettingKeys.SchedulingDefaultWorkOnSaturday,
            SettingKeys.SchedulingDefaultWorkOnSunday, SettingKeys.SchedulingDefaultPerformsShiftWork
        };

        var settings = await _context.Settings
            .Where(s => keys.Contains(s.Type))
            .ToDictionaryAsync(s => s.Type, s => s.Value);

        _defaultSettings = new DefaultSettings
        {
            NightRate = ParseDecimal(settings.GetValueOrDefault(SettingKeys.NightRate)),
            HolidayRate = ParseDecimal(settings.GetValueOrDefault(SettingKeys.HolidayRate)),
            WE1Rate = ParseDecimal(settings.GetValueOrDefault(SettingKeys.WE1Rate)),
            WE2Rate = ParseDecimal(settings.GetValueOrDefault(SettingKeys.WE2Rate)),
            WE3Rate = ParseDecimal(settings.GetValueOrDefault(SettingKeys.WE3Rate)),
            NightRateMode = ParseRateMode(settings.GetValueOrDefault(SettingKeys.SurchargeNightRateMode)),
            HolidayRateMode = ParseRateMode(settings.GetValueOrDefault(SettingKeys.SurchargeHolidayRateMode)),
            WE1RateMode = ParseRateMode(settings.GetValueOrDefault(SettingKeys.SurchargeWE1RateMode)),
            WE2RateMode = ParseRateMode(settings.GetValueOrDefault(SettingKeys.SurchargeWE2RateMode)),
            WE3RateMode = ParseRateMode(settings.GetValueOrDefault(SettingKeys.SurchargeWE3RateMode)),
            NightMinimumPerHour = ParseNullableDecimal(settings.GetValueOrDefault(SettingKeys.SurchargeNightMinimumPerHour)),
            HolidayMinimumPerHour = ParseNullableDecimal(settings.GetValueOrDefault(SettingKeys.SurchargeHolidayMinimumPerHour)),
            WE1MinimumPerHour = ParseNullableDecimal(settings.GetValueOrDefault(SettingKeys.SurchargeWE1MinimumPerHour)),
            WE2MinimumPerHour = ParseNullableDecimal(settings.GetValueOrDefault(SettingKeys.SurchargeWE2MinimumPerHour)),
            WE3MinimumPerHour = ParseNullableDecimal(settings.GetValueOrDefault(SettingKeys.SurchargeWE3MinimumPerHour)),
            NightStart = ParseTimeOfDay(settings.GetValueOrDefault(SettingKeys.SurchargeNightStart), SurchargeDefaults.NightStart),
            NightEnd = ParseTimeOfDay(settings.GetValueOrDefault(SettingKeys.SurchargeNightEnd), SurchargeDefaults.NightEnd),
            GuaranteedHours = ParseDecimal(settings.GetValueOrDefault(SettingKeys.GuaranteedHours)),
            FullTime = ParseDecimal(settings.GetValueOrDefault(SettingKeys.FullTime)),
            DefaultWorkingHours = ParseDecimal(settings.GetValueOrDefault(SettingKeys.DefaultWorkingHours)),
            OvertimeThreshold = ParseDecimal(settings.GetValueOrDefault(SettingKeys.OvertimeThreshold)),
            MaximumHours = ParseDecimal(settings.GetValueOrDefault(SettingKeys.MaximumHours)),
            MinimumHours = ParseDecimal(settings.GetValueOrDefault(SettingKeys.MinimumHours)),
            PaymentInterval = ParseInt(settings.GetValueOrDefault(SettingKeys.PaymentInterval)),
            VacationDaysPerYear = ParseInt(settings.GetValueOrDefault(SettingKeys.VacationDaysPerYear)),
            MaxWorkDays = ParseInt(settings.GetValueOrDefault(SettingKeys.SchedulingMaxWorkDays)),
            MinRestDays = ParseDecimal(settings.GetValueOrDefault(SettingKeys.SchedulingMinRestDays)),
            MinPauseHours = ParseDecimal(settings.GetValueOrDefault(SettingKeys.SchedulingMinPauseHours)),
            MaxOptimalGap = ParseDecimal(settings.GetValueOrDefault(SettingKeys.SchedulingMaxOptimalGap)),
            MaxDailyHours = ParseDecimal(settings.GetValueOrDefault(SettingKeys.SchedulingMaxDailyHours)),
            MaxWeeklyHours = ParseDecimal(settings.GetValueOrDefault(SettingKeys.SchedulingMaxWeeklyHours)),
            MaxConsecutiveDays = ParseInt(settings.GetValueOrDefault(SettingKeys.SchedulingMaxConsecutiveDays)),
            WorkOnMonday = ParseBool(settings.GetValueOrDefault(SettingKeys.SchedulingDefaultWorkOnMonday)),
            WorkOnTuesday = ParseBool(settings.GetValueOrDefault(SettingKeys.SchedulingDefaultWorkOnTuesday)),
            WorkOnWednesday = ParseBool(settings.GetValueOrDefault(SettingKeys.SchedulingDefaultWorkOnWednesday)),
            WorkOnThursday = ParseBool(settings.GetValueOrDefault(SettingKeys.SchedulingDefaultWorkOnThursday)),
            WorkOnFriday = ParseBool(settings.GetValueOrDefault(SettingKeys.SchedulingDefaultWorkOnFriday)),
            WorkOnSaturday = ParseBool(settings.GetValueOrDefault(SettingKeys.SchedulingDefaultWorkOnSaturday)),
            WorkOnSunday = ParseBool(settings.GetValueOrDefault(SettingKeys.SchedulingDefaultWorkOnSunday)),
            PerformsShiftWork = ParseBool(settings.GetValueOrDefault(SettingKeys.SchedulingDefaultPerformsShiftWork))
        };

        return _defaultSettings;
    }

    // A non-null rateSnapshot is the applicable dated rate revision (latest ValidFrom &lt;= work date): it
    // REPLACES the rule's base surcharge-rate columns as a full snapshot, so a null rate field in the
    // snapshot falls through to contract/settings and never inherits from the base rule or an earlier
    // revision. With no snapshot the resolution is identical to the pre-revision behaviour.
    private static EffectiveContractData BuildEffectiveData(
        Contract contract, DefaultSettings defaults, SchedulingRuleRateRevision? rateSnapshot,
        MonthlyTargetHours? monthlyTargetHours)
    {
        var rule = contract.SchedulingRule;

        var nightRateBase = rateSnapshot != null ? rateSnapshot.NightRate : rule?.NightRate;
        var holidayRateBase = rateSnapshot != null ? rateSnapshot.HolidayRate : rule?.HolidayRate;
        var we1RateBase = rateSnapshot != null ? rateSnapshot.WE1Rate : rule?.WE1Rate;
        var we2RateBase = rateSnapshot != null ? rateSnapshot.WE2Rate : rule?.WE2Rate;
        var we3RateBase = rateSnapshot != null ? rateSnapshot.WE3Rate : rule?.WE3Rate;

        return new EffectiveContractData
        {
            GuaranteedHours = ResolveGuaranteedHours(contract, rule, defaults, monthlyTargetHours),
            MaximumHours = rule?.MaximumHours ?? contract.MaximumHours ?? defaults.MaximumHours,
            MinimumHours = rule?.MinimumHours ?? contract.MinimumHours ?? defaults.MinimumHours,
            FullTime = rule?.FullTimeHours ?? contract.FullTime ?? defaults.FullTime,
            NightRate = nightRateBase ?? contract.NightRate ?? defaults.NightRate,
            HolidayRate = holidayRateBase ?? contract.HolidayRate ?? defaults.HolidayRate,
            WE1Rate = we1RateBase ?? contract.WE1Rate ?? defaults.WE1Rate,
            WE2Rate = we2RateBase ?? contract.WE2Rate ?? defaults.WE2Rate,
            WE3Rate = we3RateBase ?? contract.WE3Rate ?? defaults.WE3Rate,
            NightRateMode = defaults.NightRateMode,
            HolidayRateMode = defaults.HolidayRateMode,
            WE1RateMode = defaults.WE1RateMode,
            WE2RateMode = defaults.WE2RateMode,
            WE3RateMode = defaults.WE3RateMode,
            NightMinimumPerHour = defaults.NightMinimumPerHour,
            HolidayMinimumPerHour = defaults.HolidayMinimumPerHour,
            WE1MinimumPerHour = defaults.WE1MinimumPerHour,
            WE2MinimumPerHour = defaults.WE2MinimumPerHour,
            WE3MinimumPerHour = defaults.WE3MinimumPerHour,
            NightStart = rule?.NightStart ?? contract.NightStart ?? defaults.NightStart,
            NightEnd = rule?.NightEnd ?? contract.NightEnd ?? defaults.NightEnd,
            PaymentInterval = (int)contract.PaymentInterval,
            CalendarSelectionId = contract.CalendarSelectionId,

            DefaultWorkingHours = rule?.DefaultWorkingHours ?? defaults.DefaultWorkingHours,
            OvertimeThreshold = rule?.OvertimeThreshold ?? defaults.OvertimeThreshold,
            MaxWorkDays = rule?.MaxWorkDays ?? defaults.MaxWorkDays,
            MinRestDays = rule?.MinRestDays ?? defaults.MinRestDays,
            MinPauseHours = rule?.MinPauseHours ?? defaults.MinPauseHours,
            MaxOptimalGap = rule?.MaxOptimalGap ?? defaults.MaxOptimalGap,
            MaxDailyHours = rule?.MaxDailyHours ?? defaults.MaxDailyHours,
            MaxWeeklyHours = rule?.MaxWeeklyHours ?? defaults.MaxWeeklyHours,
            MaxConsecutiveDays = rule?.MaxConsecutiveDays ?? defaults.MaxConsecutiveDays,
            VacationDaysPerYear = rule?.VacationDaysPerYear ?? defaults.VacationDaysPerYear,

            HasActiveContract = true,
            ContractId = contract.Id,
            SchedulingRuleId = contract.SchedulingRuleId,

            WorkOnMonday = rule?.WorkOnMonday ?? contract.WorkOnMonday,
            WorkOnTuesday = rule?.WorkOnTuesday ?? contract.WorkOnTuesday,
            WorkOnWednesday = rule?.WorkOnWednesday ?? contract.WorkOnWednesday,
            WorkOnThursday = rule?.WorkOnThursday ?? contract.WorkOnThursday,
            WorkOnFriday = rule?.WorkOnFriday ?? contract.WorkOnFriday,
            WorkOnSaturday = rule?.WorkOnSaturday ?? contract.WorkOnSaturday,
            WorkOnSunday = rule?.WorkOnSunday ?? contract.WorkOnSunday,
            PerformsShiftWork = rule?.PerformsShiftWork ?? contract.PerformsShiftWork
        };
    }

    // A matching monthly target hours row SHORT-CIRCUITS the usual rule -> contract -> settings chain
    // instead of extending it: the company-wide monthly value wins even over a SchedulingRule, which is
    // the whole point of the override. Without a row for that month, or on any other payment interval,
    // the original chain applies unchanged. Percent scales the company value down to this contract and
    // is treated as full workload when unset.
    private static decimal ResolveGuaranteedHours(
        Contract contract, SchedulingRule? rule, DefaultSettings defaults, MonthlyTargetHours? monthlyTargetHours)
    {
        if (contract.PaymentInterval == PaymentInterval.MonthlyTargetHours && monthlyTargetHours != null)
        {
            var percent = contract.Percent ?? MonthlyTargetHoursConstants.FullWorkloadPercent;
            return monthlyTargetHours.Hours * percent / MonthlyTargetHoursConstants.FullWorkloadPercent;
        }

        return rule?.GuaranteedHours ?? contract.GuaranteedHours ?? defaults.GuaranteedHours;
    }

    // A client without a contract takes the company-wide monthly value at full workload when a row
    // exists for that month (there is no contract percent to scale by); otherwise the settings value
    // applies as before.
    private static EffectiveContractData BuildFromDefaults(DefaultSettings defaults, MonthlyTargetHours? monthlyTargetHours)
    {
        return new EffectiveContractData
        {
            GuaranteedHours = monthlyTargetHours?.Hours ?? defaults.GuaranteedHours,
            MaximumHours = defaults.MaximumHours,
            MinimumHours = defaults.MinimumHours,
            FullTime = defaults.FullTime,
            NightRate = defaults.NightRate,
            HolidayRate = defaults.HolidayRate,
            WE1Rate = defaults.WE1Rate,
            WE2Rate = defaults.WE2Rate,
            WE3Rate = defaults.WE3Rate,
            NightRateMode = defaults.NightRateMode,
            HolidayRateMode = defaults.HolidayRateMode,
            WE1RateMode = defaults.WE1RateMode,
            WE2RateMode = defaults.WE2RateMode,
            WE3RateMode = defaults.WE3RateMode,
            NightMinimumPerHour = defaults.NightMinimumPerHour,
            HolidayMinimumPerHour = defaults.HolidayMinimumPerHour,
            WE1MinimumPerHour = defaults.WE1MinimumPerHour,
            WE2MinimumPerHour = defaults.WE2MinimumPerHour,
            WE3MinimumPerHour = defaults.WE3MinimumPerHour,
            NightStart = defaults.NightStart,
            NightEnd = defaults.NightEnd,
            PaymentInterval = defaults.PaymentInterval,
            CalendarSelectionId = null,

            DefaultWorkingHours = defaults.DefaultWorkingHours,
            OvertimeThreshold = defaults.OvertimeThreshold,
            MaxWorkDays = defaults.MaxWorkDays,
            MinRestDays = defaults.MinRestDays,
            MinPauseHours = defaults.MinPauseHours,
            MaxOptimalGap = defaults.MaxOptimalGap,
            MaxDailyHours = defaults.MaxDailyHours,
            MaxWeeklyHours = defaults.MaxWeeklyHours,
            MaxConsecutiveDays = defaults.MaxConsecutiveDays,
            VacationDaysPerYear = defaults.VacationDaysPerYear,

            HasActiveContract = false,
            ContractId = null,
            SchedulingRuleId = null,

            WorkOnMonday = defaults.WorkOnMonday,
            WorkOnTuesday = defaults.WorkOnTuesday,
            WorkOnWednesday = defaults.WorkOnWednesday,
            WorkOnThursday = defaults.WorkOnThursday,
            WorkOnFriday = defaults.WorkOnFriday,
            WorkOnSaturday = defaults.WorkOnSaturday,
            WorkOnSunday = defaults.WorkOnSunday,
            PerformsShiftWork = defaults.PerformsShiftWork
        };
    }

    private static decimal ParseDecimal(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return 0;

        return decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result) ? result : 0;
    }

    private static int ParseInt(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return 0;

        return int.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result) ? result : 0;
    }

    private static string ParseTimeOfDay(string? value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value;
    }

    private static decimal? ParseNullableDecimal(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return null;

        return decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result) ? result : null;
    }

    private static SurchargeRateMode ParseRateMode(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return SurchargeRateMode.Multiplier;

        return Enum.TryParse<SurchargeRateMode>(value, ignoreCase: true, out var mode) ? mode : SurchargeRateMode.Multiplier;
    }

    // Absent rows default to true: the seed ships every SCHEDULING_DEFAULT_* flag as true and a
    // contract-less fallback that cannot work on any day would silently exclude the client from
    // planning (observed live: only early shifts were planned for a whole month).
    private static bool ParseBool(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return true;

        return !string.Equals(value, "false", StringComparison.OrdinalIgnoreCase);
    }

    private sealed record DefaultSettings
    {
        public decimal NightRate { get; init; }
        public decimal HolidayRate { get; init; }
        public decimal WE1Rate { get; init; }
        public decimal WE2Rate { get; init; }
        public decimal WE3Rate { get; init; }
        public SurchargeRateMode NightRateMode { get; init; }
        public SurchargeRateMode HolidayRateMode { get; init; }
        public SurchargeRateMode WE1RateMode { get; init; }
        public SurchargeRateMode WE2RateMode { get; init; }
        public SurchargeRateMode WE3RateMode { get; init; }
        public decimal? NightMinimumPerHour { get; init; }
        public decimal? HolidayMinimumPerHour { get; init; }
        public decimal? WE1MinimumPerHour { get; init; }
        public decimal? WE2MinimumPerHour { get; init; }
        public decimal? WE3MinimumPerHour { get; init; }
        public string NightStart { get; init; } = SurchargeDefaults.NightStart;
        public string NightEnd { get; init; } = SurchargeDefaults.NightEnd;
        public decimal GuaranteedHours { get; init; }
        public decimal FullTime { get; init; }
        public decimal DefaultWorkingHours { get; init; }
        public decimal OvertimeThreshold { get; init; }
        public decimal MaximumHours { get; init; }
        public decimal MinimumHours { get; init; }
        public int PaymentInterval { get; init; }
        public int VacationDaysPerYear { get; init; }
        public int MaxWorkDays { get; init; }
        public decimal MinRestDays { get; init; }
        public decimal MinPauseHours { get; init; }
        public decimal MaxOptimalGap { get; init; }
        public decimal MaxDailyHours { get; init; }
        public decimal MaxWeeklyHours { get; init; }
        public int MaxConsecutiveDays { get; init; }
        public bool WorkOnMonday { get; init; }
        public bool WorkOnTuesday { get; init; }
        public bool WorkOnWednesday { get; init; }
        public bool WorkOnThursday { get; init; }
        public bool WorkOnFriday { get; init; }
        public bool WorkOnSaturday { get; init; }
        public bool WorkOnSunday { get; init; }
        public bool PerformsShiftWork { get; init; }
    }
}
