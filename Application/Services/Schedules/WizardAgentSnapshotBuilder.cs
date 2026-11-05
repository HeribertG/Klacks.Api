// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Services.Schedules;
using Klacks.ScheduleOptimizer.Models;

namespace Klacks.Api.Application.Services.Schedules;

/// <summary>
/// Builds CoreAgents (one per agent) and CoreContractDays (one per agent-date pair) from
/// effective contract data. Handles mid-period contract switches by querying the provider per date.
/// The agent master data comes from <see cref="PlanningContractBasis"/>: the FIRST day inside the period that has an active contract —
/// a contract starting mid-period must not leave the agent stuck on the contract-less defaults of
/// day one; a member without any active contract day in the period is planned with the settings defaults of its first
/// member day (small companies keep no contracts). Member days without an active contract use those settings defaults
/// (their weekday flags); only days outside the agent's company membership (Membership.ValidFrom / ValidUntil,
/// inclusive) are marked WorksOnDay=false, and an agent without any member day in the period is excluded. An agent without a membership
/// row is unrestricted, like in the schedule view. When the membership starts or ends inside the target period, the
/// targets are prorated by member days (<see cref="MembershipTargetProration"/>); MaximumHours is not. The target period
/// is the pay period the targets refer to (CurrentHours carries the hours before the planning range), so planning only
/// the last week of a month still prorates against the whole month.
/// </summary>
/// <param name="contractProvider">Source of effective contract data per client and date</param>
/// <param name="membershipWindowReader">Source of the company membership window per client</param>
public sealed class WizardAgentSnapshotBuilder
{
    private readonly IClientContractDataProvider _contractProvider;
    private readonly IMembershipWindowReader _membershipWindowReader;

    public WizardAgentSnapshotBuilder(
        IClientContractDataProvider contractProvider,
        IMembershipWindowReader membershipWindowReader)
    {
        _contractProvider = contractProvider;
        _membershipWindowReader = membershipWindowReader;
    }

    public async Task<AgentSnapshotResult> BuildAsync(
        IReadOnlyList<Guid> agentIds,
        DateOnly from,
        DateOnly until,
        DateOnly targetFrom,
        DateOnly targetUntil,
        IReadOnlyDictionary<Guid, double> currentHoursPerAgent,
        CancellationToken ct)
    {
        var contractDays = new List<CoreContractDay>();

        var contractDataByDate = await _contractProvider.GetEffectiveContractDataForClientsRangeAsync(
            agentIds.ToList(), from, until);
        var membershipWindows = await _membershipWindowReader.GetWindowsAsync(agentIds, ct);

        for (var date = from; date <= until; date = date.AddDays(1))
        {
            ct.ThrowIfCancellationRequested();
            var perDay = contractDataByDate[date];

            foreach (var agentId in agentIds)
            {
                var isMember = MembershipWindows.IsMemberOn(membershipWindows, agentId, date);
                if (!perDay.TryGetValue(agentId, out var data))
                {
                    // Outside the membership the day is explicitly closed: without a contract day the engine would
                    // fall back to the static weekday flags and could plan the agent there.
                    if (!isMember)
                    {
                        contractDays.Add(ClosedDay(agentId, date));
                    }

                    continue;
                }

                // A member day without an active contract is planned with the settings defaults (small companies
                // keep no contracts); only days outside the membership are hard non-working days.
                var worksOnDay = isMember && GetWorkOnDayFlag(data, date.DayOfWeek);
                contractDays.Add(new CoreContractDay(
                    AgentId: agentId.ToString(),
                    Date: date,
                    WorksOnDay: worksOnDay,
                    PerformsShiftWork: data.PerformsShiftWork,
                    FullTimeShare: (double)data.FullTime,
                    MaximumHoursPerDay: (double)data.MaxDailyHours,
                    ContractId: data.ContractId ?? Guid.Empty));
            }
        }

        var contractBasis = PlanningContractBasis.Select(agentIds, from, until, contractDataByDate, membershipWindows);

        // Preserve the caller's roster order: it is the user's base ordering and downstream
        // consumers (auction IndexBonus, fitness Stage2 decay, repair top-bias) treat the
        // position in this list as the top-down priority rank.
        var agents = agentIds
            .Where(contractBasis.ContainsKey)
            .Select(id => BuildAgent(
                id,
                contractBasis[id],
                currentHoursPerAgent.GetValueOrDefault(id, 0),
                MembershipTargetProration.FactorFor(membershipWindows.GetValueOrDefault(id), targetFrom, targetUntil)))
            .ToList();

        return new AgentSnapshotResult(agents, contractDays);
    }

