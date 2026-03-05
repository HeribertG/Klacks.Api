// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Globalization;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.Associations;

public class ClientContractDataProvider : IClientContractDataProvider
{
    private readonly DataBaseContext _context;

    public ClientContractDataProvider(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<EffectiveContractData> GetEffectiveContractDataAsync(Guid clientId, DateOnly date, int? paymentInterval = null)
    {
        var result = await GetEffectiveContractDataForClientsAsync(new List<Guid> { clientId }, date, paymentInterval);
        return result.GetValueOrDefault(clientId) ?? BuildFromDefaults(await LoadDefaultSettingsAsync());
    }

    public async Task<Dictionary<Guid, EffectiveContractData>> GetEffectiveContractDataForClientsAsync(
        List<Guid> clientIds, DateOnly date, int? paymentInterval = null)
    {
        var contracts = await LoadActiveContractsByClientAsync(clientIds, date, paymentInterval);
        var defaults = await LoadDefaultSettingsAsync();

        var result = new Dictionary<Guid, EffectiveContractData>();

        foreach (var clientId in clientIds)
        {
            result[clientId] = contracts.TryGetValue(clientId, out var contract)
                ? BuildEffectiveData(contract, defaults)
                : BuildFromDefaults(defaults);
        }

        return result;
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
        var keys = new[]
        {
            SettingKeys.NightRate, SettingKeys.HolidayRate, SettingKeys.SaRate, SettingKeys.SoRate,
            SettingKeys.GuaranteedHours, SettingKeys.FullTime, SettingKeys.DefaultWorkingHours,
            SettingKeys.OvertimeThreshold, SettingKeys.MaximumHours, SettingKeys.MinimumHours,
            SettingKeys.PaymentInterval, SettingKeys.VacationDaysPerYear,
            SettingKeys.SchedulingMaxWorkDays, SettingKeys.SchedulingMinRestDays,
            SettingKeys.SchedulingMinPauseHours, SettingKeys.SchedulingMaxOptimalGap,
            SettingKeys.SchedulingMaxDailyHours, SettingKeys.SchedulingMaxWeeklyHours,
            SettingKeys.SchedulingMaxConsecutiveDays
        };

        var settings = await _context.Settings
            .Where(s => keys.Contains(s.Type))
            .ToDictionaryAsync(s => s.Type, s => s.Value);

        return new DefaultSettings
        {
            NightRate = ParseDecimal(settings.GetValueOrDefault(SettingKeys.NightRate)),
            HolidayRate = ParseDecimal(settings.GetValueOrDefault(SettingKeys.HolidayRate)),
            SaRate = ParseDecimal(settings.GetValueOrDefault(SettingKeys.SaRate)),
            SoRate = ParseDecimal(settings.GetValueOrDefault(SettingKeys.SoRate)),
            GuaranteedHours = ParseDecimal(settings.GetValueOrDefault(SettingKeys.GuaranteedHours)),
            FullTime = ParseDecimal(settings.GetValueOrDefault(SettingKeys.FullTime)),
            DefaultWorkingHours = ParseDecimal(settings.GetValueOrDefault(SettingKeys.DefaultWorkingHours)),
            OvertimeThreshold = ParseDecimal(settings.GetValueOrDefault(SettingKeys.OvertimeThreshold)),
            MaximumHours = ParseDecimal(settings.GetValueOrDefault(SettingKeys.MaximumHours)),
            MinimumHours = ParseDecimal(settings.GetValueOrDefault(SettingKeys.MinimumHours)),
            PaymentInterval = ParseInt(settings.GetValueOrDefault(SettingKeys.PaymentInterval)),
            VacationDaysPerYear = ParseInt(settings.GetValueOrDefault(SettingKeys.VacationDaysPerYear)),
            MaxWorkDays = ParseInt(settings.GetValueOrDefault(SettingKeys.SchedulingMaxWorkDays)),
            MinRestDays = ParseInt(settings.GetValueOrDefault(SettingKeys.SchedulingMinRestDays)),
            MinPauseHours = ParseDecimal(settings.GetValueOrDefault(SettingKeys.SchedulingMinPauseHours)),
            MaxOptimalGap = ParseDecimal(settings.GetValueOrDefault(SettingKeys.SchedulingMaxOptimalGap)),
            MaxDailyHours = ParseDecimal(settings.GetValueOrDefault(SettingKeys.SchedulingMaxDailyHours)),
            MaxWeeklyHours = ParseDecimal(settings.GetValueOrDefault(SettingKeys.SchedulingMaxWeeklyHours)),
            MaxConsecutiveDays = ParseInt(settings.GetValueOrDefault(SettingKeys.SchedulingMaxConsecutiveDays))
        };
    }

    private static EffectiveContractData BuildEffectiveData(Contract contract, DefaultSettings defaults)
    {
        var rule = contract.SchedulingRule;

        return new EffectiveContractData
        {
            GuaranteedHours = rule?.GuaranteedHours ?? contract.GuaranteedHours ?? defaults.GuaranteedHours,
            MaximumHours = rule?.MaximumHours ?? contract.MaximumHours ?? defaults.MaximumHours,
            MinimumHours = rule?.MinimumHours ?? contract.MinimumHours ?? defaults.MinimumHours,
            FullTime = rule?.FullTimeHours ?? contract.FullTime ?? defaults.FullTime,
            NightRate = contract.NightRate ?? defaults.NightRate,
            HolidayRate = contract.HolidayRate ?? defaults.HolidayRate,
            SaRate = contract.SaRate ?? defaults.SaRate,
            SoRate = contract.SoRate ?? defaults.SoRate,
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
            SchedulingRuleId = contract.SchedulingRuleId
        };
    }

    private static EffectiveContractData BuildFromDefaults(DefaultSettings defaults)
    {
        return new EffectiveContractData
        {
            GuaranteedHours = defaults.GuaranteedHours,
            MaximumHours = defaults.MaximumHours,
            MinimumHours = defaults.MinimumHours,
            FullTime = defaults.FullTime,
            NightRate = defaults.NightRate,
            HolidayRate = defaults.HolidayRate,
            SaRate = defaults.SaRate,
            SoRate = defaults.SoRate,
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
            SchedulingRuleId = null
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

    private sealed record DefaultSettings
    {
        public decimal NightRate { get; init; }
        public decimal HolidayRate { get; init; }
        public decimal SaRate { get; init; }
        public decimal SoRate { get; init; }
        public decimal GuaranteedHours { get; init; }
        public decimal FullTime { get; init; }
        public decimal DefaultWorkingHours { get; init; }
        public decimal OvertimeThreshold { get; init; }
        public decimal MaximumHours { get; init; }
        public decimal MinimumHours { get; init; }
        public int PaymentInterval { get; init; }
        public int VacationDaysPerYear { get; init; }
        public int MaxWorkDays { get; init; }
        public int MinRestDays { get; init; }
        public decimal MinPauseHours { get; init; }
        public decimal MaxOptimalGap { get; init; }
        public decimal MaxDailyHours { get; init; }
        public decimal MaxWeeklyHours { get; init; }
        public int MaxConsecutiveDays { get; init; }
    }
}
