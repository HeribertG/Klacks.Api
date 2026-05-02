// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Scheduling;

namespace Klacks.Api.Infrastructure.Services.Schedules;

/// <summary>
/// Default implementation of <see cref="ISchedulingPolicyResolver"/>. Delegates to
/// <see cref="IClientContractDataProvider"/> which already merges global settings, contract values
/// and scheduling-rule overrides into <see cref="EffectiveContractData"/>. Zero or missing values
/// fall back to <see cref="SchedulingPolicyDefaults"/> so the validator never compares against zero.
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
        var minRestHours = data.MinPauseHours > 0
            ? (double)data.MinPauseHours
            : SchedulingPolicyDefaults.MinRestHours;
        var maxDailyHours = data.MaxDailyHours > 0
            ? (double)data.MaxDailyHours
            : SchedulingPolicyDefaults.MaxDailyHours;
        var maxConsecutiveDays = data.MaxConsecutiveDays > 0
            ? data.MaxConsecutiveDays
            : SchedulingPolicyDefaults.MaxConsecutiveDays;

        return new SchedulingPolicy(
            MinRestHours: TimeSpan.FromHours(minRestHours),
            MaxDailyHours: TimeSpan.FromHours(maxDailyHours),
            MaxConsecutiveDays: maxConsecutiveDays);
    }
}