    private static CoreContractDay ClosedDay(Guid agentId, DateOnly date) => new(
        AgentId: agentId.ToString(),
        Date: date,
        WorksOnDay: false,
        PerformsShiftWork: false,
        FullTimeShare: 0,
        MaximumHoursPerDay: 0,
        ContractId: Guid.Empty);

    private static bool GetWorkOnDayFlag(EffectiveContractData data, DayOfWeek dayOfWeek) => dayOfWeek switch
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

    /// <summary>
    /// The engine agent of one employee. The targets the engine strives for (GuaranteedHours, FullTime, MinimumHours) are
    /// scaled by <paramref name="membershipFactor"/> when the membership starts or ends inside the period, so the engine
    /// does not pack the full target into the member days; MaximumHours stays the contract's hard ceiling, unscaled.
    /// </summary>
    /// <param name="agentId">Employee id</param>
    /// <param name="data">Contract data of the employee's first active member day</param>
    /// <param name="currentHours">Hours already worked before the period</param>
    /// <param name="membershipFactor">Share of the period inside the membership; null = no scaling</param>
    private static CoreAgent BuildAgent(Guid agentId, EffectiveContractData data, double currentHours, decimal? membershipFactor)
    {
        return new CoreAgent(
            Id: agentId.ToString(),
            CurrentHours: currentHours,
            GuaranteedHours: Prorate(data.GuaranteedHours, membershipFactor),
            MaxConsecutiveDays: data.MaxConsecutiveDays > 0 ? data.MaxConsecutiveDays : WizardSchedulingDefaults.MaxConsecutiveDays,
            MinRestHours: data.MinPauseHours > 0 ? (double)data.MinPauseHours : WizardSchedulingDefaults.MinRestHours,
            Motivation: WizardSchedulingDefaults.DefaultMotivation,
            MaxDailyHours: data.MaxDailyHours > 0 ? (double)data.MaxDailyHours : WizardSchedulingDefaults.MaxDailyHours,
            MaxWeeklyHours: data.MaxWeeklyHours > 0 ? (double)data.MaxWeeklyHours : WizardSchedulingDefaults.MaxWeeklyHours,
            MaxOptimalGap: data.MaxOptimalGap > 0 ? (double)data.MaxOptimalGap : WizardSchedulingDefaults.MaxOptimalGap)
        {
            FullTime = Prorate(data.FullTime, membershipFactor),
            MaximumHours = (double)data.MaximumHours,
            MinimumHours = Prorate(data.MinimumHours, membershipFactor),
            MaxWorkDays = data.MaxWorkDays > 0 ? data.MaxWorkDays : WizardSchedulingDefaults.MaxWorkDays,
            // CoreAgent plans whole calendar days; a fractional legal minimum (e.g. Spain's 1.5/week)
            // is rounded UP so the optimizer never targets fewer rest days than required - the exact
            // decimal threshold is still what ScheduleValidationBuilder checks post-hoc.
            MinRestDays = (int)Math.Ceiling(data.MinRestDays > 0 ? data.MinRestDays : SchedulingPolicyDefaults.MinRestDays),
            PerformsShiftWork = data.PerformsShiftWork,
            WorkOnMonday = data.WorkOnMonday,
            WorkOnTuesday = data.WorkOnTuesday,
            WorkOnWednesday = data.WorkOnWednesday,
            WorkOnThursday = data.WorkOnThursday,
            WorkOnFriday = data.WorkOnFriday,
            WorkOnSaturday = data.WorkOnSaturday,
            WorkOnSunday = data.WorkOnSunday,
            NightRate = data.NightRate,
            HolidayRate = data.HolidayRate,
            WE1Rate = data.WE1Rate,
            WE2Rate = data.WE2Rate,
            WE3Rate = data.WE3Rate,
            // Without the modes a fixed time credit per hour or per shift would be estimated as a
            // percentage multiplier — a silently wrong cost estimate during planning.
            NightRateMode = MapRateMode(data.NightRateMode),
            HolidayRateMode = MapRateMode(data.HolidayRateMode),
            WE1RateMode = MapRateMode(data.WE1RateMode),
            WE2RateMode = MapRateMode(data.WE2RateMode),
            WE3RateMode = MapRateMode(data.WE3RateMode),
        };
    }

    private static double Prorate(decimal target, decimal? membershipFactor)
        => membershipFactor is { } factor ? (double)(target * factor) : (double)target;

    private static CoreSurchargeRateMode MapRateMode(SurchargeRateMode mode) => mode switch
    {
        SurchargeRateMode.FixedPerHour => CoreSurchargeRateMode.FixedPerHour,
        SurchargeRateMode.FixedPerShift => CoreSurchargeRateMode.FixedPerShift,
        _ => CoreSurchargeRateMode.Multiplier,
    };
}
