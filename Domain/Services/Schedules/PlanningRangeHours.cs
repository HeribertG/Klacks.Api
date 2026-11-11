// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Services.Schedules;

/// <summary>
/// Contract hours stated per pay period, summed over exactly a planning range [from, until]: each day contributes its
/// pay-period share via <see cref="PayPeriodTargetHoursProrator"/> and uses that day's own contract data, so a contract
/// change or a month-specific company value inside the range is honoured. An agent without contract data on a day
/// contributes nothing for that day, and neither does a day outside the agent's company membership (member days only,
/// <see cref="MembershipTargetProration"/>). Values of different pay intervals become comparable this way (weekly 40 h
/// over one week is 40 h, monthly 100 h over one week of a 31-day month is 100 x 7/31 h).
/// </summary>
/// <param name="agentIds">Agents to sum; every agent gets one entry (0 without data), a repeated id is summed once</param>
/// <param name="from">First day of the planning range, inclusive</param>
/// <param name="until">Last day of the planning range, inclusive</param>
/// <param name="contractDataByDate">Effective contract data (contract or settings defaults) per day and agent</param>
/// <param name="individualPeriodsByContract">Period rows per contract with an Individual interval; a missing entry falls back to the calendar month</param>
/// <param name="membershipWindows">Membership window per agent; null or a missing entry means unrestricted</param>
public static class PlanningRangeHours
{
    /// <summary>Guaranteed hours over the range; stated per GuaranteedHoursBasisInterval, else per PaymentInterval.</summary>
    public static Dictionary<Guid, decimal> GuaranteedHours(
        IReadOnlyList<Guid> agentIds,
        DateOnly from,
        DateOnly until,
        IReadOnlyDictionary<DateOnly, Dictionary<Guid, EffectiveContractData>> contractDataByDate,
        IReadOnlyDictionary<Guid, IReadOnlyCollection<Period>> individualPeriodsByContract,
        IReadOnlyDictionary<Guid, MembershipWindow>? membershipWindows = null)
        => Sum(agentIds, from, until, contractDataByDate, individualPeriodsByContract, membershipWindows,
            data => (data.GuaranteedHours, (PaymentInterval)(data.GuaranteedHoursBasisInterval ?? data.PaymentInterval)));

    /// <summary>Full-time hours over the range; stated per the PaymentInterval of the contract (or of the settings).</summary>
    public static Dictionary<Guid, decimal> FullTime(
        IReadOnlyList<Guid> agentIds,
        DateOnly from,
        DateOnly until,
        IReadOnlyDictionary<DateOnly, Dictionary<Guid, EffectiveContractData>> contractDataByDate,
        IReadOnlyDictionary<Guid, IReadOnlyCollection<Period>> individualPeriodsByContract,
        IReadOnlyDictionary<Guid, MembershipWindow>? membershipWindows = null)
        => Sum(agentIds, from, until, contractDataByDate, individualPeriodsByContract, membershipWindows,
            data => (data.FullTime, (PaymentInterval)data.PaymentInterval));

    private static Dictionary<Guid, decimal> Sum(
        IReadOnlyList<Guid> agentIds,
        DateOnly from,
        DateOnly until,
        IReadOnlyDictionary<DateOnly, Dictionary<Guid, EffectiveContractData>> contractDataByDate,
        IReadOnlyDictionary<Guid, IReadOnlyCollection<Period>> individualPeriodsByContract,
        IReadOnlyDictionary<Guid, MembershipWindow>? membershipWindows,
        Func<EffectiveContractData, (decimal Hours, PaymentInterval Interval)> hoursPerPayPeriod)
    {
        var distinctAgentIds = agentIds.Distinct().ToList();
        var result = distinctAgentIds.ToDictionary(id => id, _ => 0m);
        for (var date = from; date <= until; date = date.AddDays(1))
        {
            if (!contractDataByDate.TryGetValue(date, out var perDay))
            {
                continue;
            }

            foreach (var agentId in distinctAgentIds)
            {
                if (!perDay.TryGetValue(agentId, out var data)
                    || (membershipWindows is not null
                        && !MembershipWindows.IsMemberOn(membershipWindows, agentId, date)))
                {
                    continue;
                }

                var (hours, interval) = hoursPerPayPeriod(data);
                var periods = data.ContractId is { } contractId
                    && individualPeriodsByContract.TryGetValue(contractId, out var found)
                        ? found
                        : null;
                result[agentId] += PayPeriodTargetHoursProrator.DailyShare(hours, interval, date, periods);
            }
        }

        return result;
    }
}
