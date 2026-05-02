// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Scheduling;

namespace Klacks.Api.Application.Interfaces.Schedules;

/// <summary>
/// Resolves the effective scheduling policy (MinRestHours, MaxDailyHours, MaxConsecutiveDays)
/// for a given client at a given date by reading the client's contract / scheduling-rule and
/// falling back to the global settings table. Used by the post-hoc validator so it enforces
/// the same numbers the wizard placement engine consumed during scheduling.
/// </summary>
public interface ISchedulingPolicyResolver
{
    Task<SchedulingPolicy> GetForClientAsync(Guid clientId, DateOnly date);

    Task<IReadOnlyDictionary<Guid, SchedulingPolicy>> GetForClientsAsync(IReadOnlyList<Guid> clientIds, DateOnly date);
}
