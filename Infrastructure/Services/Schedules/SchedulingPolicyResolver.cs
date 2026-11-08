// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Scheduling;

namespace Klacks.Api.Infrastructure.Services.Schedules;

/// <summary>
/// Default implementation of <see cref="ISchedulingPolicyResolver"/>. Delegates to
/// <see cref="IClientContractDataProvider"/> which already merges global settings, contract values
/// and scheduling-rule overrides into <see cref="EffectiveContractData"/>. Minimums (rest hours, rest days)
/// are taken as they are, so 0 means no requirement; a missing setting is already replaced by
/// <see cref="SchedulingPolicyDefaults"/> when the settings are read. Caps (daily hours, weekly hours,
/// consecutive days) below 1 are rejected on input in scheduling rules, company settings and planning profiles;
/// a stored 0 (legacy data, region import) is never read as a 0-hour cap and falls back to
/// <see cref="SchedulingPolicyDefaults"/>.
/// </summary>
/// <param name="contractProvider">Underlying provider that walks settings → contract → scheduling-rule</param>
public sealed class SchedulingPolicyResolver : ISchedulingPolicyResolver
{
    private readonly IClientContractDataProvider _contractProvider;

    public SchedulingPolicyResolver(IClientContractDataProvider contractProvider)
    {
        _contractProvider = contractProvider;
    }

    public async Task<SchedulingPolicy> GetForClientAsync(Guid clientId, DateOnly date)
    {
        var data = await _contractProvider.GetEffectiveContractDataAsync(clientId, date);
        return MapToPolicy(data);
    }

    public async Task<IReadOnlyDictionary<Guid, SchedulingPolicy>> GetForClientsAsync(
        IReadOnlyList<Guid> clientIds, DateOnly date)
    {
        if (clientIds.Count == 0)
        {
            return new Dictionary<Guid, SchedulingPolicy>();
        }

        var perClient = await _contractProvider.GetEffectiveContractDataForClientsAsync(clientIds.ToList(), date);
        var result = new Dictionary<Guid, SchedulingPolicy>(perClient.Count);
        foreach (var kv in perClient)
        {
            result[kv.Key] = MapToPolicy(kv.Value);
        }
        return result;
    }

    private static SchedulingPolicy MapToPolicy(EffectiveContractData data)
    {
        var maxDailyHours = data.MaxDailyHours > 0
            ? (double)data.MaxDailyHours
            : SchedulingPolicyDefaults.MaxDailyHours;
        var maxConsecutiveDays = data.MaxConsecutiveDays > 0
            ? data.MaxConsecutiveDays
            : SchedulingPolicyDefaults.MaxConsecutiveDays;
        var maxWeeklyHours = data.MaxWeeklyHours > 0
            ? (double)data.MaxWeeklyHours
            : SchedulingPolicyDefaults.MaxWeeklyHours;

        return new SchedulingPolicy(
            MinRestHours: TimeSpan.FromHours((double)data.MinPauseHours),
            MaxDailyHours: TimeSpan.FromHours(maxDailyHours),
            MaxConsecutiveDays: maxConsecutiveDays,
            MaxWeeklyHours: TimeSpan.FromHours(maxWeeklyHours),
            MinRestDays: data.MinRestDays,
            MaxDailySpan: data.MaxDailySpanHours > 0 ? TimeSpan.FromHours((double)data.MaxDailySpanHours) : null);
    }
}
