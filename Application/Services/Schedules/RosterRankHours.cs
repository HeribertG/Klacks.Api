// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Services.Schedules;

namespace Klacks.Api.Application.Services.Schedules;

/// <summary>
/// The sort keys of the automatic roster priority order (<see cref="RosterPriorityOrder"/>): guaranteed hours and
/// full-time hours of every agent scaled to the planning range, member days only (<see cref="PlanningRangeHours"/>), so
/// different pay intervals compare fairly and an entry or exit inside the range counts only its member days. Every wizard
/// stage computes them from the same contract data and membership windows, so all stages rank alike. The contract's
/// Individual period rows are not read (the calendar month stands in) because Wizard 1 has no access to them; this
/// affects only the sort key, never the targets the engines plan with.
/// </summary>
public sealed class RosterRankHours
{
    private static readonly IReadOnlyDictionary<Guid, IReadOnlyCollection<Period>> NoIndividualPeriods =
        new Dictionary<Guid, IReadOnlyCollection<Period>>();

    private readonly IReadOnlyDictionary<Guid, decimal> _guaranteedHours;
    private readonly IReadOnlyDictionary<Guid, decimal> _fullTime;

    private RosterRankHours(IReadOnlyDictionary<Guid, decimal> guaranteedHours, IReadOnlyDictionary<Guid, decimal> fullTime)
    {
        _guaranteedHours = guaranteedHours;
        _fullTime = fullTime;
    }

    /// <param name="agentIds">Agents to rank</param>
    /// <param name="from">First day of the planning range, inclusive</param>
    /// <param name="until">Last day of the planning range, inclusive</param>
    /// <param name="contractDataByDate">Effective contract data per day and agent over the planning range</param>
    /// <param name="membershipWindows">Membership window per agent; a missing entry means unrestricted</param>
    public static RosterRankHours Compute(
        IReadOnlyList<Guid> agentIds,
        DateOnly from,
        DateOnly until,
        IReadOnlyDictionary<DateOnly, Dictionary<Guid, EffectiveContractData>> contractDataByDate,
        IReadOnlyDictionary<Guid, MembershipWindow> membershipWindows)
        => new(
            PlanningRangeHours.GuaranteedHours(agentIds, from, until, contractDataByDate, NoIndividualPeriods, membershipWindows),
            PlanningRangeHours.FullTime(agentIds, from, until, contractDataByDate, NoIndividualPeriods, membershipWindows));

    /// <summary>Guaranteed hours of the agent over the planning range; 0 for an unknown agent.</summary>
    public double GuaranteedHoursOf(Guid agentId) => (double)_guaranteedHours.GetValueOrDefault(agentId);

    /// <summary>Full-time hours of the agent over the planning range; 0 for an unknown agent.</summary>
    public double FullTimeOf(Guid agentId) => (double)_fullTime.GetValueOrDefault(agentId);
}
