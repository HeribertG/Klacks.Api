// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Associations;

namespace Klacks.Api.Domain.Services.Schedules;

/// <summary>
/// Picks the contract data a planner uses as an employee's master data for a period (among others the shift-work
/// flag): the first member day with an active contract, otherwise the first member day planned with the settings
/// defaults. Membership follows <see cref="MembershipWindows.IsMemberOn"/>; an employee without any member day with
/// contract data gets no basis. Wizard 1 and the Harmonizer family use this one choice, so they agree on the flags.
/// </summary>
public static class PlanningContractBasis
{
    /// <summary>Basis per employee; employees without a member day with contract data are missing from the result.</summary>
    /// <param name="agentIds">Employees to resolve.</param>
    /// <param name="from">First day of the period, inclusive.</param>
    /// <param name="until">Last day of the period, inclusive.</param>
    /// <param name="contractDataByDate">Effective contract data (contract or settings defaults) per day and employee.</param>
    /// <param name="membershipWindows">Membership window per employee; a missing entry means unrestricted.</param>
    public static Dictionary<Guid, EffectiveContractData> Select(
        IReadOnlyList<Guid> agentIds,
        DateOnly from,
        DateOnly until,
        IReadOnlyDictionary<DateOnly, Dictionary<Guid, EffectiveContractData>> contractDataByDate,
        IReadOnlyDictionary<Guid, MembershipWindow> membershipWindows)
    {
        var contractBasis = new Dictionary<Guid, EffectiveContractData>();
        var defaultsBasis = new Dictionary<Guid, EffectiveContractData>();

        for (var date = from; date <= until; date = date.AddDays(1))
        {
            if (!contractDataByDate.TryGetValue(date, out var perDay))
            {
                continue;
            }

            foreach (var agentId in agentIds)
            {
                if (!perDay.TryGetValue(agentId, out var data)
                    || !MembershipWindows.IsMemberOn(membershipWindows, agentId, date))
                {
                    continue;
                }

                var target = data.HasActiveContract ? contractBasis : defaultsBasis;
                target.TryAdd(agentId, data);
            }
        }

        foreach (var (agentId, defaults) in defaultsBasis)
        {
            contractBasis.TryAdd(agentId, defaults);
        }

        return contractBasis;
    }
}
