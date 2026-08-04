// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Computes the resource monitor's per-day series (desired readiness, maximum readiness, headcount,
/// shift demand, absence load) from already loaded data. Pure and free of any group-visibility scope,
/// so the HTTP path and the background email path can share one formula instead of keeping two copies
/// that drift apart: the caller decides which data it is allowed to see, this decides what the numbers are.
/// </summary>

using Klacks.Api.Application.DTOs.Dashboard;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Staffs;

namespace Klacks.Api.Application.Services.Schedules;

public static class DailyReadinessCalculator
{
    private const int FullWeekDays = 7;

    /// <summary>
    /// Builds one entry per calendar day in the range.
    /// </summary>
    /// <param name="startDate">First day of the range</param>
    /// <param name="endDate">Last day of the range</param>
    /// <param name="allClientIds">Every client considered, contracted or plain employee</param>
    /// <param name="contractsByClient">Active contracts per client, used for the weekday pattern and the caps</param>
    /// <param name="employeeClientIds">Clients that count as active even without a contract</param>
    /// <param name="shifts">Shift definitions whose weekday flags produce the daily demand</param>
    /// <param name="absenceByDate">Already planned absence load per day, summed from placeholder DefaultValues</param>
    /// <param name="defaultPattern">Weekday pattern used when no usable contract pattern exists</param>
    /// <param name="settingMaxWorkDays">Fallback cap for the desired readiness</param>
    /// <param name="settingMaxConsecutiveDays">Fallback cap for the maximum readiness</param>
    public static List<ResourceMonitorDayResource> Build(
        DateOnly startDate,
        DateOnly endDate,
        IReadOnlyCollection<Guid> allClientIds,
        IReadOnlyDictionary<Guid, List<ClientContract>> contractsByClient,
        IReadOnlySet<Guid> employeeClientIds,
        IReadOnlyList<DashboardShiftRow> shifts,
        IReadOnlyDictionary<DateOnly, double> absenceByDate,
        WeekdayPattern defaultPattern,
        int settingMaxWorkDays,
        int settingMaxConsecutiveDays)
    {
        var totalDays = endDate.DayNumber - startDate.DayNumber + 1;
        var dailyData = new List<ResourceMonitorDayResource>(Math.Max(totalDays, 0));

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
                {
                    continue;
                }

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
                    maxCap = EffectiveCap(primary, settingMaxConsecutiveDays, useConsecutive: true);
                }

                wunschCount += ContributionForPattern(pattern, date, wunschCap);
                maxCount += ContributionForPattern(pattern, date, maxCap);
            }

            double dienstCount = 0;
            foreach (var s in shifts)
            {
                if (s.FromDate > date || (s.UntilDate.HasValue && s.UntilDate.Value < date))
                {
                    continue;
                }

                if (IsWeekdayActive(date, s.IsMonday, s.IsTuesday, s.IsWednesday, s.IsThursday, s.IsFriday, s.IsSaturday, s.IsSunday))
                {
                    dienstCount += 1;
                }
            }

            absenceByDate.TryGetValue(date, out var absenzCount);

            dailyData.Add(new ResourceMonitorDayResource
            {
                Date = date,
                WunschCount = Math.Round(wunschCount, 2),
                MaxCount = Math.Round(maxCount, 2),
                TotalCount = totalCount,
                DienstCount = dienstCount,
                AbsenzCount = Math.Round(absenzCount, 2),
            });
        }

        return dailyData;
    }

    public static int EffectiveCap(ClientContract cc, int settingFallback, bool useConsecutive)
    {
        var rule = cc.Contract?.SchedulingRule;
        if (rule != null)
        {
            var ruleValue = useConsecutive ? rule.MaxConsecutiveDays : rule.MaxWorkDays;
            if (ruleValue.HasValue && ruleValue.Value > 0)
            {
                return ruleValue.Value;
            }
        }

        return settingFallback;
    }

    public static bool ShouldUseSettings(List<ClientContract> activeContracts)
    {
        if (activeContracts.Count == 0)
        {
            return true;
        }

        foreach (var cc in activeContracts)
        {
            if (cc.Contract is null)
            {
                return true;
            }

            if (FlaggedDays(cc.Contract) == 0)
            {
                return true;
            }
        }

        return false;
    }

    public static double ContributionForPattern(WeekdayPattern pattern, DateOnly date, int cap)
    {
        var flaggedDays = pattern.FlaggedDays;
        if (flaggedDays == 0)
        {
            return 0;
        }

        if (flaggedDays == FullWeekDays)
        {
            return Math.Min(cap, FullWeekDays) / (double)FullWeekDays;
        }

        return pattern.IsActiveOn(date) ? 1.0 : 0.0;
    }

    private static int FlaggedDays(Contract contract) =>
        (contract.WorkOnMonday ? 1 : 0) +
        (contract.WorkOnTuesday ? 1 : 0) +
        (contract.WorkOnWednesday ? 1 : 0) +
        (contract.WorkOnThursday ? 1 : 0) +
        (contract.WorkOnFriday ? 1 : 0) +
        (contract.WorkOnSaturday ? 1 : 0) +
        (contract.WorkOnSunday ? 1 : 0);

    private static bool IsWeekdayActive(
        DateOnly date,
        bool mon, bool tue, bool wed, bool thu, bool fri, bool sat, bool sun) => date.DayOfWeek switch
        {
            DayOfWeek.Monday => mon,
            DayOfWeek.Tuesday => tue,
            DayOfWeek.Wednesday => wed,
            DayOfWeek.Thursday => thu,
            DayOfWeek.Friday => fri,
            DayOfWeek.Saturday => sat,
            DayOfWeek.Sunday => sun,
            _ => false,
        };
}
