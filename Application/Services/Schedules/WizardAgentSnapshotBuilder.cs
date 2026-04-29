// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Models.Associations;
using Klacks.ScheduleOptimizer.Models;

namespace Klacks.Api.Application.Services.Schedules;

/// <summary>
/// Builds CoreAgents (one per agent) and CoreContractDays (one per active agent-date pair) from
/// effective contract data. Handles mid-period contract switches by querying the provider per date.
/// </summary>
/// <param name="contractProvider">Source of effective contract data per client and date</param>
public sealed class WizardAgentSnapshotBuilder
{
    private readonly IClientContractDataProvider _contractProvider;

    public WizardAgentSnapshotBuilder(IClientContractDataProvider contractProvider)
    {
        _contractProvider = contractProvider;
    }

    public async Task<AgentSnapshotResult> BuildAsync(
        IReadOnlyList<Guid> agentIds,
        DateOnly from,
        DateOnly until,
        IReadOnlyDictionary<Guid, double> currentHoursPerAgent,
        CancellationToken ct)
    {
        var contractDays = new List<CoreContractDay>();
        var firstDayContracts = new Dictionary<Guid, EffectiveContractData>();

        for (var date = from; date <= until; date = date.AddDays(1))
        {
            ct.ThrowIfCancellationRequested();
            var perDay = await _contractProvider.GetEffectiveContractDataForClientsAsync(
                agentIds.ToList(), date);

            if (date == from)
            {
                foreach (var kv in perDay)
                {
                    firstDayContracts[kv.Key] = kv.Value;
                }
            }

            foreach (var agentId in agentIds)
            {
                if (!perDay.TryGetValue(agentId, out var data))
                {
                    continue;
                }

                var worksOnDay = GetWorkOnDayFlag(data, date.DayOfWeek);
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

        var agents = firstDayContracts
            .Select(kv => BuildAgent(kv.Key, kv.Value, currentHoursPerAgent.GetValueOrDefault(kv.Key, 0)))
            .ToList();

        return new AgentSnapshotResult(agents, contractDays);
    }

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

    private static CoreAgent BuildAgent(Guid agentId, EffectiveContractData data, double currentHours)
    {
        return new CoreAgent(
            Id: agentId.ToString(),
            CurrentHours: currentHours,
            GuaranteedHours: (double)data.GuaranteedHours,
            MaxConsecutiveDays: data.MaxConsecutiveDays > 0 ? data.MaxConsecutiveDays : 6,
            MinRestHours: data.MinPauseHours > 0 ? (double)data.MinPauseHours : 11,
            Motivation: 0.5,
            MaxDailyHours: data.MaxDailyHours > 0 ? (double)data.MaxDailyHours : 10,
            MaxWeeklyHours: data.MaxWeeklyHours > 0 ? (double)data.MaxWeeklyHours : 50,
            MaxOptimalGap: data.MaxOptimalGap > 0 ? (double)data.MaxOptimalGap : 2)
        {
            FullTime = (double)data.FullTime,
            MaximumHours = (double)data.MaximumHours,
            MinimumHours = (double)data.MinimumHours,
            MaxWorkDays = data.MaxWorkDays > 0 ? data.MaxWorkDays : 5,
            MinRestDays = data.MinRestDays > 0 ? data.MinRestDays : 2,
            PerformsShiftWork = data.PerformsShiftWork,
            WorkOnMonday = data.WorkOnMonday,
            WorkOnTuesday = data.WorkOnTuesday,
            WorkOnWednesday = data.WorkOnWednesday,
            WorkOnThursday = data.WorkOnThursday,
            WorkOnFriday = data.WorkOnFriday,
            WorkOnSaturday = data.WorkOnSaturday,
            WorkOnSunday = data.WorkOnSunday,
        };
    }
}

/// <summary>
/// Result of <see cref="WizardAgentSnapshotBuilder.BuildAsync"/>.
/// </summary>
/// <param name="Agents">One CoreAgent per agent id with non-empty contract data on the first day</param>
/// <param name="ContractDays">One CoreContractDay per (agent, date) pair within the period</param>
public sealed record AgentSnapshotResult(
    IReadOnlyList<CoreAgent> Agents,
    IReadOnlyList<CoreContractDay> ContractDays);
